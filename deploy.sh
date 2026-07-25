#!/usr/bin/env bash
# deploy.sh — twin of deploy.ps1 for Git Bash / Linux / macOS.
# Usage: ./deploy.sh [dev|prod] [up|down|logs|ps|restart|build|config]
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
cd "$ROOT"
export MSYS_NO_PATHCONV=1

ENVNAME="${1:-dev}"
ACTION="${2:-up}"
case "$ENVNAME" in dev|prod) ;; *) echo "[ERROR] env must be dev|prod"; exit 1;; esac

CONF="$ROOT/deploy/remote.conf"
[ -f "$CONF" ] || { echo "[ERROR] deploy/remote.conf not found. Copy deploy/remote.conf.example."; exit 1; }
# shellcheck disable=SC1090
source "$CONF"
: "${REMOTE_HOST:?missing in remote.conf}"; : "${REMOTE_USER:?}"; : "${DOCKER_CONTEXT:?}"; : "${PROJECT_SLUG:?}"

PROJECT="$PROJECT_SLUG-$ENVNAME"
ENV_FILE=".env.$ENVNAME"
[ -f "$ENV_FILE" ] || { echo "[ERROR] $ENV_FILE not found. Copy $ENV_FILE.example to $ENV_FILE."; exit 1; }

if ! docker context ls --format '{{.Name}}' | grep -qx "$DOCKER_CONTEXT"; then
    echo "[setup] Creating Docker context '$DOCKER_CONTEXT' -> ssh://$REMOTE_USER@$REMOTE_HOST"
    docker context create "$DOCKER_CONTEXT" --docker "host=ssh://$REMOTE_USER@$REMOTE_HOST" >/dev/null
fi

FILES=(-f docker-compose.yml)
if [ "$ENVNAME" = "prod" ]; then
    FILES+=(-f docker-compose.prod.yml)
fi

if [ "$ENVNAME" = "prod" ] && [[ "$ACTION" =~ ^(up|restart|build)$ ]]; then
    echo "About to run '$ACTION' on PROD ($PROJECT) on $REMOTE_HOST."
    read -r -p "Type the project slug '$PROJECT_SLUG' to confirm: " TYPED
    [ "$TYPED" = "$PROJECT_SLUG" ] || { echo "Aborted."; exit 1; }
fi

export ENV_FILE
DC=(docker --context "$DOCKER_CONTEXT" compose --env-file "$ENV_FILE" "${FILES[@]}" -p "$PROJECT")

echo "[$ENVNAME] docker --context $DOCKER_CONTEXT compose -p $PROJECT  ($ACTION)"
case "$ACTION" in
    up)      "${DC[@]}" up -d --build; "${DC[@]}" ps ;;
    build)   "${DC[@]}" build ;;
    down)    "${DC[@]}" down ;;
    restart) "${DC[@]}" restart ;;
    logs)    "${DC[@]}" logs -f --tail 100 ;;
    ps)      "${DC[@]}" ps ;;
    config)  "${DC[@]}" config ;;
    *) echo "[ERROR] unknown action: $ACTION"; exit 1 ;;
esac

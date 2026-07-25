@echo off
setlocal EnableExtensions
set "ROOT=%~dp0"
cd /d "%ROOT%"

echo.
echo ========================================
echo   ScopeSeal - Local Development
echo ========================================
echo.

call :requireTool dotnet ".NET 10 SDK" "https://dotnet.microsoft.com/download"
if errorlevel 1 goto :failed
call :requireTool node "Node.js 22+" "https://nodejs.org/"
if errorlevel 1 goto :failed
call :requireTool npm "npm (included with Node.js)"
if errorlevel 1 goto :failed

set "DOCKER_OK=0"
where docker >nul 2>&1
if errorlevel 1 (
    echo WARNING: Docker not found. Skipping PostgreSQL and Azurite.
    echo          Install Docker Desktop, then run: docker compose up -d
    echo.
    goto :start_api
)

echo [1/3] Starting Docker dependencies ^(PostgreSQL + Azurite^)...
docker compose -f docker-compose.local.yml --profile local up -d
if errorlevel 1 (
    echo WARNING: docker compose failed. The API may not start without PostgreSQL.
    echo.
    goto :start_api
)

set "DOCKER_OK=1"
echo Waiting for PostgreSQL to accept connections...
for /L %%i in (1,1,20) do (
    docker compose -f docker-compose.local.yml --profile local exec -T postgres pg_isready -U scopeseal -d scopeseal >nul 2>&1
    if not errorlevel 1 goto :deps_ready
    timeout /t 2 /nobreak >nul
)
echo WARNING: PostgreSQL may still be starting. The API will retry migrations on boot.

:deps_ready
echo.

:start_api
echo [2/3] Starting Backend API in a new window...
start "ScopeSeal API" cmd /k "cd /d ""%ROOT%src\backend"" && dotnet run --project hosts/ScopeSeal.Api --launch-profile http"

if not exist "%ROOT%src\clients\node_modules\" (
    echo Installing npm dependencies ^(first run^)...
    pushd "%ROOT%src\clients"
    call npm install
    if errorlevel 1 (
        popd
        goto :failed
    )
    popd
    echo.
)

echo [3/3] Starting Product App in a new window...
start "ScopeSeal Product App" cmd /k "cd /d ""%ROOT%src\clients"" && npm run start:product"

echo.
echo ========================================
echo   ScopeSeal is starting
echo ========================================
echo.
echo   Product app     http://localhost:4200
echo   API health      http://localhost:5021/health/live
echo   API ready       http://localhost:5021/health/ready
echo   System status   http://localhost:5021/api/v1/system/status
echo   OpenAPI spec    http://localhost:5021/openapi/v1.json
echo.
echo   Optional apps ^(run manually from src\clients^):
echo   Marketing site  npm run start:marketing   ^(default port 4200; use --port 4201 if product is running^)
echo   Admin portal    npm run start:admin         ^(use --port 4202 when other apps are running^)
echo.
if "%DOCKER_OK%"=="1" (
    echo   Docker services:
    echo   PostgreSQL      localhost:5432  ^(user scopeseal, db scopeseal^)
    echo   Azurite blob    localhost:10000
    echo.
    echo   Stop Docker:     docker compose -f docker-compose.local.yml --profile local down
)
echo   Stop API/Product: close their console windows.
echo.
echo Services need a few seconds to compile and bind ports.
echo.
pause
exit /b 0

:requireTool
where %~1 >nul 2>&1
if errorlevel 1 (
    echo ERROR: %~1 was not found. Install %~2.
    if not "%~3"=="" echo        %~3
    exit /b 1
)
exit /b 0

:failed
echo.
echo Startup aborted. Fix the issue above and run start.bat again.
pause
exit /b 1

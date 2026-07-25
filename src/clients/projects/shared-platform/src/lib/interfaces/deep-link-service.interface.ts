export interface DeepLinkPayload {
  readonly url: string;
  readonly path: string;
}

/** Deep link and universal link handling. */
export interface DeepLinkService {
  getLaunchUrl(): Promise<DeepLinkPayload | null>;
  onDeepLink(callback: (payload: DeepLinkPayload) => void): () => void;
}

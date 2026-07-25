/** Online/offline connectivity status. */
export interface NetworkStatusService {
  readonly isOnline: boolean;
  onStatusChange(callback: (online: boolean) => void): () => void;
}

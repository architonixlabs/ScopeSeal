export type AppLifecycleState = 'active' | 'background' | 'inactive';

/** App foreground/background lifecycle events. */
export interface AppLifecycleService {
  readonly state: AppLifecycleState;
  onStateChange(callback: (state: AppLifecycleState) => void): () => void;
}

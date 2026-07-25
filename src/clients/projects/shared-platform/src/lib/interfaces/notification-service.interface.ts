/** Push notification abstraction (native only; browser uses no-op). */
export interface NotificationService {
  requestPermission(): Promise<'granted' | 'denied' | 'default'>;
  registerDevice(): Promise<string | null>;
}

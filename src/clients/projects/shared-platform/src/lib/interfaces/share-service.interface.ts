/** Native or browser share sheet abstraction. */
export interface ShareService {
  share(options: { title?: string; text?: string; url?: string; files?: File[] }): Promise<boolean>;
}

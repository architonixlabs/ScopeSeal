export interface CapturedImage {
  readonly mimeType: string;
  readonly sizeBytes: number;
  readonly data: Blob;
}

/** Camera capture abstraction. */
export interface CameraCaptureService {
  capturePhoto(): Promise<CapturedImage | null>;
}

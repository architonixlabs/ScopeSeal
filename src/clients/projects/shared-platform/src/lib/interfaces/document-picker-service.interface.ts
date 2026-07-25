export interface PickedDocument {
  readonly name: string;
  readonly mimeType: string;
  readonly sizeBytes: number;
  readonly data: Blob;
}

/** Document selection from device storage or system picker. */
export interface DocumentPickerService {
  pickDocument(): Promise<PickedDocument | null>;
}

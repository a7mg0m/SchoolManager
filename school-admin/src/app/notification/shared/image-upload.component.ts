import { Component, input, model, signal } from '@angular/core';
import { HttpClient, HttpEventType } from '@angular/common/http';
import { environment } from '../../../environments/environment';

/** Same limits as the Notification service */
const MAX_BYTES = 5 * 1024 * 1024;
const TYPES = ['image/jpeg', 'image/png', 'image/webp', 'image/gif'];

/**
 * Full address of a picture: "/notification-files/notifications/x.jpg" → "http://gateway/notification-files/…";
 * an absolute URL (older records) is kept as it is.
 */
export function notificationFileUrl(url: string | null | undefined): string {
  if (!url) return '';
  return /^https?:\/\//i.test(url) ? url : `${environment.gatewayUrl}${url.startsWith('/') ? '' : '/'}${url}`;
}

/**
 * Picture of a notification / an event: pick or drop a file, it is uploaded at once (with progress) and its URL
 * goes to [(value)]; replace or remove it. Saved by the server under wwwroot/notifications or wwwroot/events.
 *
 * <app-image-upload uploadUrl="/api/notification/upload-image" [(value)]="imageUrl" />
 */
@Component({
  selector: 'app-image-upload',
  standalone: false,
  styleUrl: './image-upload.component.scss',
  templateUrl: './image-upload.component.html',
})
export class ImageUploadComponent {
  /** API path of the upload (through the gateway) */
  uploadUrl = input.required<string>();
  /** The picture's URL ('' = none) */
  value = model<string>('');

  uploading = signal(false);
  progress = signal(0);
  /** Translation key of the last error */
  error = signal<string | null>(null);
  dragOver = signal(false);

  readonly fileUrl = notificationFileUrl;

  constructor(private http: HttpClient) {}

  onFileInput(event: Event): void {
    const inputEl = event.target as HTMLInputElement;
    const file = inputEl.files?.[0];
    inputEl.value = ''; // the same file can be picked again
    if (file) this.upload(file);
  }

  onDrop(event: DragEvent): void {
    event.preventDefault();
    this.dragOver.set(false);
    const file = event.dataTransfer?.files?.[0];
    if (file) this.upload(file);
  }

  onDragOver(event: DragEvent): void {
    event.preventDefault();
    this.dragOver.set(true);
  }

  remove(): void {
    this.value.set('');
    this.error.set(null);
  }

  private upload(file: File): void {
    this.error.set(null);
    if (!TYPES.includes(file.type)) {
      this.error.set('imageUpload.errors.ImageTypeNotAllowed');
      return;
    }
    if (file.size > MAX_BYTES) {
      this.error.set('imageUpload.errors.ImageTooLarge');
      return;
    }

    const form = new FormData();
    form.append('file', file, file.name);
    this.uploading.set(true);
    this.progress.set(0);

    this.http
      .post<{ status?: number; data?: string; message?: string }>(`${environment.gatewayUrl}${this.uploadUrl()}`, form, {
        reportProgress: true,
        observe: 'events'
      })
      .subscribe({
        next: event => {
          if (event.type === HttpEventType.UploadProgress && event.total) {
            this.progress.set(Math.round((event.loaded / event.total) * 100));
          } else if (event.type === HttpEventType.Response) {
            const url = event.body?.data;
            if (url) {
              this.value.set(url);
            } else {
              this.error.set('imageUpload.errors.UploadFailed');
            }
            this.uploading.set(false);
          }
        },
        error: err => {
          const key = err?.error?.message;
          this.error.set(['ImageRequired', 'ImageTooLarge', 'ImageTypeNotAllowed'].includes(key)
            ? `imageUpload.errors.${key}`
            : err?.status === 413 ? 'imageUpload.errors.ImageTooLarge' : 'imageUpload.errors.UploadFailed');
          this.uploading.set(false);
        }
      });
  }
}

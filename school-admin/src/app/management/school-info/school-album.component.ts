import { Component, EventEmitter, Input, OnDestroy, Output, TemplateRef, computed, signal } from '@angular/core';
import { HttpEventType } from '@angular/common/http';
import { NgbModal } from '@ng-bootstrap/ng-bootstrap';
import { TranslateService } from '@ngx-translate/core';
import { Subject, Subscription } from 'rxjs';
import { takeUntil } from 'rxjs/operators';
import { SchoolInfoService } from '../../core/services/school-info.service';
import { SchoolAlbumItem } from '../../core/models/school-info.model';

/** A file waiting for / being uploaded */
interface UploadEntry {
  file: File;
  name: string;
  size: number;
  progress: number;
  status: 'waiting' | 'uploading' | 'done' | 'error';
  error?: string;
}

/**
 * School album on the School Info page: upload images (each one is saved by the API in
 * wwwroot/upload/school-album and its URL stored with the school info), see them in a grid, view and delete them.
 */
@Component({
  selector: 'app-school-album',
  standalone: false,
  templateUrl: './school-album.component.html',
  styleUrl: './school-album.component.scss'
})
export class SchoolAlbumComponent implements OnDestroy {
  /** Same rules as the API (SchoolAlbumRules): images 10 MB, videos 100 MB */
  static readonly maxImageBytes = 10 * 1024 * 1024;
  static readonly maxVideoBytes = 100 * 1024 * 1024;
  static readonly imageTypes = ['image/jpeg', 'image/png', 'image/gif', 'image/webp'];
  static readonly videoTypes = ['video/mp4', 'video/webm', 'video/quicktime'];
  /** Browsers do not always give a type (e.g. .mov on Windows): fall back to the extension */
  static readonly videoExtensions = ['.mp4', '.webm', '.mov'];
  static readonly imageExtensions = ['.jpg', '.jpeg', '.png', '.gif', '.webp'];
  readonly accept = '.jpg,.jpeg,.png,.gif,.webp,.mp4,.webm,.mov';

  /** Album items of the school info (from the API) */
  @Input() set items(value: SchoolAlbumItem[] | null | undefined) {
    this.album.set([...(value ?? [])]);
  }

  /** The school info is saved: the album can be used */
  @Input() enabled = false;

  /** The album after an upload or a delete (the page keeps its school info in step) */
  @Output() albumChange = new EventEmitter<SchoolAlbumItem[]>();

  album = signal<SchoolAlbumItem[]>([]);
  uploads = signal<UploadEntry[]>([]);
  isUploading = computed(() => this.uploads().some(u => u.status === 'waiting' || u.status === 'uploading'));
  isDragging = signal(false);
  deletingId = signal<string | null>(null);
  errorMessage = signal<string | null>(null);

  /** Image open in the viewer */
  viewerIndex = signal(0);
  viewerItem = computed(() => this.album()[this.viewerIndex()] ?? null);

  private current?: Subscription;
  private destroy$ = new Subject<void>();

  constructor(
    private schoolInfoService: SchoolInfoService,
    private modalService: NgbModal,
    private translate: TranslateService
  ) {}

  ngOnDestroy(): void {
    this.current?.unsubscribe();
    this.destroy$.next();
    this.destroy$.complete();
  }

  // ----- Picking files -----

  onFilesSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    this.addFiles(Array.from(input.files ?? []));
    input.value = ''; // the same file can be picked again
  }

  onDragOver(event: DragEvent): void {
    event.preventDefault();
    if (this.enabled) {
      this.isDragging.set(true);
    }
  }

  onDragLeave(event: DragEvent): void {
    event.preventDefault();
    this.isDragging.set(false);
  }

  onDrop(event: DragEvent): void {
    event.preventDefault();
    this.isDragging.set(false);
    if (this.enabled) {
      this.addFiles(Array.from(event.dataTransfer?.files ?? []));
    }
  }

  /** Queue the files (wrong type / too large are listed with their error) and start uploading */
  private addFiles(files: File[]): void {
    if (!files.length) {
      return;
    }
    this.errorMessage.set(null);
    const entries = files.map<UploadEntry>(file => {
      const kind = SchoolAlbumComponent.kindOf(file);
      const maxBytes = kind === 'video' ? SchoolAlbumComponent.maxVideoBytes : SchoolAlbumComponent.maxImageBytes;
      const error = !kind
        ? this.translate.instant('schoolInfo.album.errors.FileTypeNotAllowed')
        : file.size > maxBytes
          ? this.translate.instant('schoolInfo.album.errors.FileTooLarge')
          : undefined;
      return { file, name: file.name, size: file.size, progress: 0, status: error ? 'error' : 'waiting', error };
    });
    this.uploads.update(list => [...list.filter(u => u.status !== 'done'), ...entries]);
    this.uploadNext();
  }

  /** Upload the waiting files one by one (each request stays small) */
  private uploadNext(): void {
    if (this.current && !this.current.closed) {
      return;
    }
    const next = this.uploads().find(u => u.status === 'waiting');
    if (!next) {
      return;
    }
    this.patch(next, { status: 'uploading', progress: 0 });

    this.current = this.schoolInfoService.uploadAlbumFile(next.file)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (event) => {
          if (event.type === HttpEventType.UploadProgress && event.total) {
            this.patch(next, { progress: Math.round((event.loaded / event.total) * 100) });
          } else if (event.type === HttpEventType.Response) {
            const added = event.body?.data ?? [];
            this.album.update(list => [...list, ...added]);
            this.albumChange.emit(this.album());
            this.patch(next, { status: 'done', progress: 100 });
          }
        },
        error: (error) => {
          this.patch(next, { status: 'error', error: this.apiError(error) });
          this.current = undefined;
          this.uploadNext();
        },
        complete: () => {
          this.current = undefined;
          this.uploadNext();
        }
      });
  }

  private patch(entry: UploadEntry, changes: Partial<UploadEntry>): void {
    // New array so the list re-renders; the entry object keeps its identity for later updates
    this.uploads.update(list => list.map(u => (u === entry ? Object.assign(u, changes) : u)));
  }

  clearFinished(): void {
    this.uploads.update(list => list.filter(u => u.status === 'waiting' || u.status === 'uploading'));
  }

  // ----- Viewing / deleting -----

  open(index: number, viewer: TemplateRef<unknown>): void {
    this.viewerIndex.set(index);
    this.modalService.open(viewer, { size: 'xl', centered: true, windowClass: 'school-album-viewer' });
  }

  step(delta: number): void {
    const count = this.album().length;
    if (count) {
      this.viewerIndex.set((this.viewerIndex() + delta + count) % count);
    }
  }

  /** viewer: the open image viewer (closed after a confirmed delete) */
  confirmDelete(item: SchoolAlbumItem, dialog: TemplateRef<unknown>, viewer?: { close(): void }): void {
    this.modalService.open(dialog, { centered: true }).result.then(
      (confirmed) => {
        if (confirmed) {
          viewer?.close();
          this.delete(item);
        }
      },
      () => { /* dismissed */ }
    );
  }

  private delete(item: SchoolAlbumItem): void {
    this.deletingId.set(item.id);
    this.errorMessage.set(null);
    this.schoolInfoService.deleteAlbumItem(item.id)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: () => {
          this.album.update(list => list.filter(i => i.id !== item.id));
          this.albumChange.emit(this.album());
          this.deletingId.set(null);
        },
        error: (error) => {
          this.errorMessage.set(this.apiError(error, 'schoolInfo.album.deleteFailed'));
          this.deletingId.set(null);
        }
      });
  }

  // ----- Helpers -----

  /** 'image', 'video', or null when the album does not accept the file */
  static kindOf(file: File): 'image' | 'video' | null {
    const name = file.name.toLowerCase();
    const extension = name.includes('.') ? name.substring(name.lastIndexOf('.')) : '';
    if (SchoolAlbumComponent.videoTypes.includes(file.type) || SchoolAlbumComponent.videoExtensions.includes(extension)) {
      return SchoolAlbumComponent.videoExtensions.includes(extension) ? 'video' : null;
    }
    if (SchoolAlbumComponent.imageTypes.includes(file.type) && SchoolAlbumComponent.imageExtensions.includes(extension)) {
      return 'image';
    }
    return null;
  }

  isVideo(item: SchoolAlbumItem): boolean {
    return item.mediaType === 'video' || (item.contentType ?? '').startsWith('video/');
  }

  src(item: SchoolAlbumItem): string {
    return SchoolInfoService.albumFileUrl(item.url);
  }

  /** 1536000 -> "1.5 MB" */
  formatSize(bytes: number): string {
    if (bytes >= 1024 * 1024) {
      return `${(bytes / (1024 * 1024)).toFixed(1)} MB`;
    }
    return `${Math.max(1, Math.round(bytes / 1024))} KB`;
  }

  /** Message for an API error key (SchoolInfoRequired, FileTooLarge, …) */
  private apiError(error: any, fallbackKey = 'schoolInfo.album.uploadFailed'): string {
    const key = error?.error?.message;
    const translationKey = `schoolInfo.album.errors.${key}`;
    const text = key ? this.translate.instant(translationKey) : null;
    return text && text !== translationKey ? text : this.translate.instant(fallbackKey);
  }
}

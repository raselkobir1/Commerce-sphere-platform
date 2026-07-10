import { Component, inject, model, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Toast } from '../core/toast';
import { Uploads } from '../core/uploads';

// Reusable image field: paste a URL OR upload a file (which goes to Cloudinary via the Product
// service and fills in the URL). Two-way bound with [(value)]; shows a live preview.
//   <app-image-upload [(value)]="form.imageUrl" />
@Component({
  selector: 'app-image-upload',
  imports: [FormsModule],
  template: `
    <div class="img-upload">
      <div class="img-row">
        <input
          class="input"
          type="text"
          name="imgurl"
          [(ngModel)]="value"
          placeholder="Paste an image URL, or upload a file →"
        />
        <label class="btn upload-btn" [class.busy]="uploading()">
          {{ uploading() ? 'Uploading…' : '⬆ Upload' }}
          <input type="file" accept="image/*" hidden (change)="onPick($event)" [disabled]="uploading()" />
        </label>
      </div>
      @if (value()) {
        <div class="img-preview" [style.background-image]="'url(' + value() + ')'"></div>
      }
    </div>
  `,
  styles: [
    `
      .img-row { display: flex; gap: 8px; align-items: stretch; }
      .img-row .input { flex: 1; }
      .upload-btn {
        display: inline-flex; align-items: center; white-space: nowrap; cursor: pointer;
        padding: 0 14px; border: 1px solid var(--line, #d1d5db); border-radius: 8px;
        background: var(--panel, #fff); font-weight: 600;
      }
      .upload-btn.busy { opacity: 0.6; cursor: progress; }
      .img-preview {
        margin-top: 10px; width: 140px; height: 90px; border-radius: 8px;
        border: 1px solid var(--line, #e5e7eb); background-size: cover; background-position: center;
      }
    `,
  ],
})
export class ImageUpload {
  value = model<string>('');

  private uploads = inject(Uploads);
  private toast = inject(Toast);
  uploading = signal(false);

  onPick(event: Event): void {
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0];
    if (!file) return;

    this.uploading.set(true);
    this.uploads.uploadImage(file).subscribe({
      next: (url) => {
        this.value.set(url);
        this.uploading.set(false);
        input.value = '';
        this.toast.success('Image uploaded');
      },
      error: (err: { error?: { message?: string } }) => {
        this.uploading.set(false);
        input.value = '';
        this.toast.error(err?.error?.message ?? 'Image upload failed');
      },
    });
  }
}

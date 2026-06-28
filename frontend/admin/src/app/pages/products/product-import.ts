import { Component, OnDestroy, computed, inject, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { HttpClient } from '@angular/common/http';
import { RouterLink } from '@angular/router';
import { Subscription, switchMap, takeWhile, timer } from 'rxjs';
import { Api, API_URL } from '../../core/api';
import { BulkImportJob } from '../../core/models';

// Bulk product upload: download the template, upload a filled .xlsx, then watch the background
// job progress live (polled every 1.5s) until it finishes. Rejected rows are offered as a
// downloadable error report.
@Component({
  selector: 'app-product-import',
  imports: [DatePipe, RouterLink],
  template: `
    <div class="page-head">
      <div>
        <h1>Bulk product import</h1>
        <div class="sub">Upload up to 100,000 products from an Excel file · uploaded products start as drafts</div>
      </div>
      <a class="btn" routerLink="/products">← Back to products</a>
    </div>

    <!-- Step 1 — template + file picker (hidden once a job is running/finished) -->
    @if (!job()) {
      <div class="card card-pad" style="max-width:680px">
        <ol class="steps">
          <li>
            <strong>Download the template</strong>
            <p class="muted">An .xlsx with the required columns and an instructions sheet.</p>
            <button class="btn" [disabled]="downloadingTemplate()" (click)="downloadTemplate()">
              {{ downloadingTemplate() ? 'Preparing…' : '⬇ Download template' }}
            </button>
          </li>
          <li>
            <strong>Fill it in &amp; upload</strong>
            <p class="muted">One product per row. Duplicate or invalid rows are skipped and reported.</p>
            <div class="uploader">
              <input #picker type="file" accept=".xlsx" hidden (change)="onFile($event)" />
              <button class="btn" (click)="picker.click()">Choose file…</button>
              <span class="file-name">{{ file?.name ?? 'No file selected' }}</span>
            </div>
            <button class="btn btn-primary" style="margin-top:12px" [disabled]="!file || uploading()" (click)="upload()">
              {{ uploading() ? 'Uploading…' : 'Upload &amp; start import' }}
            </button>
          </li>
        </ol>

        @if (error()) { <p class="error">{{ error() }}</p> }
      </div>
    }

    <!-- Step 2 — live progress -->
    @if (job(); as j) {
      <div class="card card-pad" style="max-width:680px">
        <div class="job-head">
          <div>
            <div class="cell-main">{{ j.fileName }}</div>
            <div class="cell-sub">Started {{ j.createdAt | date: 'MMM d, h:mm:ss a' }}</div>
          </div>
          <span class="badge" [class]="statusClass()">{{ statusLabel() }}</span>
        </div>

        <div class="bar" [class.indeterminate]="running()">
          <!-- Total is only known at completion, so the bar animates while running, fills at the end. -->
          <div class="bar-fill" [style.width.%]="100"></div>
        </div>

        <div class="stats">
          <div class="stat"><span class="n">{{ j.processedRows }}</span><span class="l">Processed</span></div>
          <div class="stat ok"><span class="n">{{ j.succeededRows }}</span><span class="l">Imported</span></div>
          <div class="stat bad"><span class="n">{{ j.failedRows }}</span><span class="l">Rejected</span></div>
          @if (j.totalRows > 0) {
            <div class="stat"><span class="n">{{ j.totalRows }}</span><span class="l">Total rows</span></div>
          }
        </div>

        @if (running()) {
          <p class="muted">Importing… you can leave this page; the job keeps running on the server.</p>
        }

        @if (j.status === 'Failed') {
          <p class="error">Import failed: {{ j.errorMessage || 'Unexpected error.' }}</p>
        }

        @if (j.hasErrorReport) {
          <p class="muted">{{ j.failedRows }} row(s) were skipped (duplicate or invalid).</p>
          <button class="btn" [disabled]="downloadingErrors()" (click)="downloadErrors()">
            {{ downloadingErrors() ? 'Preparing…' : '⬇ Download error report' }}
          </button>
        }

        @if (!running()) {
          <div class="row" style="margin-top:16px">
            <button class="btn btn-primary" (click)="reset()">Import another file</button>
            <a class="btn" routerLink="/products">View products</a>
          </div>
        }
      </div>
    }
  `,
  styles: [
    `
      .steps { list-style: none; margin: 0; padding: 0; counter-reset: s; }
      .steps li { position: relative; padding: 0 0 22px 40px; }
      .steps li::before {
        counter-increment: s; content: counter(s);
        position: absolute; left: 0; top: 0; width: 26px; height: 26px;
        display: grid; place-items: center; border-radius: 50%;
        background: var(--accent, #4f46e5); color: #fff; font-size: 13px; font-weight: 600;
      }
      .steps li:not(:last-child)::after {
        content: ''; position: absolute; left: 12.5px; top: 28px; bottom: 4px; width: 1px; background: var(--border, #e5e7eb);
      }
      .steps p { margin: 4px 0 10px; }
      .uploader { display: flex; align-items: center; gap: 12px; }
      .file-name { color: var(--muted, #6b7280); font-size: 13px; }
      .job-head { display: flex; align-items: center; justify-content: space-between; margin-bottom: 14px; }
      .bar { position: relative; height: 10px; border-radius: 6px; background: var(--border, #e5e7eb); overflow: hidden; }
      .bar-fill { height: 100%; background: var(--accent, #4f46e5); border-radius: 6px; transition: width 0.4s ease; }
      .bar.indeterminate .bar-fill {
        width: 35% !important; animation: slide 1.2s ease-in-out infinite;
      }
      @keyframes slide { 0% { margin-left: -35%; } 100% { margin-left: 100%; } }
      .stats { display: flex; gap: 28px; margin: 18px 0 4px; flex-wrap: wrap; }
      .stat { display: flex; flex-direction: column; }
      .stat .n { font-size: 22px; font-weight: 700; }
      .stat .l { font-size: 12px; color: var(--muted, #6b7280); }
      .stat.ok .n { color: #16a34a; }
      .stat.bad .n { color: #dc2626; }
    `,
  ],
})
export class ProductImportPage implements OnDestroy {
  private api = inject(Api);
  private http = inject(HttpClient);

  file: File | null = null;
  job = signal<BulkImportJob | null>(null);
  uploading = signal(false);
  downloadingTemplate = signal(false);
  downloadingErrors = signal(false);
  error = signal('');

  private poll?: Subscription;

  private static readonly TERMINAL = ['Completed', 'CompletedWithErrors', 'Failed'];

  // Job is still being worked on (drives the indeterminate bar and the "leave page" hint).
  running = computed(() => {
    const s = this.job()?.status;
    return s === 'Pending' || s === 'Processing';
  });

  statusLabel = computed(() => {
    switch (this.job()?.status) {
      case 'CompletedWithErrors': return 'Completed with errors';
      case 'Completed': return 'Completed';
      case 'Failed': return 'Failed';
      case 'Processing': return 'Processing…';
      default: return 'Queued…';
    }
  });

  statusClass = computed(() => {
    switch (this.job()?.status) {
      case 'Completed': return 'on';
      case 'CompletedWithErrors': return 'low';
      case 'Failed': return 'off';
      default: return 'low';
    }
  });

  onFile(e: Event): void {
    this.file = (e.target as HTMLInputElement).files?.[0] ?? null;
    this.error.set('');
  }

  upload(): void {
    if (!this.file) return;
    this.uploading.set(true);
    this.error.set('');

    const fd = new FormData();
    fd.append('file', this.file, this.file.name);

    this.api.post<BulkImportJob>('/api/products/import', fd, { toastError: false }).subscribe({
      next: (j) => {
        this.uploading.set(false);
        this.job.set(j);
        this.startPolling(j.jobId);
      },
      error: (err: { error?: { message?: string } }) => {
        this.uploading.set(false);
        this.error.set(err?.error?.message ?? 'Upload failed. Please check the file and try again.');
      },
    });
  }

  private startPolling(jobId: string): void {
    this.poll?.unsubscribe();
    // Poll every 1.5s; takeWhile(..., true) emits the final terminal state then completes.
    this.poll = timer(0, 1500)
      .pipe(
        switchMap(() => this.api.get<BulkImportJob>(`/api/products/import/${jobId}`)),
        takeWhile((j) => !ProductImportPage.TERMINAL.includes(j.status), true),
      )
      .subscribe({
        next: (j) => this.job.set(j),
        error: () => this.error.set('Lost connection while tracking the import.'),
      });
  }

  downloadTemplate(): void {
    this.downloadingTemplate.set(true);
    this.http
      .get(`${API_URL}/api/products/import/template`, { responseType: 'blob' })
      .subscribe({
        next: (blob) => {
          this.saveBlob(blob, 'product-import-template.xlsx');
          this.downloadingTemplate.set(false);
        },
        error: () => {
          this.downloadingTemplate.set(false);
          this.error.set('Could not download the template.');
        },
      });
  }

  downloadErrors(): void {
    const id = this.job()?.jobId;
    if (!id) return;
    this.downloadingErrors.set(true);
    this.http
      .get(`${API_URL}/api/products/import/${id}/errors`, { responseType: 'blob' })
      .subscribe({
        next: (blob) => {
          this.saveBlob(blob, `import-errors-${id}.xlsx`);
          this.downloadingErrors.set(false);
        },
        error: () => {
          this.downloadingErrors.set(false);
          this.error.set('Could not download the error report.');
        },
      });
  }

  reset(): void {
    this.poll?.unsubscribe();
    this.file = null;
    this.job.set(null);
    this.error.set('');
  }

  private saveBlob(blob: Blob, fileName: string): void {
    const url = URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.href = url;
    a.download = fileName;
    a.click();
    URL.revokeObjectURL(url);
  }

  ngOnDestroy(): void {
    this.poll?.unsubscribe();
  }
}

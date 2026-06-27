import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Api } from '../../core/api';
import { Perms } from '../../core/perms';
import { Banner } from '../../core/models';
import { Pagination } from '../../shared/pagination';

@Component({
  selector: 'app-banners',
  imports: [FormsModule, Pagination],
  template: `
    <div class="page-head">
      <div><h1>Banners</h1><div class="sub">{{ banners().length }} banners — shown in the storefront home-page carousel</div></div>
    </div>

    @if (perms.can('banners', 'create') || form.id) {
      <div class="card card-pad" style="max-width:640px; margin-bottom:18px">
        <h2 style="margin-bottom:14px">{{ form.id ? 'Edit banner' : 'New banner' }}</h2>
        <div class="row">
          <div class="field"><label>Title</label><input class="input" name="bt" [(ngModel)]="form.title" /></div>
          <div class="field" style="max-width:110px"><label>Order</label><input class="input" type="number" name="bo" [(ngModel)]="form.sortOrder" /></div>
        </div>
        <div class="field"><label>Subtitle</label><input class="input" name="bs" [(ngModel)]="form.subtitle" placeholder="Optional tagline shown under the title" /></div>
        <div class="field"><label>Image URL</label><input class="input" name="bi" [(ngModel)]="form.imageUrl" placeholder="https://…" /></div>
        <div class="field"><label>Link URL</label><input class="input" name="bl" [(ngModel)]="form.linkUrl" placeholder="Optional — where the banner links when clicked" /></div>

        @if (form.imageUrl) {
          <div class="field">
            <label>Preview</label>
            <div class="banner-preview" [style.background-image]="'url(' + form.imageUrl + ')'">
              <div class="bp-text">
                <div class="bp-title">{{ form.title || 'Banner title' }}</div>
                @if (form.subtitle) { <div class="bp-sub">{{ form.subtitle }}</div> }
              </div>
            </div>
          </div>
        }

        @if (form.id) {
          <label class="switch-row" style="margin-bottom:14px">
            <span><strong>Active</strong><br /><span class="muted">Inactive banners are hidden from the storefront carousel.</span></span>
            <span class="switch"><input type="checkbox" [(ngModel)]="form.isActive" name="ba" /><i></i></span>
          </label>
        }
        @if (err()) { <p class="error">{{ err() }}</p> }
        <div class="actions" style="justify-content:flex-start">
          <button class="btn btn-primary" (click)="save()" [disabled]="saving()">{{ form.id ? 'Save' : 'Add banner' }}</button>
          @if (form.id) { <button class="btn" (click)="cancel()">Cancel</button> }
        </div>
      </div>
    }

    <div class="card">
      <div class="card-head"><h2>All banners</h2></div>
      @if (banners().length === 0) {
        <div class="empty">No banners yet — add one above.</div>
      } @else {
        <div class="table-wrap">
          <table>
            <thead><tr><th>Banner</th><th>Link</th><th>Order</th><th>Status</th><th class="right">Actions</th></tr></thead>
            <tbody>
              @for (b of paged(); track b.id) {
                <tr>
                  <td><div class="cell-flex">
                    <span class="thumb" [style.background-image]="'url(' + b.imageUrl + ')'"></span>
                    <div><div class="cell-main">{{ b.title }}</div><div class="cell-sub">{{ b.subtitle || '—' }}</div></div>
                  </div></td>
                  <td class="muted">{{ b.linkUrl || '—' }}</td>
                  <td>{{ b.sortOrder }}</td>
                  <td><span class="badge" [class.on]="b.isActive" [class.off]="!b.isActive">{{ b.isActive ? 'Active' : 'Hidden' }}</span></td>
                  <td class="right"><div class="actions">
                    @if (perms.can('banners', 'edit')) { <button class="btn btn-sm" (click)="edit(b)">Edit</button> }
                    @if (perms.can('banners', 'delete')) { <button class="btn btn-sm btn-danger" (click)="remove(b)">Delete</button> }
                  </div></td>
                </tr>
              }
            </tbody>
          </table>
        </div>
        <app-pagination [total]="banners().length" [pageNumber]="pageNumber()" [pageSize]="pageSize()"
                        (pageChange)="pageNumber.set($event)" (pageSizeChange)="changePageSize($event)" />
      }
    </div>
  `,
  styles: [`
    .banner-preview { height:150px; border-radius:12px; background-size:cover; background-position:center; position:relative; overflow:hidden; border:1px solid var(--line); }
    .banner-preview .bp-text { position:absolute; inset:0; display:flex; flex-direction:column; justify-content:center; gap:6px; padding:0 28px; background:linear-gradient(90deg, rgba(0,0,0,.55), rgba(0,0,0,.05)); color:#fff; }
    .banner-preview .bp-title { font-size:22px; font-weight:700; }
    .banner-preview .bp-sub { font-size:14px; opacity:.9; }
  `],
})
export class BannersPage implements OnInit {
  private api = inject(Api);
  perms = inject(Perms);

  banners = signal<Banner[]>([]);
  form: { id?: string; title: string; subtitle: string; imageUrl: string; linkUrl: string; isActive: boolean; sortOrder: number } = this.blank();
  saving = signal(false);
  err = signal('');

  pageNumber = signal(1);
  pageSize = signal(20);
  paged = computed(() => {
    const start = (this.pageNumber() - 1) * this.pageSize();
    return this.banners().slice(start, start + this.pageSize());
  });

  changePageSize(size: number): void { this.pageSize.set(size); this.pageNumber.set(1); }

  private blank() { return { title: '', subtitle: '', imageUrl: '', linkUrl: '', isActive: true, sortOrder: 0 }; }

  ngOnInit(): void { this.load(); }
  load(): void { this.api.get<Banner[]>('/api/banners').subscribe((b) => this.banners.set(b)); }

  save(): void {
    this.err.set('');
    if (!this.form.title.trim()) { this.err.set('Title is required.'); return; }
    if (!this.form.imageUrl.trim()) { this.err.set('Image URL is required.'); return; }
    this.saving.set(true);
    const body = {
      title: this.form.title, subtitle: this.form.subtitle, imageUrl: this.form.imageUrl,
      linkUrl: this.form.linkUrl, sortOrder: +this.form.sortOrder,
    };
    const done = {
      next: () => { this.saving.set(false); this.cancel(); this.load(); },
      error: (e: { error?: { message?: string } }) => { this.saving.set(false); this.err.set(e?.error?.message ?? 'Could not save banner.'); },
    };
    if (this.form.id) {
      this.api.put(`/api/banners/${this.form.id}`, { ...body, isActive: this.form.isActive }).subscribe(done);
    } else {
      this.api.post('/api/banners', body).subscribe(done);
    }
  }

  edit(b: Banner): void {
    this.err.set('');
    this.form = { id: b.id, title: b.title, subtitle: b.subtitle, imageUrl: b.imageUrl, linkUrl: b.linkUrl, isActive: b.isActive, sortOrder: b.sortOrder };
  }

  cancel(): void { this.form = this.blank(); this.err.set(''); }

  remove(b: Banner): void {
    if (!confirm(`Delete banner "${b.title}"?`)) return;
    this.api.delete(`/api/banners/${b.id}`).subscribe({
      next: () => this.load(),
      error: (e) => alert(e?.error?.message ?? 'Could not delete banner.'),
    });
  }
}

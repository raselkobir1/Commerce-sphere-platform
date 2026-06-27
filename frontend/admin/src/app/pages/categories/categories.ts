import { Component, OnInit, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Api } from '../../core/api';
import { Category } from '../../core/models';

@Component({
  selector: 'app-categories',
  imports: [FormsModule],
  template: `
    <div class="page-head">
      <div><h1>Categories</h1><div class="sub">{{ categories().length }} categories</div></div>
    </div>

    <!-- Create / edit form -->
    <div class="card card-pad" style="max-width:560px; margin-bottom:18px">
      <h2 style="margin-bottom:14px">{{ form.id ? 'Edit category' : 'New category' }}</h2>
      <div class="field"><label>Name</label><input class="input" name="cn" [(ngModel)]="form.name" /></div>
      <div class="field"><label>Description</label><input class="input" name="cd" [(ngModel)]="form.description" /></div>
      @if (form.id) {
        <label class="switch-row" style="margin-bottom:14px">
          <span><strong>Active</strong><br /><span class="muted">Inactive categories are hidden from the product dropdown.</span></span>
          <span class="switch"><input type="checkbox" [(ngModel)]="form.isActive" name="ca" /><i></i></span>
        </label>
      }
      @if (err()) { <p class="error">{{ err() }}</p> }
      <div class="actions" style="justify-content:flex-start">
        <button class="btn btn-primary" (click)="save()" [disabled]="saving()">
          {{ saving() ? 'Saving…' : (form.id ? 'Save changes' : 'Add category') }}
        </button>
        @if (form.id) { <button class="btn" (click)="cancel()">Cancel</button> }
      </div>
    </div>

    <!-- List -->
    <div class="card">
      <div class="card-head"><h2>All categories</h2></div>
      @if (categories().length === 0) {
        <div class="empty">No categories yet — add one above.</div>
      } @else {
        <div class="table-wrap">
          <table>
            <thead><tr><th>Name</th><th>Description</th><th>Status</th><th class="right">Actions</th></tr></thead>
            <tbody>
              @for (c of categories(); track c.id) {
                <tr>
                  <td><span class="chip admin">{{ c.name }}</span></td>
                  <td class="muted">{{ c.description || '—' }}</td>
                  <td><span class="badge" [class.on]="c.isActive" [class.off]="!c.isActive">{{ c.isActive ? 'Active' : 'Hidden' }}</span></td>
                  <td class="right"><div class="actions">
                    <button class="btn btn-sm" (click)="edit(c)">Edit</button>
                    <button class="btn btn-sm btn-danger" (click)="remove(c)">Delete</button>
                  </div></td>
                </tr>
              }
            </tbody>
          </table>
        </div>
      }
    </div>
  `,
})
export class CategoriesPage implements OnInit {
  private api = inject(Api);

  categories = signal<Category[]>([]);
  form: { id?: string; name: string; description: string; isActive: boolean } = { name: '', description: '', isActive: true };
  saving = signal(false);
  err = signal('');

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.api.get<Category[]>('/api/categories').subscribe((c) => this.categories.set(c));
  }

  save(): void {
    this.err.set('');
    if (!this.form.name.trim()) { this.err.set('Name is required.'); return; }
    this.saving.set(true);

    const done = {
      next: () => { this.saving.set(false); this.cancel(); this.load(); },
      error: (e: { error?: { message?: string } }) => { this.saving.set(false); this.err.set(e?.error?.message ?? 'Could not save category.'); },
    };

    if (this.form.id) {
      this.api.put(`/api/categories/${this.form.id}`, { name: this.form.name, description: this.form.description, isActive: this.form.isActive }).subscribe(done);
    } else {
      this.api.post('/api/categories', { name: this.form.name, description: this.form.description }).subscribe(done);
    }
  }

  edit(c: Category): void {
    this.err.set('');
    this.form = { id: c.id, name: c.name, description: c.description, isActive: c.isActive };
  }

  cancel(): void {
    this.form = { name: '', description: '', isActive: true };
    this.err.set('');
  }

  remove(c: Category): void {
    if (!confirm(`Delete category "${c.name}"? Existing products keep their category text.`)) return;
    this.api.delete(`/api/categories/${c.id}`).subscribe(() => this.load());
  }
}

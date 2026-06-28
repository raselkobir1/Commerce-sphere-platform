import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Api } from '../../core/api';
import { Perms } from '../../core/perms';
import { Category } from '../../core/models';
import { Pagination } from '../../shared/pagination';

@Component({
  selector: 'app-categories',
  imports: [FormsModule, Pagination],
  template: `
    <div class="page-head">
      <div><h1>Categories</h1><div class="sub">{{ categories().length }} categories — used by the storefront navigation</div></div>
    </div>

    @if (perms.can('categories', 'create') || perms.can('categories', 'edit')) {
    <div class="card card-pad" style="max-width:600px; margin-bottom:18px">
      <h2 style="margin-bottom:14px">{{ form.id ? 'Edit category' : 'New category' }}</h2>
      <div class="row">
        <div class="field"><label>Name</label><input class="input" name="cn" [(ngModel)]="form.name" /></div>
        <div class="field" style="max-width:110px"><label>Order</label><input class="input" type="number" name="co" [(ngModel)]="form.sortOrder" /></div>
      </div>
      <div class="field">
        <label>Parent category</label>
        <select name="cp" [(ngModel)]="form.parentId">
          <option [ngValue]="null">— None (top-level category) —</option>
          @for (p of parentOptions(); track p.id) { <option [ngValue]="p.id">{{ p.name }}</option> }
        </select>
        <div class="cell-sub" style="margin-top:6px">Pick a parent to make this a sub-category; leave “None” for a top-level category.</div>
      </div>
      <div class="field"><label>Description</label><input class="input" name="cd" [(ngModel)]="form.description" /></div>
      @if (form.id) {
        <label class="switch-row" style="margin-bottom:14px">
          <span><strong>Active</strong><br /><span class="muted">Inactive categories are hidden from the storefront and product dropdown.</span></span>
          <span class="switch"><input type="checkbox" [(ngModel)]="form.isActive" name="ca" /><i></i></span>
        </label>
      }
      @if (err()) { <p class="error">{{ err() }}</p> }
      <div class="actions" style="justify-content:flex-start">
        <button class="btn btn-primary" (click)="save()" [disabled]="saving()">{{ form.id ? 'Save' : 'Add category' }}</button>
        @if (form.id) { <button class="btn" (click)="cancel()">Cancel</button> }
      </div>
    </div>
    }

    <div class="card">
      <div class="card-head"><h2>All categories</h2></div>
      @if (categories().length === 0) {
        <div class="empty">No categories yet — add one above.</div>
      } @else {
        <div class="table-wrap">
          <table>
            <thead><tr><th>Category</th><th>Description</th><th>Status</th><th class="right">Actions</th></tr></thead>
            <tbody>
              @for (c of pagedCategories(); track c.id) {
                <tr>
                  <td [style.padding-left.px]="c.level ? 36 : 20">
                    @if (c.level) { <span class="muted" style="margin-right:4px">↳</span> }
                    <span class="chip" [class.admin]="!c.level">{{ c.name }}</span>
                  </td>
                  <td class="muted">{{ c.description || '—' }}</td>
                  <td><span class="badge" [class.on]="c.isActive" [class.off]="!c.isActive">{{ c.isActive ? 'Active' : 'Hidden' }}</span></td>
                  <td class="right"><div class="actions">
                    @if (perms.can('categories', 'edit')) { <button class="btn btn-sm" (click)="edit(c)">Edit</button> }
                    @if (perms.can('categories', 'delete')) { <button class="btn btn-sm btn-danger" (click)="remove(c)">Delete</button> }
                  </div></td>
                </tr>
              }
            </tbody>
          </table>
        </div>
        <app-pagination [total]="ordered().length" [pageNumber]="pageNumber()" [pageSize]="pageSize()"
                        (pageChange)="pageNumber.set($event)" (pageSizeChange)="changePageSize($event)" />
      }
    </div>
  `,
})
export class CategoriesPage implements OnInit {
  private api = inject(Api);
  perms = inject(Perms);

  categories = signal<Category[]>([]);
  form: { id?: string; name: string; description: string; isActive: boolean; parentId: string | null; sortOrder: number } = this.blank();
  saving = signal(false);
  err = signal('');

  private blank() { return { name: '', description: '', isActive: true, parentId: null as string | null, sortOrder: 0 }; }

  parentOptions = computed(() => this.categories().filter((c) => !c.parentId && c.id !== this.form.id));

  ordered = computed(() => {
    const all = this.categories();
    const order = (a: Category, b: Category) => a.sortOrder - b.sortOrder || a.name.localeCompare(b.name);
    const tops = all.filter((c) => !c.parentId).sort(order);
    const out: (Category & { level: number })[] = [];
    for (const t of tops) {
      out.push({ ...t, level: 0 });
      all.filter((c) => c.parentId === t.id).sort(order).forEach((c) => out.push({ ...c, level: 1 }));
    }
    return out;
  });

  // Client-side pagination over the flattened parent→child list.
  pageNumber = signal(1);
  pageSize = signal(20);
  pagedCategories = computed(() => {
    const start = (this.pageNumber() - 1) * this.pageSize();
    return this.ordered().slice(start, start + this.pageSize());
  });

  changePageSize(size: number): void { this.pageSize.set(size); this.pageNumber.set(1); }

  ngOnInit(): void { this.load(); }
  load(): void { this.api.get<Category[]>('/api/categories').subscribe((c) => this.categories.set(c)); }

  save(): void {
    this.err.set('');
    if (!this.form.name.trim()) { this.err.set('Name is required.'); return; }
    this.saving.set(true);
    const done = {
      next: () => { this.saving.set(false); this.cancel(); this.load(); },
      error: (e: { error?: { message?: string } }) => { this.saving.set(false); this.err.set(e?.error?.message ?? 'Could not save category.'); },
    };
    if (this.form.id) {
      this.api.put(`/api/categories/${this.form.id}`, { name: this.form.name, description: this.form.description, isActive: this.form.isActive, parentId: this.form.parentId || null, sortOrder: +this.form.sortOrder }).subscribe(done);
    } else {
      this.api.post('/api/categories', { name: this.form.name, description: this.form.description, parentId: this.form.parentId || null, sortOrder: +this.form.sortOrder }).subscribe(done);
    }
  }

  edit(c: Category): void {
    this.err.set('');
    this.form = { id: c.id, name: c.name, description: c.description, isActive: c.isActive, parentId: c.parentId ?? null, sortOrder: c.sortOrder };
  }

  cancel(): void { this.form = this.blank(); this.err.set(''); }

  remove(c: Category): void {
    if (!confirm(`Delete category "${c.name}"? Existing products keep their category text.`)) return;
    this.api.delete(`/api/categories/${c.id}`).subscribe({
      next: () => this.load(),
      error: (e) => alert(e?.error?.message ?? 'Could not delete category.'),
    });
  }
}

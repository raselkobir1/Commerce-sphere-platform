import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Api } from '../../core/api';
import { Perms } from '../../core/perms';
import { Menu } from '../../core/models';

@Component({
  selector: 'app-menus',
  imports: [FormsModule],
  template: `
    <div class="page-head"><div><h1>Menus</h1><div class="sub">{{ menus().length }} navigation items</div></div></div>

    @if (perms.can('menus', 'create') || form.id) {
      <div class="card card-pad" style="max-width:640px; margin-bottom:18px">
        <h2 style="margin-bottom:14px">{{ form.id ? 'Edit menu' : 'New menu' }}</h2>
        <div class="row">
          <div class="field"><label>Key</label><input class="input" name="mk" [(ngModel)]="form.key" [disabled]="!!form.id" placeholder="e.g. reports" /></div>
          <div class="field"><label>Label</label><input class="input" name="ml" [(ngModel)]="form.label" /></div>
        </div>
        <div class="field">
          <label>Parent menu</label>
          <select name="mp" [(ngModel)]="form.parentId">
            <option [ngValue]="null">— None (top-level menu) —</option>
            @for (p of parentOptions(); track p.id) { <option [ngValue]="p.id">{{ p.label }}</option> }
          </select>
          <div class="cell-sub" style="margin-top:6px">Pick a parent to make this a child menu; leave as “None” for a top-level menu.</div>
        </div>
        <div class="row">
          <div class="field"><label>Route</label><input class="input" name="mr" [(ngModel)]="form.route" placeholder="/reports" /></div>
          <div class="field" style="max-width:120px"><label>Icon</label><input class="input" name="mi" [(ngModel)]="form.icon" placeholder="📈" /></div>
          <div class="field" style="max-width:110px"><label>Order</label><input class="input" type="number" name="mo" [(ngModel)]="form.sortOrder" /></div>
        </div>
        @if (err()) { <p class="error">{{ err() }}</p> }
        <div class="actions" style="justify-content:flex-start">
          <button class="btn btn-primary" (click)="save()" [disabled]="saving()">{{ form.id ? 'Save' : 'Add menu' }}</button>
          @if (form.id) { <button class="btn" (click)="cancel()">Cancel</button> }
        </div>
      </div>
    }

    <div class="card">
      <div class="card-head"><h2>All menus</h2></div>
      <div class="table-wrap">
        <table>
          <thead><tr><th>Order</th><th>Menu</th><th>Route</th><th>Key</th><th class="right">Actions</th></tr></thead>
          <tbody>
            @for (m of ordered(); track m.id) {
              <tr>
                <td>{{ m.sortOrder }}</td>
                <td class="cell-main" [style.padding-left.px]="20 + m.level * 24">
                  @if (m.level) { <span class="muted" style="margin-right:4px">↳</span> }
                  <span class="nav-emoji" style="margin-right:6px">{{ m.icon }}</span>{{ m.label }}
                </td>
                <td class="muted">{{ m.route }}</td>
                <td><span class="chip">{{ m.key }}</span></td>
                <td class="right"><div class="actions">
                  @if (perms.can('menus', 'edit')) { <button class="btn btn-sm" (click)="edit(m)">Edit</button> }
                  @if (perms.can('menus', 'delete')) { <button class="btn btn-sm btn-danger" (click)="remove(m)">Delete</button> }
                </div></td>
              </tr>
            }
          </tbody>
        </table>
      </div>
    </div>
  `,
})
export class MenusPage implements OnInit {
  private api = inject(Api);
  perms = inject(Perms);

  menus = signal<Menu[]>([]);
  editingId = signal<string | null>(null);
  form: { id?: string; key: string; label: string; route: string; icon: string; sortOrder: number; parentId: string | null } = this.blank();
  saving = signal(false);
  err = signal('');

  private blank() { return { key: '', label: '', route: '', icon: '', sortOrder: 0, parentId: null as string | null }; }

  // Any menu can be a parent (unlimited depth) — except the menu being edited and its own
  // descendants (which would create a cycle).
  parentOptions = computed(() => {
    const all = this.menus();
    const selfId = this.editingId();
    const blocked = selfId ? this.descendantIds(selfId, all) : new Set<string>();
    return all.filter((m) => m.id !== selfId && !blocked.has(m.id));
  });

  // Flattened depth-first list with an indentation level per row (handles any nesting depth).
  ordered = computed(() => {
    const all = this.menus();
    const order = (a: Menu, b: Menu) => a.sortOrder - b.sortOrder || a.label.localeCompare(b.label);
    const childrenOf = (pid: string | null) => all.filter((m) => (m.parentId ?? null) === pid).sort(order);
    const out: (Menu & { level: number })[] = [];
    const walk = (m: Menu, level: number) => { out.push({ ...m, level }); childrenOf(m.id).forEach((c) => walk(c, level + 1)); };
    childrenOf(null).forEach((r) => walk(r, 0));
    const placed = new Set(out.map((o) => o.id));
    all.filter((m) => !placed.has(m.id)).forEach((m) => out.push({ ...m, level: 0 })); // orphans
    return out;
  });

  private descendantIds(id: string, all: Menu[]): Set<string> {
    const set = new Set<string>();
    const add = (pid: string) => { for (const m of all) if (m.parentId === pid && !set.has(m.id)) { set.add(m.id); add(m.id); } };
    add(id);
    return set;
  }

  ngOnInit(): void { this.load(); }
  load(): void { this.api.get<Menu[]>('/api/auth/menus').subscribe((m) => this.menus.set(m)); }

  save(): void {
    this.err.set('');
    if (!this.form.label.trim() || (!this.form.id && !this.form.key.trim())) { this.err.set('Key and label are required.'); return; }
    this.saving.set(true);
    const base = { label: this.form.label, route: this.form.route, icon: this.form.icon, sortOrder: +this.form.sortOrder, parentId: this.form.parentId || null };
    const done = {
      next: () => { this.saving.set(false); this.cancel(); this.load(); this.perms.load().subscribe(); },
      error: (e: { error?: { message?: string } }) => { this.saving.set(false); this.err.set(e?.error?.message ?? 'Could not save menu.'); },
    };
    if (this.form.id) this.api.put(`/api/auth/menus/${this.form.id}`, base).subscribe(done);
    else this.api.post('/api/auth/menus', { key: this.form.key, ...base }).subscribe(done);
  }

  edit(m: Menu): void {
    this.err.set('');
    this.editingId.set(m.id);
    this.form = { id: m.id, key: m.key, label: m.label, route: m.route, icon: m.icon, sortOrder: m.sortOrder, parentId: m.parentId ?? null };
  }

  cancel(): void { this.form = this.blank(); this.editingId.set(null); this.err.set(''); }

  remove(m: Menu): void {
    if (!confirm(`Delete menu "${m.label}"?`)) return;
    this.api.delete(`/api/auth/menus/${m.id}`).subscribe({
      next: () => { this.load(); this.perms.load().subscribe(); },
      error: (e) => alert(e?.error?.message ?? 'Could not delete menu.'),
    });
  }
}

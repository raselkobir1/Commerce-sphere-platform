import { Component, OnInit, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Api } from '../../core/api';
import { Perms } from '../../core/perms';
import { Role } from '../../core/models';

@Component({
  selector: 'app-roles',
  imports: [FormsModule],
  template: `
    <div class="page-head"><div><h1>Roles</h1><div class="sub">{{ roles().length }} roles</div></div></div>

    @if (perms.can('roles', 'create') || form.id) {
      <div class="card card-pad" style="max-width:560px; margin-bottom:18px">
        <h2 style="margin-bottom:14px">{{ form.id ? 'Edit role' : 'New role' }}</h2>
        <div class="field"><label>Name</label><input class="input" name="rn" [(ngModel)]="form.name" [disabled]="form.isSystem" /></div>
        <div class="field"><label>Description</label><input class="input" name="rd" [(ngModel)]="form.description" /></div>
        @if (err()) { <p class="error">{{ err() }}</p> }
        <div class="actions" style="justify-content:flex-start">
          <button class="btn btn-primary" (click)="save()" [disabled]="saving()">{{ form.id ? 'Save' : 'Add role' }}</button>
          @if (form.id) { <button class="btn" (click)="cancel()">Cancel</button> }
        </div>
      </div>
    }

    <div class="card">
      <div class="card-head"><h2>All roles</h2></div>
      <div class="table-wrap">
        <table>
          <thead><tr><th>Name</th><th>Description</th><th>Type</th><th class="right">Actions</th></tr></thead>
          <tbody>
            @for (r of roles(); track r.id) {
              <tr>
                <td><span class="chip admin">{{ r.name }}</span> @if (r.isDefault) { <span class="cell-sub">default</span> }</td>
                <td class="muted">{{ r.description || '—' }}</td>
                <td>{{ r.isSystem ? 'System' : 'Custom' }}</td>
                <td class="right"><div class="actions">
                  @if (perms.can('roles', 'edit')) { <button class="btn btn-sm" (click)="edit(r)">Edit</button> }
                  @if (perms.can('roles', 'delete') && !r.isSystem) { <button class="btn btn-sm btn-danger" (click)="remove(r)">Delete</button> }
                </div></td>
              </tr>
            }
          </tbody>
        </table>
      </div>
    </div>
  `,
})
export class RolesPage implements OnInit {
  private api = inject(Api);
  perms = inject(Perms);

  roles = signal<Role[]>([]);
  form: { id?: string; name: string; description: string; isSystem: boolean } = { name: '', description: '', isSystem: false };
  saving = signal(false);
  err = signal('');

  ngOnInit(): void { this.load(); }
  load(): void { this.api.get<Role[]>('/api/auth/roles').subscribe((r) => this.roles.set(r)); }

  save(): void {
    this.err.set('');
    if (!this.form.name.trim()) { this.err.set('Name is required.'); return; }
    this.saving.set(true);
    const body = { name: this.form.name, description: this.form.description };
    const done = {
      next: () => { this.saving.set(false); this.cancel(); this.load(); },
      error: (e: { error?: { message?: string } }) => { this.saving.set(false); this.err.set(e?.error?.message ?? 'Could not save role.'); },
    };
    if (this.form.id) this.api.put(`/api/auth/roles/${this.form.id}`, body).subscribe(done);
    else this.api.post('/api/auth/roles', body).subscribe(done);
  }

  edit(r: Role): void { this.err.set(''); this.form = { id: r.id, name: r.name, description: r.description, isSystem: r.isSystem }; }
  cancel(): void { this.form = { name: '', description: '', isSystem: false }; this.err.set(''); }

  remove(r: Role): void {
    if (!confirm(`Delete role "${r.name}"?`)) return;
    this.api.delete(`/api/auth/roles/${r.id}`).subscribe({
      next: () => this.load(),
      error: (e) => alert(e?.error?.message ?? 'Could not delete role.'),
    });
  }
}

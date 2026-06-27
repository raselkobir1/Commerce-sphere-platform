import { Component, OnInit, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Api } from '../../core/api';
import { Perms } from '../../core/perms';
import { MenuPermission, Role } from '../../core/models';

@Component({
  selector: 'app-permissions',
  imports: [FormsModule],
  template: `
    <div class="page-head"><div><h1>Permissions</h1><div class="sub">Grant each role access to menus and actions</div></div></div>

    <div class="card card-pad" style="margin-bottom:18px; max-width:420px">
      <div class="field" style="margin:0">
        <label>Role</label>
        <select [(ngModel)]="roleId" (ngModelChange)="loadPerms()">
          <option value="" disabled>Select a role…</option>
          @for (r of roles(); track r.id) { <option [value]="r.id">{{ r.name }}</option> }
        </select>
      </div>
    </div>

    @if (roleId) {
      <div class="card">
        <div class="card-head">
          <h2>Menu permissions</h2>
          @if (perms.can('permissions', 'edit')) {
            <button class="btn btn-primary btn-sm" (click)="save()" [disabled]="saving()">{{ saving() ? 'Saving…' : 'Save changes' }}</button>
          }
        </div>
        <div class="table-wrap">
          <table>
            <thead><tr><th>Menu</th><th class="ctr">View</th><th class="ctr">Create</th><th class="ctr">Edit</th><th class="ctr">Delete</th></tr></thead>
            <tbody>
              @for (row of rows(); track row.menuId) {
                <tr>
                  <td class="cell-main" [style.padding-left.px]="row.parentId ? 40 : 20">
                    @if (row.parentId) { <span class="muted" style="margin-right:4px">↳</span> }
                    <span class="nav-emoji" style="margin-right:6px">{{ row.icon }}</span>{{ row.label }}
                  </td>
                  <td class="ctr"><input type="checkbox" [(ngModel)]="row.canView" /></td>
                  <td class="ctr"><input type="checkbox" [(ngModel)]="row.canCreate" /></td>
                  <td class="ctr"><input type="checkbox" [(ngModel)]="row.canEdit" /></td>
                  <td class="ctr"><input type="checkbox" [(ngModel)]="row.canDelete" /></td>
                </tr>
              }
            </tbody>
          </table>
        </div>
      </div>
      @if (msg()) { <p class="note-ok">{{ msg() }}</p> }
    }
  `,
  styles: [`.ctr { text-align: center; } .ctr input { width: 17px; height: 17px; cursor: pointer; }`],
})
export class PermissionsPage implements OnInit {
  private api = inject(Api);
  perms = inject(Perms);

  roles = signal<Role[]>([]);
  roleId = '';
  rows = signal<MenuPermission[]>([]);
  saving = signal(false);
  msg = signal('');

  ngOnInit(): void {
    this.api.get<Role[]>('/api/auth/roles').subscribe((r) => this.roles.set(r));
  }

  loadPerms(): void {
    this.msg.set('');
    if (!this.roleId) return;
    this.api.get<MenuPermission[]>(`/api/auth/roles/${this.roleId}/permissions`).subscribe((p) => this.rows.set(p));
  }

  save(): void {
    this.saving.set(true);
    this.msg.set('');
    const permissions = this.rows().map((r) => ({
      menuId: r.menuId, canView: r.canView, canCreate: r.canCreate, canEdit: r.canEdit, canDelete: r.canDelete,
    }));
    this.api.put(`/api/auth/roles/${this.roleId}/permissions`, { permissions }).subscribe({
      next: () => { this.saving.set(false); this.msg.set('Permissions saved.'); this.perms.load().subscribe(); },
      error: () => { this.saving.set(false); this.msg.set('Could not save permissions.'); },
    });
  }
}

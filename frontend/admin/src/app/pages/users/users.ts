import { Component, OnInit, inject, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Api } from '../../core/api';
import { Perms } from '../../core/perms';
import { Paged, Role, User } from '../../core/models';
import { Pagination } from '../../shared/pagination';

@Component({
  selector: 'app-users',
  imports: [DatePipe, FormsModule, Pagination],
  template: `
    <div class="page-head"><div><h1>Users</h1><div class="sub">{{ total() }} users</div></div></div>

    @if (perms.can('users', 'create') || form.id) {
      <div class="card card-pad" style="max-width:680px; margin-bottom:18px">
        <h2 style="margin-bottom:14px">{{ form.id ? 'Edit user' : 'New user' }}</h2>
        @if (!form.id) {
          <div class="row">
            <div class="field"><label>First name</label><input class="input" name="fn" [(ngModel)]="form.firstName" /></div>
            <div class="field"><label>Last name</label><input class="input" name="ln" [(ngModel)]="form.lastName" /></div>
          </div>
          <div class="row">
            <div class="field"><label>Email</label><input class="input" type="email" name="em" [(ngModel)]="form.email" /></div>
            <div class="field"><label>Password</label><input class="input" type="password" name="pw" [(ngModel)]="form.password" /></div>
          </div>
        } @else {
          <p class="muted" style="margin-bottom:12px">{{ form.firstName }} {{ form.lastName }} · {{ form.email }}</p>
        }
        <div class="row">
          <div class="field"><label>Role</label>
            <select name="rl" [(ngModel)]="form.role">
              <option value="" disabled>Select role…</option>
              @for (r of roles(); track r.id) { <option [value]="r.name">{{ r.name }}</option> }
            </select>
          </div>
          @if (form.id) {
            <div class="field"><label>Status</label>
              <select name="st" [(ngModel)]="form.isActive">
                <option [ngValue]="true">Active</option>
                <option [ngValue]="false">Disabled</option>
              </select>
            </div>
          }
        </div>
        @if (err()) { <p class="error">{{ err() }}</p> }
        <div class="actions" style="justify-content:flex-start">
          <button class="btn btn-primary" (click)="save()" [disabled]="saving()">{{ form.id ? 'Save' : 'Create user' }}</button>
          @if (form.id) { <button class="btn" (click)="cancel()">Cancel</button> }
        </div>
      </div>
    }

    <div class="card">
      <div class="card-head"><h2>All users</h2></div>
      <div class="table-wrap">
        <table>
          <thead><tr><th>User</th><th>Role</th><th>Status</th><th>Joined</th><th class="right">Actions</th></tr></thead>
          <tbody>
            @for (u of users(); track u.id) {
              <tr>
                <td><div class="cell-flex">
                  <span class="avatar sm">{{ initials(u) }}</span>
                  <div><div class="cell-main">{{ u.firstName }} {{ u.lastName }}</div><div class="cell-sub">{{ u.email }}</div></div>
                </div></td>
                <td><span class="chip" [class.admin]="u.role === 'Admin'">{{ u.role }}</span></td>
                <td><span class="badge" [class.on]="u.isActive" [class.off]="!u.isActive">{{ u.isActive ? 'Active' : 'Disabled' }}</span></td>
                <td class="muted">{{ u.createdAt | date: 'mediumDate' }}</td>
                <td class="right"><div class="actions">
                  @if (perms.can('users', 'edit')) { <button class="btn btn-sm" (click)="edit(u)">Edit</button> }
                  @if (perms.can('users', 'delete')) { <button class="btn btn-sm btn-danger" (click)="remove(u)">Delete</button> }
                </div></td>
              </tr>
            }
          </tbody>
        </table>
      </div>
      <app-pagination [total]="total()" [pageNumber]="pageNumber()" [pageSize]="pageSize()"
                      (pageChange)="goTo($event)" (pageSizeChange)="changePageSize($event)" />
    </div>
  `,
})
export class UsersPage implements OnInit {
  private api = inject(Api);
  perms = inject(Perms);

  users = signal<User[]>([]);
  roles = signal<Role[]>([]);
  pageNumber = signal(1);
  pageSize = signal(20);
  total = signal(0);
  form: { id?: string; firstName: string; lastName: string; email: string; password: string; role: string; isActive: boolean } =
    this.blank();
  saving = signal(false);
  err = signal('');

  private blank() { return { firstName: '', lastName: '', email: '', password: '', role: '', isActive: true }; }

  initials(u: User): string {
    return ((u.firstName?.[0] ?? '') + (u.lastName?.[0] ?? '')).toUpperCase() || 'U';
  }

  ngOnInit(): void {
    this.load();
    this.api.get<Role[]>('/api/auth/roles').subscribe((r) => this.roles.set(r));
  }

  load(): void {
    this.api.get<Paged<User>>('/api/auth/users', { pageNumber: this.pageNumber(), pageSize: this.pageSize() })
      .subscribe((r) => { this.users.set(r.items); this.total.set(r.totalRecords); });
  }

  goTo(page: number): void { this.pageNumber.set(page); this.load(); }
  changePageSize(size: number): void { this.pageSize.set(size); this.pageNumber.set(1); this.load(); }

  save(): void {
    this.err.set('');
    if (!this.form.role) { this.err.set('Please choose a role.'); return; }
    this.saving.set(true);
    const done = {
      next: () => { this.saving.set(false); this.cancel(); this.load(); },
      error: (e: { error?: { message?: string } }) => { this.saving.set(false); this.err.set(e?.error?.message ?? 'Could not save user.'); },
    };
    if (this.form.id) {
      this.api.put(`/api/auth/users/${this.form.id}`, { role: this.form.role, isActive: this.form.isActive }).subscribe(done);
    } else {
      this.api.post('/api/auth/users', {
        email: this.form.email, password: this.form.password,
        firstName: this.form.firstName, lastName: this.form.lastName, role: this.form.role,
      }).subscribe(done);
    }
  }

  edit(u: User): void {
    this.err.set('');
    this.form = { id: u.id, firstName: u.firstName, lastName: u.lastName, email: u.email, password: '', role: u.role, isActive: u.isActive };
  }

  cancel(): void { this.form = this.blank(); this.err.set(''); }

  remove(u: User): void {
    if (!confirm(`Delete user "${u.email}"?`)) return;
    this.api.delete(`/api/auth/users/${u.id}`).subscribe({
      next: () => this.load(),
      error: (e) => alert(e?.error?.message ?? 'Could not delete user.'),
    });
  }
}

import { Component, OnInit, inject, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { Api } from '../../core/api';
import { Paged, User } from '../../core/models';

// The Auth service exposes a read-only admin listing of users (/api/auth/users).
@Component({
  selector: 'app-users',
  imports: [DatePipe],
  template: `
    <div class="page-head"><div><h1>Users</h1><div class="sub">{{ users().length }} registered users</div></div></div>

    <div class="card">
      @if (loading()) {
        <div class="empty">Loading…</div>
      } @else if (users().length === 0) {
        <div class="empty">No users found.</div>
      } @else {
        <div class="table-wrap">
          <table>
            <thead>
              <tr><th>User</th><th>Role</th><th>Status</th><th>Joined</th></tr>
            </thead>
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
                </tr>
              }
            </tbody>
          </table>
        </div>
      }
    </div>
  `,
})
export class UsersPage implements OnInit {
  private api = inject(Api);

  users = signal<User[]>([]);
  loading = signal(false);

  initials(u: User): string {
    return ((u.firstName?.[0] ?? '') + (u.lastName?.[0] ?? '')).toUpperCase() || 'U';
  }

  ngOnInit(): void {
    this.loading.set(true);
    this.api.get<Paged<User>>('/api/auth/users', { pageNumber: 1, pageSize: 100 }).subscribe({
      next: (r) => {
        this.users.set(r.items);
        this.loading.set(false);
      },
      error: () => this.loading.set(false),
    });
  }
}

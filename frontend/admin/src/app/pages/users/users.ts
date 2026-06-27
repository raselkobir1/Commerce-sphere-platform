import { Component, OnInit, inject, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { Api } from '../../core/api';
import { Paged, User } from '../../core/models';

// The Auth service exposes a read-only admin listing of users (/api/auth/users).
@Component({
  selector: 'app-users',
  imports: [DatePipe],
  template: `
    <div class="page-head"><h1>Users</h1></div>

    <div class="card">
      @if (loading()) {
        <p class="muted">Loading…</p>
      } @else {
        <table>
          <thead>
            <tr><th>Name</th><th>Email</th><th>Role</th><th>Status</th><th>Joined</th></tr>
          </thead>
          <tbody>
            @for (u of users(); track u.id) {
              <tr>
                <td>{{ u.firstName }} {{ u.lastName }}</td>
                <td class="muted">{{ u.email }}</td>
                <td>{{ u.role }}</td>
                <td>
                  <span class="badge" [class.on]="u.isActive" [class.off]="!u.isActive">
                    {{ u.isActive ? 'Active' : 'Disabled' }}
                  </span>
                </td>
                <td class="muted">{{ u.createdAt | date: 'mediumDate' }}</td>
              </tr>
            }
          </tbody>
        </table>
      }
    </div>
  `,
})
export class UsersPage implements OnInit {
  private api = inject(Api);

  users = signal<User[]>([]);
  loading = signal(false);

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

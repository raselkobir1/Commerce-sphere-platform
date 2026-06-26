import { DatePipe } from '@angular/common';
import { Component, OnInit, inject, signal } from '@angular/core';
import { MatChipsModule } from '@angular/material/chips';
import { MatIconModule } from '@angular/material/icon';
import { MatPaginatorModule, PageEvent } from '@angular/material/paginator';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatTableModule } from '@angular/material/table';
import { AuthApiService } from '../../../../core/auth/auth-api.service';
import { User } from '../../../../core/models/auth.models';

@Component({
  selector: 'app-user-list',
  imports: [DatePipe, MatTableModule, MatPaginatorModule, MatChipsModule, MatIconModule, MatProgressBarModule],
  templateUrl: './user-list.html',
  styleUrl: './user-list.scss',
})
export class UserList implements OnInit {
  private readonly authApi = inject(AuthApiService);

  readonly columns = ['name', 'email', 'role', 'status', 'created'];
  readonly users = signal<User[]>([]);
  readonly total = signal(0);
  readonly pageSize = signal(10);
  readonly pageIndex = signal(0);
  readonly loading = signal(false);

  ngOnInit(): void {
    this.load();
  }

  onPage(e: PageEvent): void {
    this.pageIndex.set(e.pageIndex);
    this.pageSize.set(e.pageSize);
    this.load();
  }

  private load(): void {
    this.loading.set(true);
    this.authApi.users(this.pageIndex() + 1, this.pageSize()).subscribe({
      next: (result) => {
        this.users.set(result.items);
        this.total.set(result.totalRecords);
        this.loading.set(false);
      },
      error: () => this.loading.set(false),
    });
  }
}

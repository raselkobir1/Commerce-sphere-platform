import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { Router, RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { Auth } from '../core/auth';
import { Perms } from '../core/perms';
import { Theme } from '../core/theme';
import { Notifications } from '../core/notifications';
import { MenuPermission, Notification } from '../core/models';

// The signed-in layout: icon sidebar + top bar (with the user/appearance menu) + the page.
@Component({
  selector: 'app-shell',
  imports: [RouterOutlet, RouterLink, RouterLinkActive, DatePipe],
  template: `
    <div class="shell">
      <aside class="sidebar">
        <div class="brand"><span class="mark">A</span>Admin<span>Sphere</span></div>

        <div class="nav-label">Menu</div>
        <nav class="nav">
          @for (m of visibleMenu(); track m.menuKey) {
            <a [routerLink]="m.route" routerLinkActive="active" [style.padding-left.px]="14 + m.level * 16"
               (click)="m.hasChildren && toggle(m.menuId)">
              <span class="nav-emoji">{{ m.icon }}</span>
              <span class="nav-text">{{ m.label }}</span>
              @if (m.hasChildren) { <span class="chev" [class.open]="m.open">▸</span> }
            </a>
          }
        </nav>

        <div class="sidebar-foot">
          <div class="user-card">
            <span class="avatar sm">{{ initials() }}</span>
            <div>
              <div class="nm">{{ auth.user()?.firstName }} {{ auth.user()?.lastName }}</div>
              <div class="rl">{{ auth.user()?.role }}</div>
            </div>
          </div>
        </div>
      </aside>

      <div class="main">
        <header class="topbar">
          <div class="greet">
            Welcome back, {{ auth.user()?.firstName }} 👋
            <small>Here's what's happening in your store</small>
          </div>

          <div class="top-actions">
            <div class="notif-wrap">
              <button class="notif-trigger" (click)="toggleNotif()" title="Notifications">
                <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8"><path d="M18 8a6 6 0 0 0-12 0c0 7-3 9-3 9h18s-3-2-3-9"/><path d="M13.7 21a2 2 0 0 1-3.4 0"/></svg>
                @if (notifications.hasUnread()) { <span class="notif-badge">{{ notifications.unread() }}</span> }
              </button>

              @if (notifOpen()) {
                <div class="backdrop" (click)="notifOpen.set(false)"></div>
                <div class="menu notif-menu">
                  <div class="notif-head">
                    <strong>Notifications</strong>
                    @if (notifications.items().length) { <span class="muted">{{ notifications.items().length }} recent</span> }
                  </div>
                  @if (notifications.items().length === 0) {
                    <div class="notif-empty">No notifications yet.</div>
                  } @else {
                    <div class="notif-list">
                      @for (n of notifications.items(); track n.id) {
                        <div class="notif-item" [class.unseen]="!n.isRead">
                          <input type="checkbox" class="notif-check"
                                 [checked]="checked().has(n.id)" [disabled]="n.isRead" (change)="toggleCheck(n.id)" />
                          <button class="notif-body" (click)="openNotif(n)">
                            <span class="notif-title">{{ n.title }}</span>
                            <span class="notif-msg">{{ n.message }}</span>
                            <span class="notif-time">{{ n.createdAt | date: 'MMM d, h:mm a' }}</span>
                          </button>
                        </div>
                      }
                    </div>
                    <div class="notif-foot">
                      <button class="btn btn-sm" (click)="selectAll()">Select all</button>
                      <button class="btn btn-sm btn-primary" (click)="markReadAction()">
                        @if (selectedCount()) { Mark read ({{ selectedCount() }}) } @else { Mark all read }
                      </button>
                    </div>
                  }
                </div>
              }
            </div>

            <div class="menu-wrap">
              <button class="menu-trigger" (click)="menuOpen.set(!menuOpen())">
                <span class="avatar sm">{{ initials() }}</span>
                <span class="nm">{{ auth.user()?.firstName }}</span>
                <span class="chev">▾</span>
              </button>

              @if (menuOpen()) {
                <div class="backdrop" (click)="menuOpen.set(false)"></div>
                <div class="menu">
                  <div class="menu-head">
                    <span class="avatar">{{ initials() }}</span>
                    <div>
                      <div class="nm">{{ auth.user()?.firstName }} {{ auth.user()?.lastName }}</div>
                      <div class="em">{{ auth.user()?.email }}</div>
                    </div>
                  </div>

                  <div class="menu-sec">
                    <div class="lbl">Theme</div>
                    <div class="seg">
                      <button [class.on]="theme.mode() === 'light'" (click)="theme.setMode('light')">☀️ Light</button>
                      <button [class.on]="theme.mode() === 'dark'" (click)="theme.setMode('dark')">🌙 Dark</button>
                    </div>
                    <div class="swatches">
                      @for (a of theme.accents; track a) {
                        <span class="swatch" [class.on]="theme.accent() === a"
                              [style.background]="theme.accentColor(a)" (click)="theme.setAccent(a)"></span>
                      }
                    </div>
                  </div>

                  <a class="menu-item" routerLink="/settings" (click)="menuOpen.set(false)">
                    <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8"><circle cx="12" cy="12" r="3"/><path d="M19 12a7 7 0 0 0-.1-1l2-1.6-2-3.4-2.4 1a7 7 0 0 0-1.7-1L14.5 2h-4l-.4 2.4a7 7 0 0 0-1.7 1l-2.4-1-2 3.4L4 11a7 7 0 0 0 0 2l-2 1.6 2 3.4 2.4-1a7 7 0 0 0 1.7 1l.4 2.4h4l.4-2.4a7 7 0 0 0 1.7-1l2.4 1 2-3.4-2-1.6c.1-.3.1-.7.1-1z"/></svg>
                    Account settings
                  </a>
                  <a class="menu-item" href="http://localhost:4300" target="_blank" rel="noopener" (click)="menuOpen.set(false)">
                    <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8"><path d="M18 13v6a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2V8a2 2 0 0 1 2-2h6"/><path d="M15 3h6v6M10 14 21 3"/></svg>
                    View store ↗
                  </a>
                  <button class="menu-item danger" (click)="logout()">
                    <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8"><path d="M9 21H5a2 2 0 0 1-2-2V5a2 2 0 0 1 2-2h4"/><path d="m16 17 5-5-5-5M21 12H9"/></svg>
                    Sign out
                  </button>
                </div>
              }
            </div>
          </div>
        </header>

        <main class="content">
          <router-outlet />
        </main>
      </div>
    </div>
  `,
})
export class Shell implements OnInit {
  auth = inject(Auth);
  perms = inject(Perms);
  theme = inject(Theme);
  notifications = inject(Notifications);
  private router = inject(Router);

  menuOpen = signal(false);
  notifOpen = signal(false);

  // Which notifications the admin has ticked to mark read.
  checked = signal<Set<string>>(new Set());
  selectedCount = computed(() => this.checked().size);

  ngOnInit(): void {
    // The shell only renders for signed-in admins, so this is the right place to go live.
    this.notifications.init();
  }

  // Opening/closing the panel no longer auto-reads — the admin picks which to mark read.
  toggleNotif(): void {
    const next = !this.notifOpen();
    this.notifOpen.set(next);
    if (!next) this.checked.set(new Set()); // clear selection on close
  }

  toggleCheck(id: string): void {
    const s = new Set(this.checked());
    s.has(id) ? s.delete(id) : s.add(id);
    this.checked.set(s);
  }

  // Only unread notifications are worth selecting — already-read ones don't change.
  selectAll(): void {
    this.checked.set(new Set(this.notifications.items().filter((n) => !n.isRead).map((n) => n.id)));
  }

  // One adaptive action: with items ticked, mark just those read; with none ticked, mark all read.
  markReadAction(): void {
    if (this.selectedCount() > 0) this.notifications.markRead([...this.checked()]);
    else this.notifications.markAllRead();
    this.checked.set(new Set());
  }

  openNotif(n: Notification): void {
    this.notifOpen.set(false);
    this.checked.set(new Set());
    this.router.navigate(['/orders'], { queryParams: { order: n.orderId } });
  }

  // Which parent menus are expanded. Empty by default → sub-menus are collapsed until clicked.
  expanded = signal<Set<string>>(new Set());

  toggle(id: string): void {
    const next = new Set(this.expanded());
    next.has(id) ? next.delete(id) : next.add(id);
    this.expanded.set(next);
  }

  // Visible sidebar rows: roots are always shown; a parent's children appear only while that
  // parent is expanded (any depth). Each row carries its indentation level + open/has-children flags.
  visibleMenu = computed(() => {
    const all = this.perms.menus();
    const ids = new Set(all.map((m) => m.menuId));
    const exp = this.expanded();
    const order = (a: MenuPermission, b: MenuPermission) => a.sortOrder - b.sortOrder;
    const childrenOf = (id: string) => all.filter((c) => c.parentId === id).sort(order);

    const out: (MenuPermission & { level: number; hasChildren: boolean; open: boolean })[] = [];
    const walk = (m: MenuPermission, level: number) => {
      const kids = childrenOf(m.menuId);
      const open = exp.has(m.menuId);
      out.push({ ...m, level, hasChildren: kids.length > 0, open });
      if (open) kids.forEach((c) => walk(c, level + 1));
    };
    all.filter((m) => !m.parentId || !ids.has(m.parentId)).sort(order).forEach((r) => walk(r, 0));
    return out;
  });

  initials = computed(() => {
    const u = this.auth.user();
    return ((u?.firstName?.[0] ?? '') + (u?.lastName?.[0] ?? '')).toUpperCase() || 'A';
  });

  logout(): void {
    this.menuOpen.set(false);
    this.notifications.disconnect();
    this.auth.logout();
    this.perms.clear();
    this.router.navigate(['/login']);
  }
}

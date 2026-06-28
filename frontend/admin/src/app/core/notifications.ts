import { Injectable, computed, inject, signal } from '@angular/core';
import { HubConnection, HubConnectionBuilder, LogLevel } from '@microsoft/signalr';
import { Api, API_URL } from './api';
import { Auth } from './auth';
import { Notification, NotificationList } from './models';

// Admin order-notification feed. Persistent unread count comes from REST (survives refresh);
// new orders arrive live over a SignalR hub so the badge updates without a reload.
@Injectable({ providedIn: 'root' })
export class Notifications {
  private api = inject(Api);
  private auth = inject(Auth);

  items = signal<Notification[]>([]);
  unread = signal(0);
  hasUnread = computed(() => this.unread() > 0);

  private hub: HubConnection | null = null;

  // Called once when the admin shell loads: seed state from the server, then go live.
  init(): void {
    this.refresh();
    this.connect();
  }

  // Re-load the recent list + unread count from the server.
  refresh(): void {
    this.api.get<NotificationList>('/api/notifications').subscribe({
      next: (r) => { this.items.set(r.items); this.unread.set(r.unreadCount); },
      error: () => { /* keep whatever we already have */ },
    });
  }

  // Mark everything read → badge clears (and stays cleared after refresh).
  markAllRead(): void {
    if (this.unread() === 0) return;
    this.unread.set(0);
    this.items.update((list) => list.map((n) => ({ ...n, isRead: true })));
    this.api.post('/api/notifications/read').subscribe({ error: () => this.refresh() });
  }

  private connect(): void {
    if (this.hub) return;
    this.hub = new HubConnectionBuilder()
      // withCredentials:false — we authenticate with a bearer token (accessTokenFactory), not
      // cookies, so we avoid the stricter CORS-with-credentials handshake on the gateway.
      .withUrl(`${API_URL}/hubs/notifications`, {
        accessTokenFactory: () => this.auth.token ?? '',
        withCredentials: false,
      })
      .withAutomaticReconnect()
      .configureLogging(LogLevel.Warning)
      .build();

    this.hub.on('notification', (n: Notification) => {
      this.items.update((list) => [n, ...list].slice(0, 30));
      this.unread.update((c) => c + 1);
    });

    this.hub.start().catch(() => { /* REST state still works; auto-reconnect will retry */ });
  }

  async disconnect(): Promise<void> {
    const hub = this.hub;
    this.hub = null;
    this.items.set([]);
    this.unread.set(0);
    if (hub) { try { await hub.stop(); } catch { /* ignore */ } }
  }
}

import { Injectable, computed, inject, signal } from '@angular/core';
import { HubConnection, HubConnectionBuilder, LogLevel } from '@microsoft/signalr';
import { Api, API_URL } from './api';
import { Auth } from './auth';

// A chat message as returned by the ChatService REST API and the SignalR hub.
export interface ChatMessage {
  id: string;
  conversationId: string;
  senderId: string;
  senderRole: 'Customer' | 'Support';
  senderName: string;
  content: string;
  sentAt: string;
}

interface Conversation {
  id: string;
  customerId: string;
  customerName: string;
  customerEmail: string;
  lastMessagePreview: string;
  lastMessageAt: string;
  unreadForSupport: number;
}

// Customer-side live-chat client: talks to the ChatService over REST (send / history) and receives
// messages in real time over SignalR. Messages are persisted server-side, so history survives reloads.
@Injectable({ providedIn: 'root' })
export class Chat {
  private api = inject(Api);
  private auth = inject(Auth);

  messages = signal<ChatMessage[]>([]);
  open = signal(false);
  connected = signal(false);
  sending = signal(false);
  // Unread support replies while the panel is closed — drives the badge on the launcher icon.
  unread = signal(0);
  hasUnread = computed(() => this.unread() > 0);

  private conversationId: string | null = null;
  private hub: HubConnection | null = null;
  private starting = false;

  canChat = computed(() => this.auth.isLoggedIn());

  // Open the panel; lazily start the conversation + realtime connection on first open.
  async toggle(): Promise<void> {
    const next = !this.open();
    this.open.set(next);
    if (next) {
      this.unread.set(0);
      if (this.auth.isLoggedIn()) await this.start();
    }
  }

  // Ensure the customer's conversation exists, load history, and connect the hub. Idempotent.
  private async start(): Promise<void> {
    if (this.conversationId || this.starting || !this.auth.isLoggedIn()) return;
    this.starting = true;
    try {
      const convo = await this.firstValue(this.api.get<Conversation>('/api/chat/conversations/me'));
      this.conversationId = convo.id;

      const history = await this.firstValue(
        this.api.get<ChatMessage[]>(`/api/chat/conversations/${convo.id}/messages`),
      );
      this.messages.set(history ?? []);

      await this.connect(convo.id);
    } catch {
      // Leave the panel open in a "not connected" state; the user can retry by sending.
    } finally {
      this.starting = false;
    }
  }

  private async connect(conversationId: string): Promise<void> {
    if (this.hub) return;

    const hub = new HubConnectionBuilder()
      .withUrl(`${API_URL}/hubs/chat`, {
        accessTokenFactory: () => this.auth.token ?? '',
        withCredentials: false,
      })
      .withAutomaticReconnect()
      .configureLogging(LogLevel.Warning)
      .build();

    hub.on('ReceiveMessage', (msg: ChatMessage) => this.receive(msg));

    // Re-join the conversation group after a dropped connection is restored.
    hub.onreconnected(() => hub.invoke('JoinConversation', conversationId).catch(() => {}));

    this.hub = hub;
    try {
      await hub.start();
      await hub.invoke('JoinConversation', conversationId);
      this.connected.set(true);
    } catch {
      this.connected.set(false);
    }
  }

  async send(text: string): Promise<void> {
    const content = text.trim();
    if (!content || !this.auth.isLoggedIn()) return;

    // Make sure we have a conversation (covers sending before the panel finished starting).
    if (!this.conversationId) await this.start();
    if (!this.conversationId) return;

    this.sending.set(true);
    try {
      const saved = await this.firstValue(
        this.api.post<ChatMessage>(`/api/chat/conversations/${this.conversationId}/messages`, { content }),
      );
      // Add immediately for a snappy UX; the SignalR echo is de-duplicated by id.
      this.receive(saved);
    } finally {
      this.sending.set(false);
    }
  }

  // Called on logout to tear down the realtime connection and clear state.
  async reset(): Promise<void> {
    const hub = this.hub;
    this.hub = null;
    this.conversationId = null;
    this.messages.set([]);
    this.unread.set(0);
    this.connected.set(false);
    this.open.set(false);
    if (hub) {
      try { await hub.stop(); } catch { /* ignore */ }
    }
  }

  // Adds a message unless we already have it (dedupes the sender's own echo).
  private receive(msg: ChatMessage): void {
    this.messages.update((list) => (list.some((m) => m.id === msg.id) ? list : [...list, msg]));
    if (!this.open() && msg.senderRole === 'Support') this.unread.update((c) => c + 1);
  }

  private firstValue<T>(obs: import('rxjs').Observable<T>): Promise<T> {
    return new Promise<T>((resolve, reject) => {
      obs.subscribe({ next: resolve, error: reject });
    });
  }
}

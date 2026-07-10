import { Injectable, inject, signal } from '@angular/core';
import { HubConnection, HubConnectionBuilder, LogLevel } from '@microsoft/signalr';
import { Api, API_URL } from './api';
import { Auth } from './auth';

export interface ChatMessage {
  id: string;
  conversationId: string;
  senderId: string;
  senderRole: 'Customer' | 'Support';
  senderName: string;
  content: string;
  sentAt: string;
}

export interface Conversation {
  id: string;
  customerId: string;
  customerName: string;
  customerEmail: string;
  lastMessagePreview: string;
  lastMessageAt: string;
  unreadForSupport: number;
}

// Support-agent side of live chat. Connects to the ChatService hub (auto-joining the "support"
// group, since agents are Admins), keeps the conversation inbox live, and lets the agent open a
// thread and reply. History and sends go over REST; delivery is real-time over SignalR.
@Injectable({ providedIn: 'root' })
export class Chat {
  private api = inject(Api);
  private auth = inject(Auth);

  conversations = signal<Conversation[]>([]);
  messages = signal<ChatMessage[]>([]);
  activeId = signal<string | null>(null);
  connected = signal(false);
  sending = signal(false);
  // True while the customer in the open thread is typing.
  otherTyping = signal(false);

  private hub: HubConnection | null = null;
  private lastTypingSentAt = 0;
  private stopTypingTimer: ReturnType<typeof setTimeout> | null = null;
  private clearTypingTimer: ReturnType<typeof setTimeout> | null = null;

  // Called when the Support Chat page loads.
  async init(): Promise<void> {
    await this.loadConversations();
    await this.connect();
  }

  async loadConversations(): Promise<void> {
    const list = await this.firstValue(
      this.api.get<Conversation[]>('/api/chat/conversations', undefined, { toastError: false }),
    );
    this.conversations.set(this.sort(list ?? []));
  }

  // Open a conversation: subscribe to its live feed and load its history. Loading history also
  // clears the server-side unread badge (and broadcasts a ConversationUpdated we pick up).
  async openConversation(id: string): Promise<void> {
    this.activeId.set(id);
    this.otherTyping.set(false);   // reset the indicator when switching threads
    if (this.hub && this.connected()) {
      try { await this.hub.invoke('JoinConversation', id); } catch { /* ignore */ }
    }
    const history = await this.firstValue(
      this.api.get<ChatMessage[]>(`/api/chat/conversations/${id}/messages`, undefined, { toastError: false }),
    );
    this.messages.set(history ?? []);
  }

  async send(text: string): Promise<void> {
    const id = this.activeId();
    const content = text.trim();
    if (!id || !content) return;

    this.stopTyping();
    this.sending.set(true);
    try {
      const saved = await this.firstValue(
        this.api.post<ChatMessage>(`/api/chat/conversations/${id}/messages`, { content }, { toastError: true }),
      );
      this.receiveMessage(saved);
    } finally {
      this.sending.set(false);
    }
  }

  // Call on each keystroke in the reply box — throttled "typing" signal to the customer.
  notifyTyping(): void {
    const id = this.activeId();
    if (!this.hub || !this.connected() || !id) return;

    const now = Date.now();
    if (now - this.lastTypingSentAt > 2000) {
      this.lastTypingSentAt = now;
      this.hub.invoke('Typing', id, true).catch(() => {});
    }
    if (this.stopTypingTimer) clearTimeout(this.stopTypingTimer);
    this.stopTypingTimer = setTimeout(() => this.stopTyping(), 2500);
  }

  private stopTyping(): void {
    if (this.stopTypingTimer) { clearTimeout(this.stopTypingTimer); this.stopTypingTimer = null; }
    this.lastTypingSentAt = 0;
    const id = this.activeId();
    if (this.hub && this.connected() && id) this.hub.invoke('Typing', id, false).catch(() => {});
  }

  private onTyping(e: { conversationId: string; senderRole: string; isTyping: boolean }): void {
    // Only reflect typing for the customer in the thread the agent is viewing.
    if (e.conversationId !== this.activeId() || e.senderRole !== 'Customer') return;
    if (this.clearTypingTimer) { clearTimeout(this.clearTypingTimer); this.clearTypingTimer = null; }
    this.otherTyping.set(e.isTyping);
    if (e.isTyping) this.clearTypingTimer = setTimeout(() => this.otherTyping.set(false), 5000);
  }

  async disconnect(): Promise<void> {
    const hub = this.hub;
    this.hub = null;
    this.connected.set(false);
    this.activeId.set(null);
    this.messages.set([]);
    this.otherTyping.set(false);
    if (this.stopTypingTimer) { clearTimeout(this.stopTypingTimer); this.stopTypingTimer = null; }
    if (this.clearTypingTimer) { clearTimeout(this.clearTypingTimer); this.clearTypingTimer = null; }
    if (hub) {
      try { await hub.stop(); } catch { /* ignore */ }
    }
  }

  private async connect(): Promise<void> {
    if (this.hub) return;

    const hub = new HubConnectionBuilder()
      .withUrl(`${API_URL}/hubs/chat`, {
        accessTokenFactory: () => this.auth.token ?? '',
        withCredentials: false,
      })
      .withAutomaticReconnect()
      .configureLogging(LogLevel.Warning)
      .build();

    hub.on('ReceiveMessage', (msg: ChatMessage) => this.receiveMessage(msg));
    hub.on('ConversationUpdated', (c: Conversation) => this.upsertConversation(c));
    hub.on('Typing', (e: { conversationId: string; senderRole: string; isTyping: boolean }) => this.onTyping(e));

    // After a reconnect, refresh the inbox and rejoin the open thread.
    hub.onreconnected(async () => {
      await this.loadConversations();
      const id = this.activeId();
      if (id) { try { await hub.invoke('JoinConversation', id); } catch { /* ignore */ } }
    });

    this.hub = hub;
    try {
      await hub.start();
      this.connected.set(true);
      const id = this.activeId();
      if (id) await hub.invoke('JoinConversation', id);
    } catch {
      this.connected.set(false);
    }
  }

  private receiveMessage(msg: ChatMessage): void {
    if (msg.conversationId !== this.activeId()) return;
    this.messages.update((list) => (list.some((m) => m.id === msg.id) ? list : [...list, msg]));
    // A customer message means they've stopped typing.
    if (msg.senderRole === 'Customer') this.otherTyping.set(false);
  }

  private upsertConversation(c: Conversation): void {
    this.conversations.update((list) => {
      const rest = list.filter((x) => x.id !== c.id);
      return this.sort([c, ...rest]);
    });
  }

  private sort(list: Conversation[]): Conversation[] {
    return [...list].sort((a, b) => new Date(b.lastMessageAt).getTime() - new Date(a.lastMessageAt).getTime());
  }

  private firstValue<T>(obs: import('rxjs').Observable<T>): Promise<T> {
    return new Promise<T>((resolve, reject) => {
      obs.subscribe({ next: resolve, error: reject });
    });
  }
}

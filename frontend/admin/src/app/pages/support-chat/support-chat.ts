import { Component, ElementRef, OnDestroy, OnInit, computed, effect, inject, signal, viewChild } from '@angular/core';
import { DatePipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Chat } from '../../core/chat';
import { TypingIndicator } from '../../shared/typing-indicator';

// Support-agent inbox: a live list of customer conversations on the left, the selected thread on
// the right with a reply box. Everything updates in real time over SignalR.
@Component({
  selector: 'app-support-chat',
  imports: [DatePipe, FormsModule, TypingIndicator],
  template: `
    <div class="page-head">
      <div>
        <h1>Support Chat</h1>
        <div class="sub">
          <span class="dot" [class.on]="chat.connected()"></span>
          {{ chat.connected() ? 'Live' : 'Connecting…' }} · {{ chat.conversations().length }} conversation(s)
        </div>
      </div>
    </div>

    <div class="chat-layout card">
      <!-- Conversation list -->
      <aside class="convo-list">
        @if (chat.conversations().length === 0) {
          <div class="empty">No conversations yet.</div>
        }
        @for (c of chat.conversations(); track c.id) {
          <button class="convo" [class.active]="c.id === chat.activeId()" (click)="open(c.id)">
            <div class="convo-top">
              <span class="convo-name">{{ c.customerName }}</span>
              @if (c.unreadForSupport > 0 && c.id !== chat.activeId()) {
                <span class="convo-badge">{{ c.unreadForSupport }}</span>
              }
            </div>
            <div class="convo-preview">{{ c.lastMessagePreview || 'No messages yet' }}</div>
            <div class="convo-time">{{ c.lastMessageAt | date: 'short' }}</div>
          </button>
        }
      </aside>

      <!-- Active thread -->
      <section class="thread">
        @if (!chat.activeId()) {
          <div class="empty thread-empty">Select a conversation to start replying.</div>
        } @else {
          <div class="thread-head">{{ activeName() }}</div>
          <div class="thread-body" #body>
            @for (m of chat.messages(); track m.id) {
              <div class="msg" [class.mine]="m.senderRole === 'Support'">
                <div class="bubble">
                  {{ m.content }}
                  <span class="time">{{ m.sentAt | date: 'shortTime' }}</span>
                </div>
              </div>
            }
            <app-typing-indicator [active]="chat.otherTyping()" [who]="activeName()" />
          </div>
          <form class="reply" (ngSubmit)="submit()">
            <input type="text" [(ngModel)]="draft" name="draft" placeholder="Type a reply…" autocomplete="off" [disabled]="chat.sending()" (ngModelChange)="chat.notifyTyping()" />
            <button class="btn primary" type="submit" [disabled]="chat.sending() || !draft.trim()">Send</button>
          </form>
        }
      </section>
    </div>
  `,
  styles: [
    `
      .dot { display: inline-block; width: 8px; height: 8px; border-radius: 50%; background: #cbd5e1; margin-right: 4px; }
      .dot.on { background: #22c55e; }
      .chat-layout { display: grid; grid-template-columns: 300px 1fr; height: calc(100vh - 200px); min-height: 420px; padding: 0; overflow: hidden; }
      .convo-list { border-right: 1px solid var(--line, #e5e7eb); overflow-y: auto; }
      .convo { display: block; width: 100%; text-align: left; border: 0; border-bottom: 1px solid var(--line, #f1f5f9); background: transparent; padding: 12px 14px; cursor: pointer; }
      .convo:hover { background: var(--hover, #f8fafc); }
      .convo.active { background: var(--brand-soft, #eff6ff); }
      .convo-top { display: flex; justify-content: space-between; align-items: center; }
      .convo-name { font-weight: 600; }
      .convo-badge { background: #e11d48; color: #fff; border-radius: 10px; font-size: 11px; font-weight: 700; padding: 1px 7px; }
      .convo-preview { color: var(--muted, #6b7280); font-size: 13px; margin-top: 2px; overflow: hidden; text-overflow: ellipsis; white-space: nowrap; }
      .convo-time { color: var(--muted, #9ca3af); font-size: 11px; margin-top: 3px; }
      .thread { display: flex; flex-direction: column; overflow: hidden; }
      .thread-empty { display: grid; place-items: center; height: 100%; }
      .thread-head { padding: 14px 16px; border-bottom: 1px solid var(--line, #e5e7eb); font-weight: 600; }
      .thread-body { flex: 1; overflow-y: auto; padding: 16px; display: flex; flex-direction: column; gap: 8px; background: var(--bg, #f9fafb); }
      .msg { display: flex; }
      .msg.mine { justify-content: flex-end; }
      .bubble { max-width: 70%; padding: 9px 12px; border-radius: 12px; font-size: 14px; line-height: 1.4; background: #fff; border: 1px solid var(--line, #e5e7eb); white-space: pre-wrap; word-wrap: break-word; }
      .msg.mine .bubble { background: #2563eb; color: #fff; border-color: #2563eb; }
      .time { display: block; font-size: 10px; opacity: 0.65; margin-top: 3px; text-align: right; }
      .reply { display: flex; gap: 8px; padding: 12px; border-top: 1px solid var(--line, #e5e7eb); }
      .reply input { flex: 1; border: 1px solid var(--line, #d1d5db); border-radius: 8px; padding: 9px 12px; font-size: 14px; outline: none; }
      .reply input:focus { border-color: #2563eb; }
      .btn.primary { background: #2563eb; color: #fff; border: 0; border-radius: 8px; padding: 0 18px; font-weight: 600; cursor: pointer; }
      .btn.primary:disabled { opacity: 0.5; cursor: not-allowed; }
    `,
  ],
})
export class SupportChatPage implements OnInit, OnDestroy {
  chat = inject(Chat);
  draft = '';

  private body = viewChild<ElementRef<HTMLDivElement>>('body');

  activeName = computed(() => {
    const id = this.chat.activeId();
    return this.chat.conversations().find((c) => c.id === id)?.customerName ?? 'Conversation';
  });

  constructor() {
    effect(() => {
      this.chat.messages();
      this.chat.otherTyping();
      queueMicrotask(() => {
        const el = this.body()?.nativeElement;
        if (el) el.scrollTop = el.scrollHeight;
      });
    });
  }

  ngOnInit(): void {
    void this.chat.init();
  }

  ngOnDestroy(): void {
    void this.chat.disconnect();
  }

  open(id: string): void {
    void this.chat.openConversation(id);
  }

  async submit(): Promise<void> {
    const text = this.draft;
    this.draft = '';
    await this.chat.send(text);
  }
}

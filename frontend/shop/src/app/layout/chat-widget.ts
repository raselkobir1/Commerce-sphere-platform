import { Component, ElementRef, effect, inject, viewChild } from '@angular/core';
import { DatePipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { Auth } from '../core/auth';
import { Chat } from '../core/chat';
import { TypingIndicator } from './typing-indicator';
import { I18n } from '../core/i18n';
import { TranslatePipe } from '../core/translate.pipe';

// Floating live-support chat, pinned to the bottom-right of every page. Click the launcher to open a
// panel and chat with a shop support agent in real time. Requires the customer to be signed in.
@Component({
  selector: 'app-chat-widget',
  imports: [FormsModule, RouterLink, DatePipe, TypingIndicator, TranslatePipe],
  template: `
    <!-- Launcher -->
    <button class="chat-fab" type="button" (click)="chat.toggle()" [attr.aria-label]="(chat.open() ? 'chat.closeChat' : 'chat.openChat') | t">
      @if (chat.open()) {
        <span class="chat-fab-ico">✕</span>
      } @else {
        <span class="chat-fab-ico">💬</span>
        @if (chat.hasUnread()) { <span class="chat-badge">{{ chat.unread() }}</span> }
      }
    </button>

    @if (chat.open()) {
      <div class="chat-panel">
        <div class="chat-head">
          <div class="chat-head-title">
            <span class="chat-dot" [class.on]="chat.connected()"></span>
            {{ 'chat.title' | t }}
          </div>
          <div class="chat-head-sub">{{ 'chat.subtitle' | t }}</div>
        </div>

        @if (!auth.isLoggedIn()) {
          <div class="chat-empty">
            <p>{{ 'chat.pleaseSignIn' | t }}</p>
            <a class="btn btn-primary btn-sm" routerLink="/login" (click)="chat.open.set(false)">{{ 'common.signIn' | t }}</a>
          </div>
        } @else {
          <div class="chat-body" #body>
            @if (chat.messages().length === 0) {
              <div class="chat-hint">{{ 'chat.hint' | t }}</div>
            }
            @for (m of chat.messages(); track m.id) {
              <div class="chat-msg" [class.mine]="m.senderRole === 'Customer'">
                <div class="chat-bubble">
                  {{ m.content }}
                  <span class="chat-time">{{ m.sentAt | date: 'shortTime' }}</span>
                </div>
              </div>
            }
            <app-typing-indicator [active]="chat.otherTyping()" [who]="'chat.support' | t" />
          </div>

          <form class="chat-input" (ngSubmit)="submit()">
            <input
              type="text"
              [(ngModel)]="draft"
              name="draft"
              [placeholder]="'chat.placeholder' | t"
              autocomplete="off"
              [disabled]="chat.sending()"
              (ngModelChange)="chat.notifyTyping()"
            />
            <button class="btn btn-primary btn-sm" type="submit" [disabled]="chat.sending() || !draft.trim()">{{ 'chat.send' | t }}</button>
          </form>
        }
      </div>
    }
  `,
  styles: [
    `
      .chat-fab {
        position: fixed; right: 22px; bottom: 22px; z-index: 1000;
        width: 58px; height: 58px; border-radius: 50%; border: 0; cursor: pointer;
        background: var(--brand); color: #fff; font-size: 24px;
        box-shadow: 0 8px 24px rgba(0, 0, 0, 0.22); display: grid; place-items: center;
        transition: transform 0.15s ease, background 0.15s ease;
      }
      .chat-fab:hover { background: var(--brand-dark); transform: translateY(-2px); }
      .chat-badge {
        position: absolute; top: 4px; right: 4px; min-width: 18px; height: 18px; padding: 0 4px;
        background: var(--red, #e11d48); color: #fff; border-radius: 10px; font-size: 11px;
        font-weight: 700; display: grid; place-items: center;
      }
      .chat-panel {
        position: fixed; right: 22px; bottom: 92px; z-index: 1000;
        width: 360px; max-width: calc(100vw - 44px); height: 480px; max-height: calc(100vh - 130px);
        background: var(--panel, #fff); border: 1px solid var(--line, #e5e7eb); border-radius: 14px;
        box-shadow: 0 16px 48px rgba(0, 0, 0, 0.24); display: flex; flex-direction: column; overflow: hidden;
      }
      .chat-head { background: var(--brand); color: #fff; padding: 14px 16px; }
      .chat-head-title { font-weight: 700; display: flex; align-items: center; gap: 8px; }
      .chat-head-sub { font-size: 12px; opacity: 0.9; margin-top: 2px; }
      .chat-dot { width: 9px; height: 9px; border-radius: 50%; background: rgba(255, 255, 255, 0.5); }
      .chat-dot.on { background: #4ade80; }
      .chat-body { flex: 1; overflow-y: auto; padding: 14px; display: flex; flex-direction: column; gap: 8px; background: var(--bg, #f9fafb); }
      .chat-hint { text-align: center; color: var(--muted, #6b7280); font-size: 13px; margin: auto 0; }
      .chat-msg { display: flex; }
      .chat-msg.mine { justify-content: flex-end; }
      .chat-bubble {
        max-width: 78%; padding: 8px 11px; border-radius: 12px; font-size: 14px; line-height: 1.35;
        background: #fff; border: 1px solid var(--line, #e5e7eb); color: var(--ink, #111827);
        word-wrap: break-word; white-space: pre-wrap;
      }
      .chat-msg.mine .chat-bubble { background: var(--brand); color: #fff; border-color: var(--brand); }
      .chat-time { display: block; font-size: 10px; opacity: 0.65; margin-top: 3px; text-align: right; }
      .chat-input { display: flex; gap: 8px; padding: 10px; border-top: 1px solid var(--line, #e5e7eb); background: var(--panel, #fff); }
      .chat-input input {
        flex: 1; border: 1px solid var(--line, #d1d5db); border-radius: 20px; padding: 9px 14px; font-size: 14px; outline: none;
      }
      .chat-input input:focus { border-color: var(--brand); }
      .chat-empty { flex: 1; display: flex; flex-direction: column; gap: 12px; align-items: center; justify-content: center; padding: 20px; text-align: center; color: var(--muted, #6b7280); }
    `,
  ],
})
export class ChatWidget {
  chat = inject(Chat);
  auth = inject(Auth);
  draft = '';

  private body = viewChild<ElementRef<HTMLDivElement>>('body');

  constructor() {
    // Auto-scroll to the newest message (or the typing indicator) whenever they change.
    effect(() => {
      this.chat.messages();
      this.chat.otherTyping();
      queueMicrotask(() => {
        const el = this.body()?.nativeElement;
        if (el) el.scrollTop = el.scrollHeight;
      });
    });
  }

  async submit(): Promise<void> {
    const text = this.draft;
    this.draft = '';
    await this.chat.send(text);
  }
}

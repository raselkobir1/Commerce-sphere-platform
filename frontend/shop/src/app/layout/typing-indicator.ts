import { Component, input } from '@angular/core';

// Reusable "someone is typing…" indicator: three animated dots plus an optional name.
// Render it and toggle with the [active] input, e.g.
//   <app-typing-indicator [active]="chat.otherTyping()" who="Support" />
@Component({
  selector: 'app-typing-indicator',
  template: `
    @if (active()) {
      <div class="typing" role="status" aria-live="polite">
        <span class="typing-dots" aria-hidden="true"><span></span><span></span><span></span></span>
        <span class="typing-text">{{ who() }} is typing…</span>
      </div>
    }
  `,
  styles: [
    `
      .typing {
        display: flex;
        align-items: center;
        gap: 8px;
        padding: 4px 2px;
        color: var(--muted, #6b7280);
        font-size: 12px;
      }
      .typing-dots {
        display: inline-flex;
        align-items: center;
        gap: 3px;
      }
      .typing-dots span {
        width: 6px;
        height: 6px;
        border-radius: 50%;
        background: currentColor;
        opacity: 0.4;
        animation: typing-bounce 1.2s infinite ease-in-out;
      }
      .typing-dots span:nth-child(2) { animation-delay: 0.15s; }
      .typing-dots span:nth-child(3) { animation-delay: 0.3s; }
      @keyframes typing-bounce {
        0%, 60%, 100% { transform: translateY(0); opacity: 0.4; }
        30% { transform: translateY(-4px); opacity: 1; }
      }
    `,
  ],
})
export class TypingIndicator {
  active = input(false);
  who = input('Someone');
}

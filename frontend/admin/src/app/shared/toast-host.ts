import { Component, inject } from '@angular/core';
import { Toast } from '../core/toast';

// Renders the stack of active toasts (top-right). Included once in the shell.
@Component({
  selector: 'app-toast-host',
  template: `
    <div class="toast-wrap" aria-live="polite" aria-atomic="true">
      @for (t of toast.items(); track t.id) {
        <div class="toast" [class]="t.kind" role="status">
          <span class="ic">
            @switch (t.kind) {
              @case ('success') { ✓ }
              @case ('error') { ✕ }
              @default { i }
            }
          </span>
          <span class="msg">{{ t.text }}</span>
          <button class="x" (click)="toast.dismiss(t.id)" aria-label="Dismiss">×</button>
        </div>
      }
    </div>
  `,
  styles: [
    `
      .toast-wrap {
        position: fixed; top: 18px; right: 18px; z-index: 9999;
        display: flex; flex-direction: column; gap: 10px;
        max-width: min(380px, calc(100vw - 36px)); pointer-events: none;
      }
      .toast {
        pointer-events: auto;
        display: flex; align-items: center; gap: 10px;
        padding: 12px 14px; border-radius: 10px;
        background: var(--card, #fff); color: var(--text, #111827);
        border: 1px solid var(--border, #e5e7eb);
        border-left: 4px solid var(--muted, #9ca3af);
        box-shadow: 0 8px 24px rgba(0, 0, 0, 0.16);
        font-size: 14px; line-height: 1.35;
        animation: toast-in 0.22s ease-out;
      }
      .toast.success { border-left-color: #16a34a; }
      .toast.error { border-left-color: #dc2626; }
      .toast.info { border-left-color: var(--accent, #4f46e5); }
      .ic {
        flex: none; width: 22px; height: 22px; border-radius: 50%;
        display: grid; place-items: center; font-size: 13px; font-weight: 700; color: #fff;
      }
      .success .ic { background: #16a34a; }
      .error .ic { background: #dc2626; }
      .info .ic { background: var(--accent, #4f46e5); }
      .msg { flex: 1; word-break: break-word; }
      .x {
        flex: none; border: 0; background: transparent; cursor: pointer;
        font-size: 20px; line-height: 1; color: var(--muted, #9ca3af); padding: 0 2px;
      }
      .x:hover { color: var(--text, #111827); }
      @keyframes toast-in {
        from { opacity: 0; transform: translateX(16px); }
        to { opacity: 1; transform: translateX(0); }
      }
    `,
  ],
})
export class ToastHost {
  toast = inject(Toast);
}

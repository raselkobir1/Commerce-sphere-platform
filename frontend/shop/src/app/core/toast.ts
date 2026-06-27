import { Component, Injectable, inject, signal } from '@angular/core';

interface ToastMsg {
  id: number;
  text: string;
}

// Minimal toast notifications ("Added to cart", etc.).
@Injectable({ providedIn: 'root' })
export class Toast {
  private seq = 0;
  messages = signal<ToastMsg[]>([]);

  show(text: string): void {
    const id = ++this.seq;
    this.messages.update((m) => [...m, { id, text }]);
    setTimeout(() => this.messages.update((m) => m.filter((t) => t.id !== id)), 2500);
  }
}

// Drop <app-toast /> once at the app root to render the stack.
@Component({
  selector: 'app-toast',
  template: `
    <div class="toast-stack">
      @for (t of toast.messages(); track t.id) {
        <div class="toast">✓ {{ t.text }}</div>
      }
    </div>
  `,
})
export class ToastOutlet {
  toast = inject(Toast);
}

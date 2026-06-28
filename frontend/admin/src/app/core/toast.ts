import { Injectable, signal } from '@angular/core';

export type ToastKind = 'success' | 'error' | 'info';

export interface ToastItem {
  id: number;
  kind: ToastKind;
  text: string;
}

// App-wide toast notifications. Anything can inject this and call success()/error()/info();
// the single <app-toast-host> in the shell renders the stack. Toasts auto-dismiss, and errors
// linger a little longer so they're not missed.
@Injectable({ providedIn: 'root' })
export class Toast {
  private seq = 0;
  readonly items = signal<ToastItem[]>([]);

  success(text: string): void { this.show('success', text, 3500); }
  info(text: string): void { this.show('info', text, 3500); }
  error(text: string): void { this.show('error', text, 6000); }

  dismiss(id: number): void {
    this.items.update((list) => list.filter((t) => t.id !== id));
  }

  private show(kind: ToastKind, text: string, ms: number): void {
    if (!text) return;
    const id = ++this.seq;
    this.items.update((list) => [...list, { id, kind, text }]);
    setTimeout(() => this.dismiss(id), ms);
  }
}

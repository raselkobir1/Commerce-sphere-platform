import { Injectable, signal } from '@angular/core';

type Mode = 'light' | 'dark';

// Accent presets: [primary, secondary]. The "soft" tints are derived in CSS via color-mix,
// so setting --brand / --brand-2 is enough to re-theme the whole app.
const ACCENTS: Record<string, [string, string]> = {
  indigo: ['#4f46e5', '#6366f1'],
  emerald: ['#059669', '#10b981'],
  rose: ['#e11d48', '#f43f5e'],
  amber: ['#d97706', '#f59e0b'],
  sky: ['#0284c7', '#0ea5e9'],
};

// Stores and applies the admin's appearance preferences (persisted in localStorage).
@Injectable({ providedIn: 'root' })
export class Theme {
  readonly accents = Object.keys(ACCENTS);
  mode = signal<Mode>((localStorage.getItem('admin.theme') as Mode) || 'light');
  accent = signal<string>(localStorage.getItem('admin.accent') || 'indigo');

  constructor() {
    this.applyMode();
    this.applyAccent();
  }

  setMode(m: Mode): void {
    this.mode.set(m);
    localStorage.setItem('admin.theme', m);
    this.applyMode();
  }

  setAccent(name: string): void {
    if (!ACCENTS[name]) return;
    this.accent.set(name);
    localStorage.setItem('admin.accent', name);
    this.applyAccent();
  }

  accentColor(name: string): string {
    return (ACCENTS[name] ?? ACCENTS['indigo'])[0];
  }

  private applyMode(): void {
    document.documentElement.setAttribute('data-theme', this.mode());
  }

  private applyAccent(): void {
    const [brand, brand2] = ACCENTS[this.accent()] ?? ACCENTS['indigo'];
    const root = document.documentElement.style;
    root.setProperty('--brand', brand);
    root.setProperty('--brand-2', brand2);
  }
}

import { Injectable, effect, signal } from '@angular/core';
import { BN, EN } from './i18n.data';

export type Lang = 'en' | 'bn';

const STORAGE_KEY = 'shop.lang';

// Signal-based, runtime-switchable UI language for the storefront (no page reload,
// no separate Angular i18n build per locale). Persists the choice in localStorage.
@Injectable({ providedIn: 'root' })
export class I18n {
  lang = signal<Lang>(this.restore());

  constructor() {
    effect(() => {
      const lang = this.lang();
      localStorage.setItem(STORAGE_KEY, lang);
      document.documentElement.lang = lang;
    });
  }

  private restore(): Lang {
    const saved = localStorage.getItem(STORAGE_KEY);
    return saved === 'bn' ? 'bn' : 'en';
  }

  set(lang: Lang): void {
    this.lang.set(lang);
  }

  t(key: string, params?: Record<string, string | number>): string {
    const dict = this.lang() === 'bn' ? BN : EN;
    let text = dict[key] ?? EN[key] ?? key;
    if (params) {
      for (const [k, v] of Object.entries(params)) {
        text = text.replace(new RegExp(`{{\\s*${k}\\s*}}`, 'g'), String(v));
      }
    }
    return text;
  }
}

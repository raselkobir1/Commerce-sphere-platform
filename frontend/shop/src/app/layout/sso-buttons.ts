import { Component, OnInit, inject, signal } from '@angular/core';
import { Sso } from '../core/sso';

// Renders a "continue with <provider>" button for each enabled social provider.
// Drop <app-sso-buttons /> on the login / register pages.
@Component({
  selector: 'app-sso-buttons',
  template: `
    @if (providers().length) {
      <div class="sso-divider"><span>or continue with</span></div>
      <div class="sso-grid">
        @for (p of providers(); track p) {
          <button type="button" class="btn sso-btn" (click)="sso.start(p)">
            <span class="sso-ico">{{ icon(p) }}</span> {{ label(p) }}
          </button>
        }
      </div>
    }
  `,
})
export class SsoButtons implements OnInit {
  sso = inject(Sso);
  providers = signal<string[]>([]);

  ngOnInit(): void {
    this.sso.providers().subscribe({
      next: (p) => this.providers.set(p),
      error: () => this.providers.set([]), // SSO not available — just hide the buttons
    });
  }

  icon(p: string): string {
    return { google: 'G', github: '', facebook: 'f' }[p.toLowerCase()] ?? '•';
  }

  label(p: string): string {
    return p.charAt(0).toUpperCase() + p.slice(1);
  }
}

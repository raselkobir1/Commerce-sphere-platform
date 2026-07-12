import { Component, OnInit, inject, signal } from '@angular/core';
import { Sso, SsoProvider } from '../core/sso';
import { I18n } from '../core/i18n';
import { TranslatePipe } from '../core/translate.pipe';

// Renders a "continue with <provider>" button for every supported social provider.
// Every provider is always shown; one whose credentials aren't configured yet is rendered
// disabled with a hint, so the buttons appear in dev and light up once credentials are set.
// Drop <app-sso-buttons /> on the login / register pages.
@Component({
  selector: 'app-sso-buttons',
  imports: [TranslatePipe],
  template: `
    @if (providers().length) {
      <div class="sso-divider"><span>{{ 'sso.orContinueWith' | t }}</span></div>
      <div class="sso-grid">
        @for (p of providers(); track p.name) {
          <button
            type="button"
            class="btn sso-btn"
            [disabled]="!p.enabled"
            [title]="p.enabled ? i18n.t('sso.continueWith', { provider: label(p.name) }) : i18n.t('sso.notConfiguredYet', { provider: label(p.name) })"
            (click)="p.enabled && sso.start(p.name)"
          >
            <span class="sso-ico">{{ icon(p.name) }}</span> {{ label(p.name) }}
            @if (!p.enabled) {
              <span class="sso-soon">{{ 'sso.soon' | t }}</span>
            }
          </button>
        }
      </div>
    }
  `,
  styles: [
    `
      .sso-btn[disabled] {
        opacity: 0.55;
        cursor: not-allowed;
      }
      .sso-soon {
        margin-left: 0.4rem;
        font-size: 0.7em;
        text-transform: uppercase;
        letter-spacing: 0.05em;
        opacity: 0.7;
      }
    `,
  ],
})
export class SsoButtons implements OnInit {
  sso = inject(Sso);
  i18n = inject(I18n);
  providers = signal<SsoProvider[]>([]);

  ngOnInit(): void {
    this.sso.providers().subscribe({
      next: (p) => this.providers.set(p),
      error: () => this.providers.set([]), // SSO endpoint unreachable — hide the section
    });
  }

  icon(name: string): string {
    return { google: 'G', facebook: 'f' }[name.toLowerCase()] ?? '•';
  }

  label(name: string): string {
    return name.charAt(0).toUpperCase() + name.slice(1);
  }
}

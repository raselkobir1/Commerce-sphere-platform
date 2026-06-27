import { Component, OnDestroy, OnInit, computed, inject, signal } from '@angular/core';
import { Api } from '../core/api';
import { Banner } from '../core/models';

// Auto-rotating hero carousel of admin-managed banners, shown at the top of the home page.
// Renders nothing when there are no active banners.
@Component({
  selector: 'app-banner-carousel',
  template: `
    @if (slides().length) {
      <div class="carousel" (mouseenter)="pause()" (mouseleave)="resume()">
        @for (b of slides(); track b.id; let i = $index) {
          <a class="slide" [class.on]="i === index()"
             [style.background-image]="'url(' + b.imageUrl + ')'"
             [href]="b.linkUrl || null" [attr.target]="external(b.linkUrl) ? '_blank' : null"
             [attr.rel]="external(b.linkUrl) ? 'noopener' : null">
            <div class="slide-overlay">
              <div class="slide-text">
                <h2>{{ b.title }}</h2>
                @if (b.subtitle) { <p>{{ b.subtitle }}</p> }
                @if (b.linkUrl) { <span class="slide-cta">Shop now →</span> }
              </div>
            </div>
          </a>
        }

        @if (slides().length > 1) {
          <button class="car-nav prev" (click)="prev($event)" aria-label="Previous">‹</button>
          <button class="car-nav next" (click)="next($event)" aria-label="Next">›</button>
          <div class="car-dots">
            @for (b of slides(); track b.id; let i = $index) {
              <button class="dot" [class.on]="i === index()" (click)="goTo(i, $event)" [attr.aria-label]="'Slide ' + (i + 1)"></button>
            }
          </div>
        }
      </div>
    }
  `,
})
export class BannerCarousel implements OnInit, OnDestroy {
  private api = inject(Api);

  slides = signal<Banner[]>([]);
  index = signal(0);
  private timer: ReturnType<typeof setInterval> | null = null;

  count = computed(() => this.slides().length);

  ngOnInit(): void {
    this.api.get<Banner[]>('/api/banners').subscribe((list) => {
      const active = list.filter((b) => b.isActive).sort((a, b) => a.sortOrder - b.sortOrder);
      this.slides.set(active);
      if (active.length > 1) this.start();
    });
  }

  ngOnDestroy(): void { this.stop(); }

  private start(): void {
    this.stop();
    this.timer = setInterval(() => this.advance(1), 5000);
  }
  private stop(): void {
    if (this.timer) { clearInterval(this.timer); this.timer = null; }
  }

  pause(): void { this.stop(); }
  resume(): void { if (this.count() > 1) this.start(); }

  private advance(step: number): void {
    const n = this.count();
    if (n === 0) return;
    this.index.set((this.index() + step + n) % n);
  }

  prev(e: Event): void { e.preventDefault(); this.advance(-1); this.resume(); }
  next(e: Event): void { e.preventDefault(); this.advance(1); this.resume(); }
  goTo(i: number, e: Event): void { e.preventDefault(); this.index.set(i); this.resume(); }

  external(url: string): boolean { return /^https?:\/\//i.test(url); }
}

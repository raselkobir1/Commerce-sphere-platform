import { Component } from '@angular/core';
import { RouterOutlet } from '@angular/router';

// Centered card shell for all unauthenticated pages (login, register, password, verify).
@Component({
  selector: 'app-auth-layout',
  imports: [RouterOutlet],
  template: `
    <div class="auth-shell">
      <div class="auth-brand">
        <span class="logo">◆</span>
        <span class="name">CommerceSphere</span>
      </div>
      <div class="auth-card">
        <router-outlet />
      </div>
      <p class="auth-foot">© CommerceSphere — multi-service commerce platform</p>
    </div>
  `,
  styles: [
    `
      .auth-shell {
        min-height: 100dvh;
        display: flex;
        flex-direction: column;
        align-items: center;
        justify-content: center;
        gap: 1.25rem;
        padding: 2rem 1rem;
        background: radial-gradient(1200px 600px at 50% -10%, #e8eefc, transparent), #f6f7fb;
      }
      .auth-brand {
        display: flex;
        align-items: center;
        gap: 0.5rem;
        font-size: 1.4rem;
        font-weight: 700;
        color: #1b3a8f;
      }
      .auth-brand .logo {
        color: #3b6cf6;
      }
      .auth-card {
        width: 100%;
        max-width: 420px;
        background: #fff;
        border-radius: 16px;
        box-shadow: 0 10px 40px rgba(20, 40, 90, 0.12);
        padding: 2rem;
      }
      .auth-foot {
        color: #8a93a6;
        font-size: 0.8rem;
      }
    `,
  ],
})
export class AuthLayout {}

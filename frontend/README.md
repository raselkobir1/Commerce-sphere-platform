# CommerceSphere — Angular Frontend

Angular 22 single-page app for the CommerceSphere platform. Role-aware: **Admins** get a
management dashboard; **Customers** get a storefront (browse → cart → checkout). Talks to the
backend through the API Gateway (`http://localhost:5000`).

## Stack

- **Angular 22** standalone components, **signals** for state, functional guards & interceptors.
- **Angular Material** (Azure/Blue theme) + SCSS.
- **Vitest** (`@angular/build:unit-test`) for unit tests.
- No NgRx — state lives in small signal-based services (`AuthService`, `CartStore`).

## Run

```bash
cd frontend
npm install          # first time only
npm start            # ng serve → http://localhost:4200 (talks to gateway on :5000)
```

The backend must be running (`docker compose up -d` at the repo root). The gateway has CORS
configured for `http://localhost:4200` (see `Cors:AllowedOrigins`).

```bash
npm test             # unit tests (Vitest)
npm run build        # production bundle → dist/frontend
```

## Environments

`apiBaseUrl` is the only environment knob.

| File | Used by | apiBaseUrl |
|---|---|---|
| `src/environments/environment.development.ts` | `ng serve` | `http://localhost:5000` |
| `src/environments/environment.ts` | `ng build` (prod) | set to your deployed gateway origin |

## Architecture

```
src/app/
  core/                 # cross-cutting singletons
    auth/               # AuthService (signal state), AuthApiService (every endpoint), TokenStorage
    http/               # ApiService — unwraps the ApiResponse<T> envelope
    interceptors/       # error · correlation-id · auth-token · refresh-on-401
    guards/             # authGuard · roleGuard · guestGuard
    models/             # typed contracts mirroring the backend DTOs
    notifications/      # NotificationService (Material snackbars)
  layouts/              # AuthLayout · AdminLayout · StoreLayout
  features/
    auth/               # login (+2FA/OTP step-up), register, forgot/reset, verify-email, SSO callback
    account/            # profile, security (change password · 2FA · OTP · sessions) — shared by both shells
    admin/              # dashboard, users, products (CRUD), inventory
    storefront/         # catalog, product detail, cart, checkout (+ CartStore, product/cart API)
```

### Auth flow

- Login returns either tokens or a **challenge** (2FA / email OTP). The login page detects the
  challenge and shows a step-up code screen, then calls `2fa/verify` or `otp/verify`.
- Tokens are stored by `TokenStorageService`; `AuthService` holds the current `User` as a signal.
- After login the user is routed by role: **Admin → `/admin`**, **Customer → `/shop`**.
- The **refresh interceptor** transparently refreshes an expired access token on a 401 (single-flight:
  concurrent requests queue and retry once the new token lands); if refresh fails, the user is logged out.
- Every request carries an `X-Correlation-Id` for end-to-end tracing parity with the backend.

### Auth API coverage

Every Auth endpoint is wired in `core/auth/auth-api.service.ts` and surfaced in the UI:
register, login, refresh, revoke, me, users (admin), 2fa/otp verify, profile update, change password,
sessions (list + revoke-all), email verify (send/resend/confirm), password forgot/reset,
2fa setup/confirm/disable, otp toggle, and sso providers.

## Tests

`AuthApiService` (all endpoints), `TokenStorageService`, the three guards, `AuthService`
(login branching, logout, hydrate), and the refresh-on-401 interceptor (retry + logout paths)
are covered with `HttpTestingController`. Run `npm test`.

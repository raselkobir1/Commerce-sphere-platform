# CommerceSphere Tests

Automated test suite for CommerceSphere. The **Auth** service is fully covered; other services
will follow the same two-project pattern.

## Projects

| Project | Type | What it covers | Needs Docker? |
|---|---|---|---|
| `CommerceSphere.AuthService.UnitTests` | Unit | Domain entities + all Application managers (mocked dependencies) | No |
| `CommerceSphere.AuthService.IntegrationTests` | Integration | Every Auth HTTP endpoint through the real ASP.NET pipeline, EF Core, and Redis | Yes |

## Running

```bash
# Unit tests only — fast, no infrastructure
dotnet test tests/CommerceSphere.AuthService.UnitTests/CommerceSphere.AuthService.UnitTests.csproj

# Integration tests — spins up throwaway PostgreSQL + Redis via Testcontainers (Docker must be running)
dotnet test tests/CommerceSphere.AuthService.IntegrationTests/CommerceSphere.AuthService.IntegrationTests.csproj

# Everything (from the solution)
dotnet test CommerceSphere.slnx
```

## Unit tests (`CommerceSphere.AuthService.UnitTests`)

Pure, fast, no I/O. Dependencies are mocked with **Moq**; assertions use **FluentAssertions**.
A `FakeUnitOfWork` (in-memory repositories) lets manager tests exercise real read-after-write
behaviour without a database.

- `Domain/UserTests`, `Domain/RefreshTokenTests` — entity rules: email normalisation, lockout
  after 5 failed logins, token generation/expiry, 2FA/OTP state transitions, token rotation.
- `Managers/AuthManagerTests` — register/login/refresh/revoke, the 2FA-vs-OTP challenge branching,
  account lockout, anti-enumeration.
- `Managers/AccountManagerTests` — profile, change/forgot/reset password, email verification, sessions.
- `Managers/TwoFactorManagerTests`, `Managers/OtpManagerTests` — setup/confirm/verify/disable flows.
- `Services/TotpServiceTests` — the real `TotpService` against the real Otp.NET library.

## Integration tests (`CommerceSphere.AuthService.IntegrationTests`)

`AuthApiFactory` (a `WebApplicationFactory<Program>`) boots the **real** API pipeline — controllers,
middleware, JWT auth, EF Core — against disposable **PostgreSQL** and **Redis** containers started
by [Testcontainers](https://dotnet.testcontainers.org/). Only the external edges are faked:

- `FakeEmailService` — captures verification tokens, reset tokens, and OTP codes so tests can read
  what would have been emailed (no SMTP).
- `FakeUserEventProducer` — swallows the `user-created` Kafka event (no broker).
- `FakeKeycloakService` — returns a static provider list (no Keycloak server).

Notes:
- The schema is created with `RelationalDatabaseCreator.CreateTablesAsync()` from the current EF
  model (the committed migration files are applied to the running stack via raw SQL and lack the
  designer attributes EF's migrator needs, so tests build the schema from the model instead).
- Each test uses a unique email, so the shared container needs no per-test reset.
- TOTP codes are computed from the setup secret with Otp.NET — the same algorithm the server validates.

## Coverage at a glance

Every Auth endpoint is asserted end to end: `register`, `login` (incl. 2FA/OTP challenge branches),
`refresh-token`, `revoke-token`, `me`, `users` (role-gated), `PATCH me`, `change-password`,
`sessions` (list + revoke-all), `email/verify/{send,resend,confirm}`, `password/{forgot,reset}`,
`2fa/{setup,confirm,verify,disable}`, `otp/{toggle,verify}`, and `sso/providers` — plus the
auth-guard (401) and role (403) paths.

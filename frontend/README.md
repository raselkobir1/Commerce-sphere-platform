# CommerceSphere — Frontend

Two separate Angular 22 apps that share one workspace (so there's a single `npm install`):

| App | Folder | For | Dev URL |
|---|---|---|---|
| **AdminSphere** | `admin/` | Store admins — manage products, categories, inventory, users | http://localhost:4200 |
| **ShopSphere** | `shop/` | Customers — browse products, cart, checkout | http://localhost:4300 |

Both talk to the backend through the API Gateway at `http://localhost:5000`.

## Run

```bash
cd frontend
npm install        # first time only (one install for both apps)

npm run admin      # AdminSphere → http://localhost:4200
npm run shop       # ShopSphere  → http://localhost:4300
```

Run each in its own terminal. The backend must be up (`docker compose up -d` at the repo root).
The gateway allows CORS from both ports (`Cors:AllowedOrigins`).

```bash
npm run build:admin   # production bundle → dist/admin
npm run build:shop    # production bundle → dist/shop
npm run build         # build both
```

## API base URL

Each app sets the gateway URL in one place — `src/app/core/api.ts` (`API_URL`). It defaults to
`http://localhost:5000`; change it for production.

## Layout

Both apps follow the same simple shape (no extra abstractions, plain services + signals):

```
admin/ (and shop/)
  src/app/
    core/          # api.ts (tiny HTTP helper) · auth.ts · auth.interceptor.ts · models.ts
    pages/         # one folder per screen
    app.routes.ts  # the routes
```

### AdminSphere — `admin/`
- **Login** (admins only)
- **Dashboard** — product / customer / low-stock counts
- **Products** — list, create, edit, activate/deactivate
- **Categories** — read-only list of categories in use *(see note below)*
- **Inventory** — view stock and set quantities
- **Users** — read-only customer list

### ShopSphere — `shop/`
- **Catalog** — search + category filter, add to cart
- **Product detail**
- **Cart** — change quantities, remove items
- **Checkout** — places the order (triggers the backend checkout saga)
- **Sign in / Register**

## Notes & backend limitations

- **Categories** are just a text field on each product — there is no Category table or CRUD
  endpoint. AdminSphere's Categories page therefore lists the categories currently in use and is
  read-only. Add a Category entity + endpoints to the Product service for full management.
- **Users** — the Auth service only exposes a read-only admin listing (`GET /api/auth/users`).
  There are no create/disable/role-change endpoints yet, so the Users page is read-only.
- These apps handle the common **email + password** login. Accounts with 2FA/OTP enabled show a
  friendly "extra verification not supported here" message.

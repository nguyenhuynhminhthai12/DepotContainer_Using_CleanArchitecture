# TechSpherex Depot — Angular Frontend

Standalone Angular 19 client that talks to the .NET 10 Container Depot API.

## Features

| Module | Purpose |
|---|---|
| `core/services` | Typed wrappers around REST endpoints (`yard`, `containers`, `gate`, `delivery-orders`, `reports`) |
| `core/interceptors/auth.interceptor.ts` | Attaches `Authorization: Bearer <jwt>` and `X-Tenant-Id` to every request |
| `core/guards/auth.guard.ts` | Redirects unauthenticated users to `/login` |
| `features/login` | Email + password sign-in (seeded admin: `admin@TechSpherex.dev / Admin@123`) |
| `features/yard-map` | Block × Bay × Row × Tier occupancy grid (green = occupied) |
| `features/containers` | Paginated list with search + condition filter |
| `features/gate` | Gate In / Gate Out forms with delivery-order authorisation |
| `features/delivery-orders` | Active release orders with line quantities |
| `features/reports` | Yard aging (0-10 / ≥10 days) and daily throughput tables |

## Running

```bash
cd client
npm install
npm start
```

The dev server runs on `http://localhost:4200` and proxies `/api/*` to the .NET API at `http://localhost:8080` (configured in `proxy.conf.json`). The CORS allowlist in `src/Api/appsettings.json` already includes `http://localhost:4200`.

## Build

```bash
npm run build      # outputs to client/dist/techspherex-depot-client
```

The build target is Angular 19 standalone components — no NgModules. See `tsconfig.json` and `angular.json` for details.

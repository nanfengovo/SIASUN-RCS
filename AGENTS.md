# AGENTS.md

## Cursor Cloud specific instructions

This repo is the **SIASUN RCS (Robot Control System) dispatching platform**. It has two runnable
apps plus two infra services:

| Service | Path | Stack | Dev port |
| --- | --- | --- | --- |
| Backend API | `Siasun.RCS/BackEnd/src/SIASUN.RCS.HttpApi.Host` | .NET 10 + ABP (OpenIddict, EF Core, Serilog) | `9000` |
| Frontend UI | `Siasun.RCS/FrontEnd/SIASUN.RCS.UI` | Vue 3 + Vite 8 + pnpm (SoybeanAdmin template) | `9527` |
| SQL Server 2022 | (docker container `rcs-db`) | mssql/server:2022-latest | `1433` |
| Redis 7.2 | (docker container `rcs-redis`) | redis:7.2-alpine | `6379` |

The startup update script only refreshes dependencies (`dotnet restore`, `pnpm install`). It does
**not** start Docker, containers, or the app servers — start those yourself as below.

### Toolchain (already installed in the snapshot)
- `.NET SDK`: both GA `10.0.400` and `10.0.100-rc.2` runtimes live in `~/.dotnet`. `dotnet`, `abp`,
  `node` (v24), `pnpm`, `yarn`, `npm` are symlinked into `/usr/local/bin`, so they work in any shell.
- The ABP CLI (`abp`) requires the **GA** runtime (`10.0.11`); the rc.2 runtime alone is not enough.
- Node **24** is required for `abp install-libs` (the `select2` dependency needs node `>=24`). It is
  set as the nvm default. Node 22 is fine for the frontend but not for `abp install-libs`.

### Start infrastructure (Docker is not auto-started)
```bash
sudo dockerd &                         # daemon uses fuse-overlayfs (configured in /etc/docker/daemon.json)
sudo docker start rcs-db rcs-redis     # containers already exist; recreate only if missing (see below)
```
Recreate containers only if they are gone:
```bash
sudo docker network create rcs-net 2>/dev/null
sudo docker run -d --name rcs-db --network rcs-net -p 1433:1433 \
  -e ACCEPT_EULA=Y -e 'MSSQL_SA_PASSWORD=abc,1234' -e TZ=Asia/Shanghai \
  mcr.microsoft.com/mssql/server:2022-latest
sudo docker run -d --name rcs-redis --network rcs-net -p 6379:6379 \
  redis:7.2-alpine redis-server --requirepass 'abc,123' --appendonly yes
```

> **DB password gotcha:** `appsettings.json` ships `Password=abc,123`, but SQL Server rejects it
> (< 8 chars fails the password policy). The container uses `abc,1234` instead, so you must override
> the connection string via the `ConnectionStrings__Default` env var when running the backend /
> migrator (see below). The Host module does **not** use Redis; Redis is only wired up in the
> production `docker-compose.yml`.

### Backend (.NET 10 ABP API)
```bash
CONN='Server=localhost,1433;Database=SIASUN.RCSCore;User Id=sa;Password=abc,1234;Encrypt=True;TrustServerCertificate=True;'

# One-time (and after new EF migrations): create + seed the DB. Seeds admin user admin / 1q2w3E*
ConnectionStrings__Default="$CONN" \
  dotnet run --project Siasun.RCS/BackEnd/src/SIASUN.RCS.DbMigrator

# Client-side libs for the MVC/Swagger UI. REQUIRED once after a fresh checkout, else /health-status
# and Swagger return HTTP 500 with "The Libs Folder is Missing!". Needs node 24 + abp CLI.
(cd Siasun.RCS/BackEnd/src/SIASUN.RCS.HttpApi.Host && abp install-libs)

# Run the API (Development uses an auto-generated dev signing cert; no openiddict.pfx needed)
cd Siasun.RCS/BackEnd/src/SIASUN.RCS.HttpApi.Host
ASPNETCORE_ENVIRONMENT=Development ConnectionStrings__Default="$CONN" dotnet run
```
- Health: `http://localhost:9000/health-status` → `{"status":"Healthy",...}`. Swagger: `/swagger`.
- The `wwwroot/libs` check is evaluated at startup, so run `abp install-libs` **before** starting the
  host; if you install libs while it is running, restart the host.
- Build / test (matches CI): `dotnet build SIASUN.RCS.slnx -c Release` and
  `dotnet test SIASUN.RCS.slnx`. Only `SIASUN.RCS.EntityFrameworkCore.Tests` contains tests (3); the
  other test projects are placeholders and report "No test is available" — that is expected.
- **OAuth over HTTP:** the token/`/connect/token` endpoint rejects HTTP because
  `AuthServer:RequireHttpsMetadata=true`. To exercise auth (Swagger login, password grant) over plain
  HTTP in dev, add `AuthServer__RequireHttpsMetadata=false` to the run env. Example password-grant
  login (public client `RCS_App`, scope `RCS`): `POST /connect/token`
  `grant_type=password&client_id=RCS_App&username=admin&password=1q2w3E*&scope=RCS`.

### Frontend (Vue 3 / Vite)
```bash
cd Siasun.RCS/FrontEnd/SIASUN.RCS.UI
pnpm dev        # vite --mode test, serves on http://localhost:9527
```
- Lint/typecheck/build (CI runs typecheck + build): `pnpm typecheck`, `pnpm build`, `pnpm lint`.
  Note `pnpm lint` runs `oxlint --fix && eslint --fix .` (auto-fixes files); use
  `pnpm exec oxlint` / `pnpm exec eslint .` for a non-mutating check.
- **The frontend talks to an ApiFox online mock, not the local .NET backend.**
  `.env.test`/`.env.prod` set `VITE_SERVICE_BASE_URL=https://mock.apifox.cn/...`. The mock now
  requires a token, which is baked into `src/service/request/index.ts` (`apifoxToken` header), so
  login works out of the box with demo creds **`Soybean` / `123456`** and reaches the dashboard.
  The SoybeanAdmin API contract (`code: "0000"`) does not match ABP/OpenIddict, so the UI is not
  wired to the real backend — outbound access to `mock.apifox.cn` is required for the UI to function.

### Pre-commit hook gotcha
`pnpm install` (via `simple-git-hooks`) installs a repo-root `pre-commit` hook that runs
`pnpm typecheck && pnpm lint && pnpm fmt && git diff --exit-code`. That command assumes the frontend
package root, so committing from `/workspace` can fail. Commit with `git commit --no-verify`, or run
the checks from `Siasun.RCS/FrontEnd/SIASUN.RCS.UI` first.

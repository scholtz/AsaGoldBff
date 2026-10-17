# CLAUDE.md

Guidance for working in this repo.

## What this is

AsaGoldBff is the backend-for-frontend (BFF) for [ASA.gold](https://www.asa.gold), a gold tokenisation project on Algorand. It is an ASP.NET Core (net10.0) Web API that sits between the frontend ([gold-asa-frontend](https://github.com/scholtz/gold-asa-frontend)) and the [AsaGoldRepository](https://github.com/scholtz/AsaGoldRepository) service. It handles user account lookup, email verification, and an IPFS caching proxy.

## Layout

- `AsaGoldBff/` – the web app
  - `Program.cs` – all DI/config wiring: Swagger, ARC-14 Algorand auth, CORS (required, throws if `Cors` is empty), `Admins`, and email sender selection
  - `Controllers/ApiController.cs` – `api/v1/*` endpoints (account, send-verification-email, verify-code-from-email, update-profile, profile, rfq, rfq/{id}, dao-managed-accounts); all are `[Authorize]`d except `dao-managed-accounts`, which is anonymous; `[Authorize]` uses ARC-14 auth (only this controller has auth attributes)
  - `Controllers/IpfsController.cs` – `GET /ipfs/{hash}`: memory cache -> local `ipfs/` disk cache -> private IPFS (`Ipfs:IpfsUrl`) -> public gateways; the private node is queried only if `IpfsUrl`, `User` and `Password` are all set (1 s timeout; any failure, timeout or non-success status (e.g. 404) silently falls through to the public gateways; a hit is cached in memory and on disk, returned without pinning or trying public gateways); a public-gateway hit is pinned to the private node only if `ApiUrl`, `User` and `Password` are all set. The endpoint is anonymous (`[Authorize]` is commented out) and has no rate limit
  - `Controllers/Email/` – `IEmailSender` implementations (note: `SendGridEmailSender.cs` declares class `SendGridController`); Program.cs picks the first configured: MailGun > Azure > SendGrid > RabbitMQ > `NoEmailSender`
  - `UseCase/` – business logic (`AccountUseCase`, `EmailValidationUseCase`), registered as singletons
  - `Model/` – DTO folders: `Config` (`BFFOptions`, `IpfsOptions`), `Settings` (email/SMS provider configs), `Email`, `SMS`, `Cache`, `Result`, `Auth`
  - `OpenAPIs/swagger.json` – the AsaGoldRepository spec; NSwag generates the `AsaGoldRepository.Client` from it at build time (namespace `AsaGoldRepository`). Do not hand-edit generated code; update the JSON instead.
  - `doc/readme.md` – loaded at startup as the Swagger description (copied to output)
  - `EmailTemplates/` – Handlebars HTML templates (copied to output)
- `AsaGoldBffTests/` – NUnit + Moq tests (`EmailValidationUseCaseTests`, `IpfsControllerTests`)
- `.github/workflows/compose.yaml` – on push to `master`, SSHes to the server and runs `deploy.sh`. **Merging to master deploys.**

## Commands

Run from the repo root (`AsaGoldBff.sln`):

```
dotnet restore
dotnet build
dotnet test
dotnet run --project AsaGoldBff        # Swagger UI at /swagger
docker build -f AsaGoldBff/Dockerfile .  # build context is the repo root
```

The projects target **net10.0** (Dockerfile uses the 10.0 SDK/aspnet images). Tests: `StartupSmokeTests` boot the app in-process (Swagger generation, auth); `EmailValidationUseCaseTests` are `[Explicit]` integration tests needing AsaGoldRepository on `https://localhost:44333` and Algorand testnet, run them with `dotnet test --filter Category=Integration`.

## Configuration

`appsettings.json` holds defaults. Secrets must come from user-secrets / environment, never committed (`UserSecretsId` is set on the web project). Notable sections: `BFF`, `AlgorandAuthentication` (algod server/token), `Ipfs` (`IpfsUrl`, `ApiUrl`, `User`, `Password`), `Cors`, `Admins`, and one email provider section (`MailGun`, `Azure`, `SendGrid`, `RabbitMQEmail`). Logging uses NLog (`nlog.config`).

## Conventions and gotchas

- Controllers stay thin; put logic in `UseCase/` and inject via constructor.
- New email providers: implement `IEmailSender` and add a block in the `#region Email` chain in `Program.cs`.
- Keep `IpfsController` testable: its constructor takes `IMemoryCache`, `ILogger`, `IOptions<IpfsOptions>`. Its disk cache writes to a relative `ipfs/` directory (relative to the process working directory, e.g. `AsaGoldBff/ipfs` under `dotnet run`; it is not git-ignored, so never commit it); new tests that touch it must clean it up (the existing tests do not), or stale files leak between runs.
- Known issue: `IpfsController` builds file paths (`ipfs/{hash}.data`) straight from the route `hash` with no validation, so path traversal (read and write of gateway-supplied bytes) may be possible on this anonymous route (not verified; depends on how routing decodes encoded separators), and the same unvalidated value goes into the cache key and outbound gateway URLs. Do not copy that pattern; allow-list the hash (`^[A-Za-z0-9]+$`) before any use and reject with a 400 (needs `Get` to return `IActionResult` instead of `FileResult`). Note every thrown exception, including the existing `Not found`, becomes a 400 via the global handler. `Get` also carries `[ResponseCache(Duration = 1000000)]`, so a returned error may be cached (unverified; the 400 is produced by the global exception handler); consider `NoStore` on error paths. Also change the final `throw new Exception("Not found")` to a 404. The public-gateway path also lacks the `?? "application/octet-stream"` Content-Type fallback the private path has.
- Some files (e.g. `IpfsController.cs`) carry a UTF-8 BOM; check with `head -c3 file | xxd` before rewriting a file. `.gitattributes` only sets `* text=auto`, so git normalises line endings; keep each file's existing BOM and don't mass-convert endings.
- New tests should not hit the network or real providers; mock `IEmailSender`, repository clients, etc. (existing `IpfsControllerTests` use real hostnames because `IpfsController` news up `RestClient` directly, with no injection seam).
- Never push to master directly: work on a feature branch and open a PR; master auto-deploys, and CI does not build or run tests (the workflow only SSHes and runs `deploy.sh`), so run `dotnet build` and `dotnet test` locally before merging, plus `docker build -f AsaGoldBff/Dockerfile .` to catch Dockerfile/asset problems; `deploy.sh` lives on the server, not in this repo, so build failures there are not visible from here.

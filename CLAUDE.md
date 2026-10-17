# CLAUDE.md

Guidance for working in this repo.

## What this is

AsaGoldBff is the backend-for-frontend (BFF) for [ASA.gold](https://www.asa.gold), a gold tokenisation project on Algorand. It is an ASP.NET Core (net7.0) Web API that sits between the frontend ([gold-asa-frontend](https://github.com/scholtz/gold-asa-frontend)) and the [AsaGoldRepository](https://github.com/scholtz/AsaGoldRepository) service. It handles user account lookup, email verification, and an IPFS caching proxy.

## Layout

- `AsaGoldBff/` – the web app
  - `Program.cs` – all DI/config wiring: Swagger, ARC-14 Algorand auth, CORS (required, throws if `Cors` is empty), `Admins`, and email sender selection
  - `Controllers/ApiController.cs` – `api/v1/*` endpoints (account, send-verification-email, verify-code-from-email); `[Authorize]` uses ARC-14 auth
  - `Controllers/IpfsController.cs` – `GET /ipfs/{hash}`: memory cache -> local `ipfs/` disk cache -> private IPFS (`Ipfs:IpfsUrl`) -> public gateways; successful public fetches are pinned to the private node (`Ipfs:ApiUrl`)
  - `Controllers/Email/` – `IEmailSender` implementations; Program.cs picks the first configured: MailGun > Azure > SendGrid > RabbitMQ > `NoEmailSender`
  - `UseCase/` – business logic (`AccountUseCase`, `EmailValidationUseCase`), registered as singletons
  - `Model/` – DTOs: `Config` (bound options), `Settings` (email/SMS provider configs), `Email`, `SMS`, `Cache`, `Result`, `Auth`
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

The projects target **net7.0**; a .NET 7 SDK/runtime must be installed (the dev machine may only have newer SDKs, in which case builds/tests need the 7.0 runtime available).

## Configuration

`appsettings.json` holds defaults. Secrets must come from user-secrets / environment, never committed (`UserSecretsId` is set on the web project). Notable sections: `BFF`, `AlgorandAuthentication` (algod server/token), `Ipfs` (`IpfsUrl`, `ApiUrl`, `User`, `Password`), `Cors`, `Admins`, and one email provider section (`MailGun`, `Azure`, `SendGrid`, `RabbitMQEmail`). Logging uses NLog (`nlog.config`).

## Conventions and gotchas

- Controllers stay thin; put logic in `UseCase/` and inject via constructor.
- New email providers: implement `IEmailSender` and add a block in the `#region Email` chain in `Program.cs`.
- Keep `IpfsController` testable: its constructor takes `IMemoryCache`, `ILogger`, `IOptions<IpfsOptions>`. Its disk cache writes to a relative `ipfs/` directory; tests should clean it up.
- The IPFS `hash` route value is used to build file paths (`ipfs/{hash}.data`); treat it as untrusted input.
- Files carry a UTF-8 BOM in places and CRLF line endings (see `.gitattributes`); preserve them to keep diffs small.
- Tests must not hit the network or real providers; mock `IEmailSender`, repository clients, etc.
- Commit directly to feature branches and open a PR; master auto-deploys.

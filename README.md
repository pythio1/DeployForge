# DeployForge

DeployForge is a self-hosted CI/CD platform built with .NET. It exposes an HTTP API for projects and pipelines, queues pipeline runs in RabbitMQ, and executes pipeline steps in isolated Docker containers.

The project is a practical exploration of clean layering, background processing, container isolation, secret handling, and automated testing in ASP.NET Core.

## Features

- Create projects linked to HTTPS Git repositories.
- Define ordered pipelines and shell-command steps.
- Queue runs through RabbitMQ and process them in a separate Runner.
- Track queued, running, successful, and failed pipeline runs.
- Execute steps with Docker resource and privilege restrictions.
- Clone public and private GitHub repositories.
- Encrypt repository credentials at rest with ASP.NET Core Data Protection.
- Keep GitHub tokens out of API responses, logs, and pipeline containers.
- Run unit and PostgreSQL-backed integration tests in GitHub Actions.

## Architecture

```text
HTTP client
    │
    ▼
DeployForge.Api ────── PostgreSQL
    │                      ▲
    └──── RabbitMQ ────────┤
                           │
                    DeployForge.Runner
                           │
               ┌───────────┴───────────┐
               ▼                       ▼
        trusted clone container   pipeline container
        (credential available)    (no credential)
               └──── named Docker volume ────┘
```

The solution is split into four application layers and two entry points:

- `DeployForge.Domain` — entities and business rules.
- `DeployForge.Application` — use cases and dependency abstractions.
- `DeployForge.Infrastructure` — EF Core, PostgreSQL, RabbitMQ, Docker, and Data Protection implementations.
- `DeployForge.Api` — ASP.NET Core Minimal API.
- `DeployForge.Runner` — background pipeline-run consumer.
- `tests` — unit and integration test projects.

## Technology stack

- .NET 10 and C#
- ASP.NET Core Minimal API and Swagger
- Entity Framework Core 10 with PostgreSQL 17
- RabbitMQ 4
- Docker and Docker.DotNet
- xUnit
- GitHub Actions

## Prerequisites

- [.NET SDK 10](https://dotnet.microsoft.com/download/dotnet/10.0)
- Docker with the Compose plugin
- Git

The Runner must be able to reach the Docker daemon. On Linux and macOS it uses `unix:///var/run/docker.sock` by default.

## Local setup

Clone the repository and create local configuration:

```bash
git clone https://github.com/pythio1/DeployForge.git
cd DeployForge
cp .env.example .env
```

Replace the placeholder passwords in `.env`, then start PostgreSQL and RabbitMQ:

```bash
docker compose --env-file .env -f infrastructure/docker-compose.yml up -d
```

Create a persistent directory shared by the API and Runner for Data Protection keys:

```bash
mkdir -p .data/data-protection-keys
export DEPLOYFORGE_DATAPROTECTION_KEYS_PATH="$(pwd)/.data/data-protection-keys"
```

Configure the application processes. The password values must match `.env`:

```bash
export ConnectionStrings__DefaultConnection="Host=localhost;Port=5432;Database=deployforge;Username=deployforge;Password=change_me_locally"
export RabbitMQ__HostName=localhost
export RabbitMQ__Port=5672
export RabbitMQ__UserName=deployforge
export RabbitMQ__Password=change_me_locally
```

Apply database migrations:

```bash
dotnet ef database update \
  --project src/DeployForge.Infrastructure \
  --startup-project src/DeployForge.Api
```

Start the API and Runner in separate terminals with the same environment variables:

```bash
dotnet run --project src/DeployForge.Api
```

```bash
dotnet run --project src/DeployForge.Runner
```

Swagger UI is available in the API's Development environment at `/swagger`.

## API overview

| Method | Route | Purpose |
|---|---|---|
| `POST` | `/api/projects` | Create a project |
| `GET` | `/api/projects` | List projects |
| `GET` | `/api/projects/{projectId}` | Get a project |
| `POST` | `/api/projects/{projectId}/pipelines` | Create a pipeline |
| `GET` | `/api/projects/{projectId}/pipelines` | List project pipelines |
| `GET` | `/api/projects/{projectId}/pipelines/{pipelineId}` | Get a pipeline |
| `DELETE` | `/api/projects/{projectId}/pipelines/{pipelineId}` | Delete a pipeline |
| `POST` | `/api/projects/{projectId}/pipelines/{pipelineId}/runs` | Queue a run |
| `GET` | `/api/projects/{projectId}/pipelines/{pipelineId}/runs` | List runs |
| `GET` | `/api/projects/{projectId}/pipelines/{pipelineId}/runs/{runId}` | Get a run |
| `PUT` | `/api/projects/{projectId}/repository-credential` | Add or rotate a credential |
| `GET` | `/api/projects/{projectId}/repository-credential` | Get credential metadata |
| `DELETE` | `/api/projects/{projectId}/repository-credential` | Remove a credential |

### Private GitHub repositories

DeployForge currently supports the `github` credential provider. Use a fine-grained personal access token restricted to the required repository with read-only `Contents` permission:

```bash
curl -X PUT \
  http://localhost:5148/api/projects/PROJECT_ID/repository-credential \
  -H 'Content-Type: application/json' \
  -d '{"provider":"github","secret":"YOUR_TOKEN"}'
```

The response contains only the project ID, provider, and creation time. Neither the plaintext nor encrypted token is returned.

## Security model

- Repository URLs must be absolute HTTPS URLs without embedded credentials, query strings, or fragments.
- Credentials are encrypted with Data Protection before PostgreSQL persistence.
- API and Runner must share the same persistent Data Protection key ring and application name.
- Private-repository cloning runs in a dedicated container.
- The clone container is removed before user-defined pipeline commands start.
- Source code moves between the containers through a short-lived named Docker volume.
- The execution container never receives the repository token.
- Execution containers drop all Linux capabilities, disable privileged mode, enable `no-new-privileges`, and enforce memory, CPU, process, and time limits.

Data Protection keys are sensitive operational data. Back them up securely, restrict filesystem access, and never commit `.data/` or `.env`.

## Tests

Run the unit suite:

```bash
dotnet test tests/DeployForge.UnitTests/DeployForge.UnitTests.csproj
```

Integration tests require PostgreSQL on `localhost:5432` with database `deployforge_test`, user `deployforge`, and password `dev_password`:

```bash
dotnet test tests/DeployForge.IntegrationTests/DeployForge.IntegrationTests.csproj
```

Run the complete solution checks:

```bash
dotnet restore DeployForge.sln
dotnet build DeployForge.sln --configuration Release --no-restore
dotnet test tests/DeployForge.UnitTests/DeployForge.UnitTests.csproj --configuration Release --no-build
dotnet test tests/DeployForge.IntegrationTests/DeployForge.IntegrationTests.csproj --configuration Release --no-build
```

GitHub Actions performs the same build and test flow for pushes and pull requests targeting `main`.

## Current limitations

- Only GitHub credentials are supported.
- Authentication and authorization for the DeployForge API are not implemented yet; do not expose the current API directly to an untrusted network.
- Pipeline persistence and RabbitMQ publishing are not yet atomic; an Outbox pattern is planned.
- The Runner assumes access to a trusted Docker daemon.

See [`docs/PROJECT_CONTEXT.md`](docs/PROJECT_CONTEXT.md) for detailed implementation notes and the development roadmap.

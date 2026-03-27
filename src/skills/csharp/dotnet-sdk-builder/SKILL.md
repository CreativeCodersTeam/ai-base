---
name: dotnet-sdk-builder
description: This skill should be used when the user asks to "create a .NET SDK", "build a .NET library", "generate an SDK from classes", "create an API wrapper in C#", "build a .NET integration library", "create a .NET client library", "wrap a REST API in .NET", "generate a .NET HTTP client", or "create a typed HTTP client". Generates complete .NET SDK libraries with Dependency Injection support, interfaces, typed HTTP clients, Options pattern, typed exceptions, XML documentation (via csharp-docs skill), and tests (via tester skill).
version: 0.1.0
---

# .NET SDK Library Builder

Generate complete, production-ready .NET SDK libraries from existing C# classes or API documentation. The output follows Microsoft's library design guidelines with full DI support, testability via interfaces, and idiomatic C# patterns.

## When to Use This Skill

- Creating an HTTP client / REST API wrapper library
- Building an integration library for an external service
- Wrapping existing service classes into a redistributable library
- Generating a typed SDK from OpenAPI/Swagger or Markdown documentation

## Workflow Overview

Follow these steps in order. See the reference files for detailed guidance on each phase.

### Step 1: Analyze Input

Determine what the input is:

- **Existing C# classes**: Read and understand the public API surface, method signatures, and responsibilities.
- **Documentation** (OpenAPI, Swagger JSON/YAML, Markdown): Parse endpoints, request/response models, authentication, and error responses.

### Step 2: Determine .NET Version

1. Find all `.csproj` files in the solution.
2. Extract the `<TargetFramework>` (or `<TargetFrameworks>`) value.
3. If all projects use the same version → use that version.
4. If versions differ → ask the user which version to target.
5. Enable nullable reference types based on version:
   - New project: add `<Nullable>enable</Nullable>` to `.csproj`.
   - Existing project with nullable disabled: add `#pragma warning disable CS8600` / `#nullable enable` per source file, not globally.

### Step 3: Determine Target Project

1. If the user specified a project → use it.
2. If no project specified → scan the solution for existing library projects (`.csproj` with no `Sdk="Microsoft.NET.Sdk.Web"` and no executable output).
3. If a candidate project is found → **ask the user** before adding files to it.
4. If no suitable project exists → create a new class library project. See [project-setup.md](references/project-setup.md) for conventions.

### Step 4: Derive Names

Derive the service/client name from the input:

| Input | Derived Name Example |
|---|---|
| `GitHubService` class | `GitHub` → `IGitHubClient`, `GitHubClient`, `AddGitHub(...)` |
| `PaymentsApi` class | `Payments` → `IPaymentsClient`, `PaymentsClient`, `AddPayments(...)` |
| OpenAPI `title: Stripe API` | `Stripe` → `IStripeClient`, `StripeClient`, `AddStripe(...)` |

If the name cannot be derived with confidence → ask the user.

### Step 5: Ask About Resilience

Before generating HTTP client code, ask:

> "Should resilience policies (retry, circuit breaker) be added to the HTTP client using `Microsoft.Extensions.Http.Resilience`?"

If yes → add the Polly-based resilience pipeline. See [http-client-patterns.md](references/http-client-patterns.md#resilience).

### Step 6: Generate Library Code

Generate all components. See [di-patterns.md](references/di-patterns.md) and [http-client-patterns.md](references/http-client-patterns.md) for full patterns.

**Required components:**

| Component | Description |
|---|---|
| `IXxxClient` interface | Public contract for DI and testing |
| `XxxClient` implementation | Concrete HTTP client using `IHttpClientFactory` |
| `XxxOptions` class | Configuration via Options pattern |
| `XxxServiceCollectionExtensions` | `AddXxx(...)` extension method |
| `XxxException` (+ subtypes) | Typed exceptions with diagnostic properties |
| Model classes | Request/response DTOs |

**NuGet packages to add:**

```xml
<PackageReference Include="Microsoft.Extensions.Http" Version="*" />
<PackageReference Include="Microsoft.Extensions.Options" Version="*" />
<PackageReference Include="Microsoft.Extensions.DependencyInjection.Abstractions" Version="*" />
<!-- If resilience requested: -->
<PackageReference Include="Microsoft.Extensions.Http.Resilience" Version="*" />
```

Always use the latest stable version compatible with the target .NET version.

### Step 7: Document the Code

After generating all source files, invoke the `csharp-docs` skill to add XML documentation comments to all public types and members.

### Step 8: Write Tests

After documentation is complete, invoke the `tester` skill to generate unit and integration tests for the library.

## Key Design Principles

- **Interface-first**: Every public service class must have a corresponding interface.
- **Options pattern**: Configuration always via `IOptions<XxxOptions>`, never constructor parameters for config values.
- **IHttpClientFactory**: Never inject `HttpClient` directly; always use the named/typed factory pattern.
- **Typed exceptions**: HTTP errors become typed exceptions with status code, reason, and response body properties.
- **Nullable**: Follow the project's nullable settings (see Step 2).
- **No static state**: All state via DI; no singleton anti-patterns.

## Additional Resources

- **[di-patterns.md](references/di-patterns.md)** — DI registration, Options pattern, extension method patterns
- **[http-client-patterns.md](references/http-client-patterns.md)** — IHttpClientFactory, typed clients, resilience, typed exceptions
- **[project-setup.md](references/project-setup.md)** — New project structure, folder layout, `.csproj` conventions

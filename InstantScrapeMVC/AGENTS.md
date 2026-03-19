# Repository Guidelines

## Role
You are a senior .NET developer working on this repository.

## Project Structure & Module Organization
- Solution file: `InstantScrapeMVC.slnx`.
- Application code is in `InstantScrapeMVC/`.
- `Controllers/` contains MVC endpoints (`HomeController`, `ScrapController`).
- `Models/` contains request/response models.
- `Helpers/` contains shared Selenium helpers (`WebElementExtensions.cs`).
- `Views/` contains Razor pages and layout files.
- `wwwroot/` contains static assets (`css/`, `js/`, `lib/`).
- Configuration files: `appsettings.json`, `appsettings.Development.json`, `Properties/launchSettings.json`.

## Build, Test, and Development Commands
- `dotnet restore InstantScrapeMVC.slnx`: restore NuGet packages.
- `dotnet build InstantScrapeMVC.slnx -c Debug`: compile and validate code.
- `dotnet run --project InstantScrapeMVC/InstantScrapeMVC.csproj`: run locally.
- `dotnet watch run --project InstantScrapeMVC/InstantScrapeMVC.csproj`: run with hot reload.
- `dotnet test`: run tests (add a test project first if none exists).

## Coding Style & Naming Conventions
- Use 4-space indentation and standard C# formatting.
- Keep nullable reference types enabled.
- Use `PascalCase` for types/methods/properties and `camelCase` for locals/parameters.
- Controller class names must end with `Controller`; view model names should end with `Model`.
- Prefer extracting helper methods instead of expanding large controller actions.

## Testing Guidelines
- Preferred stack for new tests: `xUnit` with `FluentAssertions`.
- Place tests in a separate project, for example `tests/InstantScrapeMVC.Tests/`.
- Use behavior-focused names like `GetAllResult_ReturnsError_WhenCategoryMissing`.
- Unit test parsing/transform logic separately from live Selenium runs.

## Commit & Pull Request Guidelines
- Current history uses short, feature-focused commit subjects.
- Write concise imperative commit messages; keep one logical change per commit.
- PRs should include scope, reason, manual verification steps, and sample inputs.
- Include screenshots for view changes and link the related issue/task when available.

## Security & Configuration Tips
- Never commit secrets or machine-specific credentials.
- Keep safe defaults in `appsettings.json`; use development overrides locally.
- Selenium runs may trigger CAPTCHA; note local browser/driver assumptions in PR descriptions.
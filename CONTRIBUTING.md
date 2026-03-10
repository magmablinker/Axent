# Contributing to Axent

Thanks for your interest in contributing to Axent.

Axent is a source-generated CQRS library for modern .NET focused on performance, minimal boilerplate, typed pipelines, and clean ASP.NET Core integration. Contributions that improve correctness, performance, developer experience, documentation, and maintainability are all welcome.

## Code of Conduct

By participating in this project, you agree to follow the repository's [Code of Conduct](CODE_OF_CONDUCT.md).

Please be respectful, constructive, and professional in all interactions.

## Ways to Contribute

There are several ways to contribute:

- Report bugs
- Suggest features or API improvements
- Improve documentation
- Add or expand tests
- Improve performance
- Improve templates, samples, or developer experience

For small fixes, feel free to open a pull request directly.

For larger changes, please open an issue or discussion first so the direction can be aligned before implementation starts.

## Repository Structure

The repository is organized into a few main areas:

- `src/` contains the main packages and templates
- `tests/` contains the test projects
- `docs/` contains feature documentation
- `samples/` contains example applications
- `benchmarks/` contains performance benchmarks

When contributing, try to place changes in the most appropriate project instead of mixing unrelated concerns.

## Getting Started

### Prerequisites

Make sure you have the following installed:

- .NET SDK 8.0 or later
- Git

### Clone the Repository

```bash
git clone https://github.com/magmablinker/Axent.git
cd Axent
```

### Restore, Build, and Test
Run the following commands before opening a pull request:

```bash
dotnet restore
dotnet build --configuration Release
dotnet test --configuration Release
dotnet format --verify-no-changes
```

These checks should pass locally before you submit changes.

## Coding Guidelines
Please follow the existing repository conventions instead of introducing a new style.
General expectations:
* Keep code simple and focused
* Follow the current naming and folder structure
* Avoid unrelated refactors in the same pull request
* Prefer consistency with the existing codebase over personal preference
* Keep public APIs minimal and clear
* Preserve performance where possible
* Avoid unnecessary abstraction

If the repository formatting differs from your local defaults, trust the repository settings and run:

```bash
dotnet format
```

## Testing Guidelines
Tests are expected for meaningful changes.
As a general rule:
* Bug fixes should include a regression test
* New features should include tests for expected behavior and edge cases
* Changes to public APIs should be covered by tests where applicable
* Generator-related changes should include generator-focused tests
* Extension packages should be tested in their matching test projects

Please keep tests close to the relevant project area rather than placing everything into a shared test location.

## Documentation Guidelines
Documentation is part of the contribution.
Please update documentation when you:
* Add a feature
* Change behavior
* Modify a public API
* Introduce new configuration
* Add a new extension package
* Change how users are expected to use the library

Relevant places may include:
* `README.md`
* `docs/`
* `samples/`
* changelog entries if applicable

Clear documentation makes the library easier to adopt and maintain.

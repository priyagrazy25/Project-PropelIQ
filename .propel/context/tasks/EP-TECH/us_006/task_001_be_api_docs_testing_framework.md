---
post_title: "TASK_001 - API Documentation & Testing Framework"
author1: "AI Senior Developer"
post_slug: "task-001-be-api-docs-testing-framework"
microsoft_alias: "N/A"
featured_image: "N/A"
categories: "Healthcare, Development Tasks"
tags: "EP-TECH, US_006, backend, Swagger, OpenAPI, xUnit, Playwright"
ai_note: "Generated with AI assistance from user story US_006"
summary: "Configure OpenAPI 3.0 Swagger documentation and testing infrastructure with xUnit, WebApplicationFactory, and Playwright."
post_date: "2026-04-16"
---

# Task - TASK_001_BE_API_DOCS_TESTING_FRAMEWORK

## Requirement Reference
- User Story: us_006
- Story Location: .propel/context/tasks/EP-TECH/us_006/us_006.md
- Acceptance Criteria:
    - AC-1: Swagger UI accessible at /swagger with all endpoints documented
    - AC-2: xUnit test project with sample test, WebApplicationFactory configured for integration tests
    - AC-3: Playwright test project scaffolded for E2E tests
    - AC-4: OpenAPI spec validates without errors and includes request/response examples
    - AC-5: Coverage reporting configured targeting 80% unit / 60% integration
- Edge Cases:
    - Endpoint missing XML documentation → Swagger displays warning annotation
    - WebApplicationFactory fails to start → test output includes clear startup error diagnostics

## Design References (Frontend Tasks Only)
| Reference Type | Value |
|----------------|-------|
| **UI Impact** | No |
| **Figma URL** | N/A |
| **Wireframe Status** | N/A |
| **Wireframe Type** | N/A |
| **Wireframe Path/URL** | N/A |
| **Screen Spec** | N/A |
| **UXR Requirements** | N/A |
| **Design Tokens** | N/A |

## Applicable Technology Stack
| Layer | Technology | Version |
|-------|------------|---------|
| API Documentation | Swashbuckle (OpenAPI 3.0) | 6.x |
| Unit Testing | xUnit + Moq | 2.x / 4.x |
| Integration Testing | WebApplicationFactory + Testcontainers | 8.0 / 3.x |
| E2E Testing | Playwright | 1.x |
| Coverage | coverlet | Latest |
| Backend | ASP.NET Core | 8.0 LTS |

## AI References (AI Tasks Only)
| Reference Type | Value |
|----------------|-------|
| **AI Impact** | No |
| **AIR Requirements** | N/A |
| **AI Pattern** | N/A |
| **Prompt Template Path** | N/A |
| **Guardrails Config** | N/A |
| **Model Provider** | N/A |

## Mobile References (Mobile Tasks Only)
| Reference Type | Value |
|----------------|-------|
| **Mobile Impact** | No |
| **Platform Target** | N/A |
| **Min OS Version** | N/A |
| **Mobile Framework** | N/A |

## Task Overview
Set up the complete testing infrastructure and API documentation pipeline. Configure Swashbuckle for auto-generated OpenAPI 3.0 documentation with request/response examples. Create xUnit test project with WebApplicationFactory for integration tests and Testcontainers for containerized SQL Server in CI. Scaffold Playwright for E2E testing. Configure coverlet for code coverage targeting 80% unit and 60% integration.

## Dependent Tasks
- task_001_be_modular_monolith_setup (US_002) — Requires project structure for test project references

## Impacted Components
- NEW: xUnit unit test project per module
- NEW: Integration test project with WebApplicationFactory
- NEW: Playwright E2E test project
- NEW: Swagger/OpenAPI configuration
- NEW: Coverage configuration

## Implementation Plan
1. Configure Swashbuckle in Program.cs with XML documentation, response types, and example values
2. Enable XML documentation generation in all API projects (.csproj)
3. Create xUnit test project with module-specific test folders and Moq for mocking
4. Configure WebApplicationFactory with in-memory test server and Testcontainers SQL Server
5. Create Playwright .NET test project with base page objects and configuration
6. Configure coverlet for coverage collection with 80% unit / 60% integration thresholds
7. Add sample tests: one unit test, one integration test, one Playwright test
8. Verify Swagger UI loads and all test runners execute successfully

## Current Project State
```
[PLACEHOLDER - Updated after US_002 backend setup]
```

## Expected Changes
| Action | File Path | Description |
|--------|-----------|-------------|
| MODIFY | backend/src/Host/Program.cs | Add Swagger/OpenAPI configuration |
| CREATE | backend/tests/UnitTests/UnitTests.csproj | xUnit unit test project |
| CREATE | backend/tests/IntegrationTests/IntegrationTests.csproj | WebApplicationFactory integration test project |
| CREATE | backend/tests/IntegrationTests/CustomWebApplicationFactory.cs | Configured test server factory |
| CREATE | backend/tests/E2ETests/E2ETests.csproj | Playwright E2E test project |
| CREATE | backend/tests/coverlet.runsettings | Coverage collection configuration |

## External References
- Swashbuckle Docs: https://learn.microsoft.com/en-us/aspnet/core/tutorials/getting-started-with-swashbuckle
- xUnit Docs: https://xunit.net/docs/getting-started/netcore/cmdline
- WebApplicationFactory: https://learn.microsoft.com/en-us/aspnet/core/test/integration-tests
- Playwright .NET: https://playwright.dev/dotnet/docs/intro

## Build Commands
- `dotnet test` — Run all tests
- `dotnet test --collect:"XPlat Code Coverage"` — Run with coverage

## Implementation Validation Strategy
- [x] Swagger UI accessible at /swagger with auto-generated docs
- [x] `dotnet test` runs all test projects without failures
- [x] Coverage report generates with threshold checks

## Implementation Checklist
- [x] Configure Swashbuckle with XML docs, response types, and examples
- [x] Enable XML documentation generation in all API .csproj files
- [x] Create xUnit unit test project with Moq and sample tests
- [x] Configure WebApplicationFactory with Testcontainers SQL Server
- [x] Scaffold Playwright .NET E2E test project with base config
- [x] Configure coverlet coverage thresholds (80% unit / 60% integration)
- [x] Add sample unit, integration, and E2E tests as templates
- [x] Verify Swagger UI, test runners, and coverage reporting all functional

# GitHub Actions Pipeline Documentation

## Workflow Files

| Workflow | File | Trigger | Purpose |
|----------|------|---------|---------|
| CI Pipeline | `ci.yml` | Push/PR to develop, main, feature/* | Build, lint, test, PHI scan |
| Security Scan | `security-scan.yml` | Push/PR to main, develop + weekly | SAST, SCA, container, secrets, IaC scan |
| Deploy Dev | `cd-dev.yml` | Push to develop | Rolling deploy to dev |
| Deploy Staging | `cd-staging.yml` | Push to main, rc tags | Blue/Green deploy with 1 approval |
| Deploy Prod | `cd-prod.yml` | Version tags (v*.*.*) | Canary deploy with 2 approvals |
| Terraform Plan | `terraform-plan.yml` | PR affecting IaC files | Plan + security scan for infra changes |

## Required Secrets

| Secret | Scope | Purpose |
|--------|-------|---------|
| `AZURE_CLIENT_ID` | Repository | OIDC auth |
| `AZURE_TENANT_ID` | Repository | OIDC auth |
| `AZURE_SUBSCRIPTION_ID` | Repository | OIDC auth |
| `GCP_WORKLOAD_IDENTITY_PROVIDER` | Repository | GCP OIDC auth |
| `GCP_SERVICE_ACCOUNT` | Repository | GCP service account |
| `SNYK_TOKEN` | Repository | Vulnerability scanning |
| `CODECOV_TOKEN` | Repository | Coverage reporting |
| `SLACK_WEBHOOK_URL` | Repository | Notifications |
| `DEV_SQL_CONNECTION` | Environment: dev | Database connection |
| `TEST_SQL_PASSWORD` | Repository | Test database |

## Environment Protection Rules

| Environment | Approvers | Timeout | Deployment Branch |
|-------------|-----------|---------|-------------------|
| dev | None | N/A | develop |
| staging | 1 reviewer | 24h | main |
| production | 2 reviewers | 72h | tags: v*.*.* |

## Security Gates (Non-Bypassable)

All security gates use `continue-on-error: false` per cicd-pipeline-standards:

- CodeQL SAST: 0 Critical/High
- Snyk SCA: 0 Critical/High
- Trivy Container: 0 Critical
- GitLeaks Secrets: 0 findings
- tfsec IaC: 0 Critical
- PHI Pattern Scan: 0 findings

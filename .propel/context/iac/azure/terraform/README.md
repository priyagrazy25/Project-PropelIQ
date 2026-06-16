# Azure Terraform Infrastructure

Infrastructure-as-Code for the Unified Patient Access & Clinical Intelligence Platform on Azure.

## Module Structure

| Module | Purpose | Key Resources |
|--------|---------|---------------|
| `networking` | VNet, subnets, NSGs, NAT Gateway | VNet, 3 subnets, 2 NSGs, NAT GW |
| `compute` | App Service, SignalR | Linux Web App, Service Plan, Auto-scale |
| `database` | Azure SQL with private endpoint | SQL Server, SQL Database, Private Endpoint |
| `storage` | Blob Storage with lifecycle mgmt | Storage Account, 3 containers |
| `security` | Key Vault for secrets | Key Vault with RBAC |
| `monitoring` | Logging and alerting | App Insights, Log Analytics, Alerts |

## Environments

| Environment | Compute | Database | Cache | Storage |
|-------------|---------|----------|-------|---------|
| dev | B1 (1 inst) | Basic (2 GB) | Upstash Free | LRS |
| qa | B1 (1 inst) | S0 (10 GB) | Upstash Free | LRS |
| staging | S1 (2 inst) | S2 (10 GB) | C0 Redis | LRS |
| prod | S2 (2-5 inst) | S3 (10 GB, Zone-HA) | C1 Redis | GRS |

## Usage

```bash
cd environments/dev
terraform init
terraform plan -var-file="terraform.tfvars"
terraform apply -var-file="terraform.tfvars"
```

## Security

- All secrets in Azure Key Vault (no hardcoded values)
- Managed Identity for service-to-service auth
- Private endpoints for SQL and Storage
- NSG deny-by-default with explicit allow rules
- TLS 1.2+ enforced on all resources

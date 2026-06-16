# GCP Terraform Infrastructure

Infrastructure-as-Code for the Unified Patient Access & Clinical Intelligence Platform on GCP.

## Module Structure

| Module | Purpose | Key Resources |
|--------|---------|---------------|
| `networking` | VPC, subnets, firewall, Cloud NAT | VPC, 2 subnets, firewall rules, NAT |
| `compute` | Cloud Run backend service | Cloud Run v2, VPC Connector |
| `database` | Cloud SQL for SQL Server | SQL Instance, private networking |
| `storage` | Cloud Storage with lifecycle | Buckets for docs and backups |
| `security` | IAM and Secret Manager | Service accounts, secrets |
| `monitoring` | Alerts and uptime checks | Alert policies, notification channels |

## Environments

| Environment | Compute | Database | Storage |
|-------------|---------|----------|---------|
| dev | Cloud Run (1 vCPU, 512 MB, 0-2) | Express (1 vCPU, 3.75 GB) | Standard |
| qa | Cloud Run (1 vCPU, 1 GB, 1-3) | Express (1 vCPU, 3.75 GB) | Standard |
| staging | Cloud Run (2 vCPU, 2 GB, 2-5) | Standard (2 vCPU, 7.5 GB, HA) | Standard |
| prod | Cloud Run (2 vCPU, 2 GB, 2-10) | Standard (2 vCPU, 7.5 GB, HA) | Standard |

## Usage

```bash
cd environments/dev
terraform init
terraform plan -var-file="terraform.tfvars" -var="project_id=YOUR_PROJECT"
terraform apply -var-file="terraform.tfvars" -var="project_id=YOUR_PROJECT"
```

## Security

- Workload Identity for service auth
- Secret Manager for all credentials
- Private IP for Cloud SQL (no public access)
- VPC firewall deny-by-default
- TLS enforced on all connections

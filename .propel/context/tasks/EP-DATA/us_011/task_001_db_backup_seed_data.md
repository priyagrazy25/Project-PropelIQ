---
post_title: "TASK_001 - Backup Infrastructure & Seed Data"
author1: "AI Senior Developer"
post_slug: "task-001-db-backup-seed-data"
microsoft_alias: "N/A"
featured_image: "N/A"
categories: "Healthcare, Development Tasks"
tags: "EP-DATA, US_011, database, backup, seed-data, RPO, RTO"
ai_note: "Generated with AI assistance from user story US_011"
summary: "Configure automated database backups with RPO 24h / RTO 4h and seed predefined dummy insurance records via EF Core migration."
post_date: "2026-04-16"
---

# Task - TASK_001_DB_BACKUP_SEED_DATA

## Requirement Reference
- User Story: us_011
- Story Location: .propel/context/tasks/EP-DATA/us_011/us_011.md
- Acceptance Criteria:
    - AC-1: Automated backup runs on 24-hour schedule meeting RPO per DR-014
    - AC-2: Recovery procedure verified restoring within 4-hour RTO per DR-014
    - AC-3: Backups encrypted with AES-256 in separate storage location per DR-015
    - AC-4: Seed migration inserts predefined dummy insurance records per DR-017
    - AC-5: EF Core migration with sequential versioning and rollback script per DR-016
- Edge Cases:
    - Backup storage full → alert triggered; oldest backup beyond retention rotated
    - Seed data already exists → migration is idempotent; no duplicate inserts

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
| Database | SQL Server Express | 2022 |
| ORM | Entity Framework Core | 8.0 |

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
Set up automated database backup infrastructure with AES-256 encrypted backups stored in a separate location, meeting RPO 24h and RTO 4h targets. Create an EF Core seed migration with predefined dummy insurance records (InsuranceName, ValidMemberIDPattern) for the insurance pre-check feature. Verify the full backup-and-restore cycle.

## Dependent Tasks
- task_001_db_schema_orm_setup (US_003) — Requires database and migration infrastructure
- task_001_db_integrity_validation_rules (US_010) — Requires complete schema for meaningful backups

## Impacted Components
- NEW: SQL Server backup job configuration
- NEW: Backup encryption and storage scripts
- NEW: EF Core seed migration for insurance records
- NEW: Restore verification script

## Implementation Plan
1. Create SQL Server Agent job (or scheduled script for Express edition) for daily full backups
2. Configure backup encryption with AES-256 certificate
3. Configure backup storage to separate directory/drive from primary database
4. Create restore procedure script and document RTO verification steps
5. Create EF Core seed migration V005_SeedInsuranceRecords with idempotent insurance data
6. Define dummy insurance records (InsuranceName, ValidMemberIDPattern) matching design.md DR-017
7. Generate rollback script for seed migration
8. Verify backup, restore, and seed data end-to-end

## Current Project State
```
[PLACEHOLDER - Updated after US_003, US_010 tasks]
```

## Expected Changes
| Action | File Path | Description |
|--------|-----------|-------------|
| CREATE | scripts/backup/daily-backup.sql | Automated backup script with AES-256 encryption |
| CREATE | scripts/backup/restore-verify.sql | Restore and verification procedure |
| CREATE | scripts/backup/create-backup-certificate.sql | AES-256 backup encryption certificate |
| CREATE | backend/migrations/V005_SeedInsuranceRecords/ | Seed migration with dummy insurance data |

## External References
- SQL Server Backup Encryption: https://learn.microsoft.com/en-us/sql/relational-databases/backup-restore/backup-encryption
- EF Core Data Seeding: https://learn.microsoft.com/en-us/ef/core/modeling/data-seeding

## Build Commands
- `dotnet ef migrations add V005_SeedInsuranceRecords` — Generate seed migration
- `dotnet ef database update` — Apply seed migration
- `sqlcmd -i scripts/backup/daily-backup.sql` — Execute backup

## Implementation Validation Strategy
- [x] Backup file created with AES-256 encryption
- [x] Backup stored in separate location from primary database
- [x] Full restore completes within 4 hours (RTO verification)
- [x] Seed insurance records queryable after migration

## Implementation Checklist
- [x] Create AES-256 backup encryption certificate in SQL Server
- [x] Create automated daily backup script with encrypted output
- [x] Configure backup storage to separate location from primary database
- [x] Create restore procedure and verify RTO within 4 hours
- [x] Create EF Core seed migration V005 with dummy insurance records (idempotent)
- [x] Generate rollback script for seed migration
- [x] Verify end-to-end backup → restore → data integrity cycle

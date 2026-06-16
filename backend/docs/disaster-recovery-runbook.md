# Disaster Recovery Runbook

**System**: Unified Patient Access Platform  
**Version**: 1.0  
**Last Updated**: April 2026  
**Compliance**: DR-014 (RPO 24h, RTO 4h), DR-015 (AES-256 encrypted backups)

---

## Table of Contents

1. [Overview](#overview)
2. [Recovery Objectives](#recovery-objectives)
3. [Prerequisites](#prerequisites)
4. [Recovery Procedures](#recovery-procedures)
5. [Post-Recovery Validation](#post-recovery-validation)
6. [Escalation Contacts](#escalation-contacts)
7. [Appendix](#appendix)

---

## Overview

This runbook provides step-by-step procedures for recovering the Unified Patient Access Platform database from encrypted backups. It ensures compliance with:

- **RPO (Recovery Point Objective)**: 24 hours — maximum data loss tolerance
- **RTO (Recovery Time Objective)**: 4 hours — maximum downtime tolerance
- **AIR-S01**: Zero PHI transmission to external providers during recovery

### When to Use This Runbook

- Database corruption or hardware failure
- Ransomware or security incident requiring clean restore
- Catastrophic failure of primary SQL Server instance
- Scheduled disaster recovery drill (quarterly recommended)

---

## Recovery Objectives

| Metric | Target | Measurement |
|--------|--------|-------------|
| RPO | 24 hours | Time since last successful backup |
| RTO | 4 hours | Time from incident detection to service restoration |
| Data Integrity | 100% | All schemas and tables restored without corruption |

### Estimated Recovery Timeline

| Step | Duration | Cumulative |
|------|----------|------------|
| 1. Incident Assessment | 15 min | 15 min |
| 2. Locate Latest Backup | 10 min | 25 min |
| 3. Restore Certificate | 15 min | 40 min |
| 4. Restore Database | 30-120 min | 160 min |
| 5. Verify Data Integrity | 30 min | 190 min |
| 6. Update Configuration | 15 min | 205 min |
| 7. Validate Health Checks | 15 min | 220 min |
| **Total** | **~4 hours** | |

---

## Prerequisites

### Required Access

- [ ] SQL Server `sysadmin` role on target instance
- [ ] Access to backup storage location (primary and secondary)
- [ ] Access to certificate backup files
- [ ] Certificate private key password

### Required Files

1. **Backup File**: `UnifiedPatientAccess_YYYYMMDD_HHMMSS.bak`
   - Primary: `D:\Backups\UnifiedPatientAccess\`
   - Secondary: `E:\Backups\Secondary\`

2. **Certificate Files** (stored separately from backups):
   - `UnifiedPatientAccess_BackupCert.cer`
   - `UnifiedPatientAccess_BackupCert.pvk`

### Environment Variables

```powershell
$BackupFile = "D:\Backups\UnifiedPatientAccess\UnifiedPatientAccess_YYYYMMDD_HHMMSS.bak"
$CertPath = "\\secure-share\certificates"
$CertPassword = "<RETRIEVE FROM SECURE VAULT>"
$DataDir = "D:\SQLData"
$LogDir = "D:\SQLLogs"
```

---

## Recovery Procedures

### Step 1: Assess Incident (15 min)

1. **Confirm Failure Scope**
   ```sql
   -- Check if database is accessible
   SELECT state_desc FROM sys.databases WHERE name = 'UnifiedPatientAccess';
   ```

2. **Document Incident**
   - Record timestamp of failure detection
   - Note error messages and symptoms
   - Identify affected components (scheduling, clinical, identity)

3. **Notify Stakeholders**
   - IT Operations Lead
   - Clinical Systems Manager
   - Compliance Officer (for PHI-related incidents)

### Step 2: Locate Latest Valid Backup (10 min)

1. **Check Primary Backup Location**
   ```powershell
   Get-ChildItem "D:\Backups\UnifiedPatientAccess\*.bak" | 
       Sort-Object LastWriteTime -Descending | 
       Select-Object -First 5
   ```

2. **If Primary Unavailable, Check Secondary**
   ```powershell
   Get-ChildItem "E:\Backups\Secondary\*.bak" | 
       Sort-Object LastWriteTime -Descending | 
       Select-Object -First 5
   ```

3. **Verify Backup Age (RPO Check)**
   - Backup must be less than 24 hours old
   - If older, escalate to management — RPO violation

4. **Record Selected Backup**
   ```
   Backup File: ________________________________
   Backup Date: ________________________________
   File Size:   ________________________________
   ```

### Step 3: Restore Encryption Certificate (15 min)

> ⚠️ **CRITICAL**: Certificate must be restored BEFORE database restore

1. **Check if Certificate Exists on Target Server**
   ```sql
   SELECT name, expiry_date 
   FROM master.sys.certificates 
   WHERE name = 'UnifiedPatientAccess_BackupCert';
   ```

2. **If Certificate Not Present, Restore It**
   ```sql
   USE master;
   GO
   
   -- Create master key if not exists
   IF NOT EXISTS (SELECT 1 FROM sys.symmetric_keys 
                  WHERE name = '##MS_DatabaseMasterKey##')
   BEGIN
       CREATE MASTER KEY ENCRYPTION BY PASSWORD = '<MasterKeyPassword>';
   END
   GO
   
   -- Restore certificate from backup
   CREATE CERTIFICATE UnifiedPatientAccess_BackupCert
       FROM FILE = '\\secure-share\certificates\UnifiedPatientAccess_BackupCert.cer'
       WITH PRIVATE KEY (
           FILE = '\\secure-share\certificates\UnifiedPatientAccess_BackupCert.pvk',
           DECRYPTION BY PASSWORD = '<CertPrivateKeyPassword>'
       );
   GO
   ```

3. **Verify Certificate Restored**
   ```sql
   SELECT name, expiry_date, pvt_key_encryption_type_desc
   FROM master.sys.certificates 
   WHERE name = 'UnifiedPatientAccess_BackupCert';
   ```

### Step 4: Restore Database (30-120 min)

1. **Verify Backup Integrity**
   ```sql
   RESTORE VERIFYONLY 
   FROM DISK = 'D:\Backups\UnifiedPatientAccess\UnifiedPatientAccess_20260427_020000.bak'
   WITH CHECKSUM;
   ```
   
   Expected: `The backup set on file 1 is valid.`

2. **Get Logical File Names**
   ```sql
   RESTORE FILELISTONLY 
   FROM DISK = 'D:\Backups\UnifiedPatientAccess\UnifiedPatientAccess_20260427_020000.bak';
   ```

3. **Restore Database**
   
   **Option A: Restore Over Existing (Destructive)**
   ```sql
   -- Take database offline
   ALTER DATABASE [UnifiedPatientAccess] SET OFFLINE WITH ROLLBACK IMMEDIATE;
   GO
   
   RESTORE DATABASE [UnifiedPatientAccess]
   FROM DISK = 'D:\Backups\UnifiedPatientAccess\UnifiedPatientAccess_20260427_020000.bak'
   WITH
       REPLACE,
       STATS = 10;
   GO
   
   ALTER DATABASE [UnifiedPatientAccess] SET ONLINE;
   GO
   ```
   
   **Option B: Restore to New Instance**
   ```sql
   RESTORE DATABASE [UnifiedPatientAccess]
   FROM DISK = 'D:\Backups\UnifiedPatientAccess\UnifiedPatientAccess_20260427_020000.bak'
   WITH
       MOVE 'UnifiedPatientAccess' TO 'D:\SQLData\UnifiedPatientAccess.mdf',
       MOVE 'UnifiedPatientAccess_log' TO 'D:\SQLLogs\UnifiedPatientAccess_log.ldf',
       STATS = 10;
   GO
   ```

4. **Record Restore Time**
   ```
   Restore Start:    ________________________________
   Restore Complete: ________________________________
   Duration:         ________________________________
   ```

### Step 5: Verify Data Integrity (30 min)

1. **Check Database State**
   ```sql
   SELECT name, state_desc, recovery_model_desc
   FROM sys.databases 
   WHERE name = 'UnifiedPatientAccess';
   ```
   
   Expected: `state_desc = 'ONLINE'`

2. **Verify All Schemas Exist**
   ```sql
   USE UnifiedPatientAccess;
   GO
   
   SELECT SCHEMA_NAME 
   FROM INFORMATION_SCHEMA.SCHEMATA 
   WHERE SCHEMA_NAME IN ('identity', 'scheduling', 'clinical', 'notification')
   ORDER BY SCHEMA_NAME;
   ```
   
   Expected: All 4 schemas present

3. **Verify Core Tables**
   ```sql
   SELECT TABLE_SCHEMA, TABLE_NAME
   FROM INFORMATION_SCHEMA.TABLES
   WHERE TABLE_TYPE = 'BASE TABLE'
     AND TABLE_SCHEMA IN ('identity', 'scheduling', 'clinical', 'notification')
   ORDER BY TABLE_SCHEMA, TABLE_NAME;
   ```
   
   Expected: Minimum 15 tables

4. **Verify Data Counts**
   ```sql
   SELECT 
       (SELECT COUNT(*) FROM identity.Users) AS Users,
       (SELECT COUNT(*) FROM scheduling.Appointments) AS Appointments,
       (SELECT COUNT(*) FROM scheduling.Providers) AS Providers;
   ```

5. **Run DBCC Checks**
   ```sql
   DBCC CHECKDB ('UnifiedPatientAccess') WITH NO_INFOMSGS;
   ```
   
   Expected: `CHECKDB found 0 allocation errors and 0 consistency errors.`

### Step 6: Update Application Configuration (15 min)

1. **Update Connection Strings** (if server changed)
   - `appsettings.json`: `ConnectionStrings:IdentityDb`, `SchedulingDb`, `ClinicalDb`

2. **Restart Application**
   ```powershell
   # If running as Windows Service
   Restart-Service -Name "UnifiedPatientAccess"
   
   # If running in IIS
   iisreset /restart
   
   # If running in Docker
   docker-compose restart backend
   ```

### Step 7: Validate Health Checks (15 min)

1. **Check Application Health**
   ```powershell
   $response = Invoke-RestMethod -Uri "http://localhost:5037/health/ready"
   $response | ConvertTo-Json
   ```
   
   Expected: All checks `Healthy`

2. **Verify Backup Health Check**
   ```powershell
   $response = Invoke-RestMethod -Uri "http://localhost:5037/health/ready"
   $response.Checks | Where-Object { $_.Name -eq "backup_status" }
   ```

3. **Test Core Operations**
   - [ ] Login with admin credentials
   - [ ] View appointment queue
   - [ ] Access patient records
   - [ ] Check audit logs

---

## Post-Recovery Validation

### Checklist

- [ ] Database online and accessible
- [ ] All schemas restored (identity, scheduling, clinical, notification)
- [ ] DBCC CHECKDB passed with no errors
- [ ] Application health checks passing
- [ ] Admin login successful
- [ ] Core scheduling operations functional
- [ ] Audit logging operational
- [ ] Backup job rescheduled and tested

### Documentation

1. Complete incident report
2. Update this runbook with lessons learned
3. Schedule post-incident review (within 48 hours)

---

## Escalation Contacts

| Role | Contact | Phone |
|------|---------|-------|
| IT Operations Lead | [TBD] | [TBD] |
| DBA On-Call | [TBD] | [TBD] |
| Clinical Systems Manager | [TBD] | [TBD] |
| Compliance Officer | [TBD] | [TBD] |
| Vendor Support | Microsoft SQL Server | 1-800-936-5800 |

---

## Appendix

### A. Common Errors and Solutions

| Error | Cause | Solution |
|-------|-------|----------|
| "Cannot open backup device" | File permissions | Grant SQL Server service account read access |
| "Certificate not found" | Certificate not restored | Execute Step 3 first |
| "Exclusive access could not be obtained" | Database in use | Use `WITH ROLLBACK IMMEDIATE` |
| "Insufficient disk space" | Low storage | Free space or use different drive |

### B. Backup Schedule

| Type | Frequency | Retention |
|------|-----------|-----------|
| Full | Daily 02:00 | 30 days |
| Differential | Every 4 hours | 7 days |
| Transaction Log | Every 30 minutes | 3 days |

### C. Related Scripts

- `scripts/backup/create-backup-certificate.sql`
- `scripts/backup/daily-backup.sql`
- `scripts/backup/restore-verify.sql`

---

**Document Control**

| Version | Date | Author | Changes |
|---------|------|--------|---------|
| 1.0 | April 2026 | Platform Team | Initial version |

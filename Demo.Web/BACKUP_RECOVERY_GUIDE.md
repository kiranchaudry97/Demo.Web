# ?? Backup & Recovery Strategie

## ?? Overzicht

Complete backup strategie voor data persistentie, disaster recovery en GDPR compliance.

---

## ??? Data Storage Layers

### **Layer 1: Primary Database (SQLite)**
- **Locatie:** `Demo.Web/bookstore.db`
- **Type:** SQLite database file
- **Inhoud:** Orders, Klanten, Boeken, OrderRegels

### **Layer 2: RabbitMQ Messages (Tijdelijk)**
- **Locatie:** RabbitMQ server (in-memory + disk persistence)
- **Type:** Message queue
- **Retentie:** Tot message wordt geconsumeerd (ACK)
- **Durable:** Yes (persistent messages)

### **Layer 3: Application Logs**
- **Locatie:** Console output (kan naar file)
- **Type:** Structured logging
- **Inhoud:** Audit trail van alle acties

---

## ?? Backup Strategie

### **1. Database Backup**

#### **Automatische Backup (Aanbevolen)**

```bash
# Daily backup script (Windows)
# Maak bestand: backup-database.ps1

$timestamp = Get-Date -Format "yyyyMMdd_HHmmss"
$source = "C:\Users\admin\source\repos\Demo.Web\Demo.Web\bookstore.db"
$backupDir = "C:\Backups\Bookstore"
$destination = "$backupDir\bookstore_$timestamp.db"

# Create backup directory if not exists
if (!(Test-Path $backupDir)) {
    New-Item -ItemType Directory -Path $backupDir
}

# Copy database
Copy-Item $source $destination

# Keep only last 30 backups
Get-ChildItem $backupDir -Filter "bookstore_*.db" | 
    Sort-Object LastWriteTime -Descending | 
    Select-Object -Skip 30 | 
    Remove-Item

Write-Host "Backup created: $destination"
```

**Scheduled Task Setup:**
```powershell
# Run daily at 02:00
$action = New-ScheduledTaskAction -Execute "PowerShell.exe" -Argument "-File C:\Scripts\backup-database.ps1"
$trigger = New-ScheduledTaskTrigger -Daily -At 02:00AM
Register-ScheduledTask -Action $action -Trigger $trigger -TaskName "BookstoreBackup" -Description "Daily database backup"
```

---

#### **Manual Backup**

```bash
# Copy database file
cp Demo.Web/bookstore.db backups/bookstore_$(date +%Y%m%d_%H%M%S).db
```

---

### **2. RabbitMQ Message Backup**

RabbitMQ messages zijn **durable** en **persistent**:

```csharp
// In RabbitMqService.cs
properties.Persistent = true;  // Messages survive broker restart
_channel.QueueDeclare(durable: true);  // Queue survives restart
```

**RabbitMQ Backup Opties:**

```bash
# Export RabbitMQ definitions (queues, exchanges, bindings)
rabbitmqadmin export rabbitmq-backup.json

# Backup RabbitMQ data directory (full backup)
# Windows: C:\Users\<user>\AppData\Roaming\RabbitMQ
# Linux: /var/lib/rabbitmq

tar -czf rabbitmq-backup.tar.gz /var/lib/rabbitmq/
```

---

### **3. Application Code Backup**

**Git Repository (Aanbevolen):**

```bash
# Initialize git (if not already)
git init
git add .
git commit -m "Bookstore API - Complete implementation"

# Push to remote repository
git remote add origin https://github.com/your-username/bookstore-api.git
git push -u origin main
```

**Versie Controle:**
- ? Source code in Git
- ? Tag releases: `git tag v1.0.0`
- ? Remote backup (GitHub/GitLab/Azure DevOps)

---

## ?? Rollback Strategie

### **Scenario 1: Database Corrupted**

```bash
# Stop application
dotnet stop

# Restore from backup
cp backups/bookstore_20240115_020000.db Demo.Web/bookstore.db

# Restart application
dotnet run
```

**Rollback Point:** Latest backup (bijvoorbeeld: dagelijks 02:00 AM)

---

### **Scenario 2: RabbitMQ Message Loss**

**Problem:** Messages lost bij crash

**Solution:**
1. RabbitMQ persistent messages (already implemented ?)
2. Messages worden niet verwijderd tot ACK
3. Bij crash: Messages blijven in queue

```csharp
// In Consumer
_channel.BasicAck(ea.DeliveryTag, false);  // Only ACK after successful processing
```

**Rollback Point:** Messages blijven in queue tot succesvol verwerkt

---

### **Scenario 3: Application Crash During Order**

**Transaction Flow:**

```csharp
// In OrdersController
using var transaction = await _context.Database.BeginTransactionAsync();
try
{
    // Create order
    _context.Orders.Add(order);
    await _context.SaveChangesAsync();
    
    // Publish to RabbitMQ
    await _rabbitMqService.PublishOrderAsync(orderMessage);
    
    await transaction.CommitAsync();
}
catch
{
    await transaction.RollbackAsync();  // ? Rollback database changes
    throw;
}
```

**Current Implementation:** ?? **No explicit transactions**

**Recommended:** Add database transactions for critical operations

---

### **Scenario 4: Complete System Failure**

**Recovery Steps:**

1. **Restore Database**
   ```bash
   cp backups/bookstore_latest.db Demo.Web/bookstore.db
   ```

2. **Restore RabbitMQ**
   ```bash
   rabbitmqctl import_definitions rabbitmq-backup.json
   ```

3. **Redeploy Application**
   ```bash
   git pull origin main
   dotnet restore
   dotnet run
   ```

4. **Verify Data Integrity**
   - Check last order ID
   - Verify RabbitMQ queues
   - Test API endpoints

**Rollback Point:** Laatste volledige backup (max 24 uur data verlies)

---

## ?? Backup Schedule

| Component | Frequency | Retention | Location |
|-----------|-----------|-----------|----------|
| **SQLite Database** | Daily (02:00) | 30 days | Local disk |
| **RabbitMQ Config** | Weekly | 4 weeks | Local disk |
| **Application Logs** | Continuous | 7 days | Console/File |
| **Git Repository** | On commit | Permanent | Remote (GitHub) |

---

## ?? GDPR Compliance

### **Data Backup & Privacy**

1. **Encrypted Backups**
   ```powershell
   # Encrypt backup with password
   7z a -p<password> -mhe=on bookstore_backup.7z bookstore.db
   ```

2. **Secure Storage**
   - Backups on encrypted drive
   - Access control (admin only)
   - Off-site backup (cloud storage)

3. **Data Retention Policy**
   ```
   - Active database: Indefinite (operational data)
   - Backups: 30 days
   - Logs: 7 days
   - Deleted customer data: Immediate removal from backups
   ```

4. **Right to Be Forgotten**
   ```csharp
   // When customer requests deletion:
   1. Delete from database (immediate)
   2. Publish delete event to RabbitMQ
   3. Purge from backups (scheduled cleanup)
   4. Notify external systems (Salesforce, SAP)
   ```

---

## ?? Backup Testing

### **Monthly Backup Test Procedure:**

```bash
# 1. Create test environment
mkdir test-restore
cd test-restore

# 2. Copy latest backup
cp ../backups/bookstore_latest.db bookstore.db

# 3. Start application with test database
dotnet run --urls "http://localhost:5270"

# 4. Verify data
curl http://localhost:5270/api/klanten
curl http://localhost:5270/api/boeken
curl http://localhost:5270/api/orders

# 5. Check last order ID
# Compare with production

# 6. Cleanup
cd ..
rm -rf test-restore
```

---

## ?? Monitoring & Alerts

### **Backup Monitoring**

```powershell
# Check last backup age
$lastBackup = Get-ChildItem C:\Backups\Bookstore -Filter "bookstore_*.db" | 
    Sort-Object LastWriteTime -Descending | 
    Select-Object -First 1

$age = (Get-Date) - $lastBackup.LastWriteTime

if ($age.TotalHours -gt 25) {
    Write-Warning "Last backup is older than 24 hours!"
    # Send alert email
}
```

---

## ?? Recovery Time Objectives (RTO)

| Scenario | RTO | RPO | Data Loss |
|----------|-----|-----|-----------|
| **Database corruption** | 15 min | 24 hours | Last backup |
| **RabbitMQ crash** | 5 min | 0 | None (persistent) |
| **Application crash** | 2 min | 0 | None (auto-restart) |
| **Complete system failure** | 1 hour | 24 hours | Last backup |

**RTO:** Recovery Time Objective (tijd om te herstellen)  
**RPO:** Recovery Point Objective (max data verlies)

---

## ? Backup Checklist

- ? Daily automatic database backups
- ? RabbitMQ durable queues
- ? Persistent messages
- ? Git version control
- ?? Transaction management (needs improvement)
- ?? Encrypted backups (optional, recommended)
- ?? Off-site backup (optional, recommended)
- ? Automated backup testing (to implement)
- ? Backup monitoring/alerts (to implement)

---

## ?? Implementation Code

### **Add Database Transactions**

```csharp
// Update OrdersController.cs
[HttpPost]
public async Task<ActionResult<OrderResponseDto>> CreateOrder(CreateOrderDto orderDto)
{
    using var transaction = await _context.Database.BeginTransactionAsync();
    
    try
    {
        // Create order
        var order = new Order { /* ... */ };
        _context.Orders.Add(order);
        await _context.SaveChangesAsync();
        
        // Update stock
        foreach (var item in orderDto.Items)
        {
            var boek = await _context.Boeken.FindAsync(item.BoekId);
            boek.VoorraadAantal -= item.Aantal;
        }
        await _context.SaveChangesAsync();
        
        // Publish to RabbitMQ
        await _rabbitMqService.PublishOrderAsync(orderMessage);
        
        // Commit transaction
        await transaction.CommitAsync();
        
        return Ok(response);
    }
    catch (Exception ex)
    {
        await transaction.RollbackAsync();
        _logger.LogError($"Order creation failed, rolled back: {ex.Message}");
        return StatusCode(500, new { error = "Order creation failed" });
    }
}
```

---

## ?? Summary

| Aspect | Status | Notes |
|--------|--------|-------|
| **Database Backup** | ? Implemented | Daily backups, 30 day retention |
| **RabbitMQ Persistence** | ? Implemented | Durable queues, persistent messages |
| **Rollback Capability** | ?? Partial | Database: 24h, RabbitMQ: 0, Transactions: Missing |
| **GDPR Compliance** | ?? Partial | Data retention policy defined, encryption optional |

**Recommended Next Steps:**
1. Implement database transactions
2. Set up automated backups (scheduled task)
3. Add backup encryption
4. Implement backup monitoring
5. Test recovery procedures monthly

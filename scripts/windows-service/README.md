# StockLab Worker Windows Service

Run PowerShell as administrator from the repository root.

Create a local settings file first:

```powershell
Copy-Item src\StockLab.Worker\appsettings.Local.example.json src\StockLab.Worker\appsettings.Local.json
notepad src\StockLab.Worker\appsettings.Local.json
```

Fill in the database password and Discord webhook in `appsettings.Local.json`.

Install or update the service:

```powershell
.\scripts\windows-service\Install-StockLabWorker.ps1
```

Install without starting:

```powershell
.\scripts\windows-service\Install-StockLabWorker.ps1 -NoStart
```

Use a custom publish path:

```powershell
.\scripts\windows-service\Install-StockLabWorker.ps1 -PublishPath "D:\Services\StockLab.Worker"
```

Uninstall the service:

```powershell
.\scripts\windows-service\Uninstall-StockLabWorker.ps1
```

The service name is `StockLabWorker`. The display name is `StockLab Worker`.

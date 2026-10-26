# RxIntel Backend CI/CD Setup for IIS

This directory contains automated CI/CD deployment scripts for Windows Server (IIS).

---

## 🔒 Configuration Protection (Zero Overwrite)

The deployment script strictly excludes:
- `appsettings.json`
- `appsettings.Development.json`
- `appsettings.Production.json` (and all `appsettings.*.json`)
- `web.config`

The live IIS server's production settings, connection strings, secrets, and rewrite rules will **NEVER** be overwritten or altered during automated releases.

---

## 📁 Files Included

1. **`deploy.ps1`**: The core PowerShell automation script.
   - Runs `dotnet publish -c Release`.
   - Places `app_offline.htm` in IIS directory to release DLL file locks.
   - Uses `robocopy /XF appsettings.json appsettings.*.json web.config` to sync new binaries without overwriting configs.
   - Removes `app_offline.htm` and recycles IIS Application Pool.
2. **`.github/workflows/deploy.yml`**: GitHub Actions workflow that triggers `deploy.ps1 -SkipGitPull` on a self-hosted runner.

---

## 🧪 Testing the Deployment Manually

You can test the deployment locally at any time by running PowerShell as Administrator:
```powershell
powershell -ExecutionPolicy Bypass -File "deploy\deploy.ps1" -IisSitePath "C:\inetpub\wwwroot\RxIntel_API"
```

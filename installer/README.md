# Windows installer (WiX)

`Package.wxs` builds a per-machine **MSI** that installs the self-contained publish output under Program Files, with Start Menu and Desktop shortcuts.

## Local pack

From the repo root:

```powershell
.\scripts\pack-release.ps1 -Version 1.0.1
```

Artifacts land in `artifacts/` (gitignored).

## CI

GitHub Actions workflow `.github/workflows/release.yml` builds zip + MSI on tags `v*`.

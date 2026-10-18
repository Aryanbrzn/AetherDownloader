# yt-dlp (not committed)

This folder is intentionally empty in Git. Do **not** commit `yt-dlp.exe` or checksum sidecars.

## Option A — In-app bootstrapper (recommended for users)

Run AetherDownloader. If `yt-dlp` is missing, the app offers to download the official latest Windows build from the [yt-dlp GitHub Releases](https://github.com/yt-dlp/yt-dlp/releases) into:

`%LOCALAPPDATA%\AetherDownloader\tools\yt-dlp\yt-dlp.exe`

## Option B — Developer local copy (optional)

For local builds and integration tests, place a Windows build here:

```powershell
New-Item -ItemType Directory -Force -Path tools/yt-dlp | Out-Null
Invoke-WebRequest -Uri "https://github.com/yt-dlp/yt-dlp/releases/latest/download/yt-dlp.exe" `
  -OutFile tools/yt-dlp/yt-dlp.exe
```

The App / Tests projects will copy `tools/yt-dlp/**` to the output directory when present. `.gitignore` excludes `*.exe` so these files stay local.

## Option C — System PATH

Install `yt-dlp` via winget, scoop, or pip and ensure `yt-dlp.exe` is on `PATH`.

## License

yt-dlp is Unlicense. See https://github.com/yt-dlp/yt-dlp

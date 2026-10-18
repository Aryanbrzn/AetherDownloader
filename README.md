# AetherDownloader

Clean Windows GUI for [yt-dlp](https://github.com/yt-dlp/yt-dlp) — a .NET WPF front-end for inspecting media URLs, choosing formats, and downloading **public-domain or freely licensed** content with optional FFmpeg muxing.

## Screenshots / Demo

_Placeholder — add UI screenshots here before publishing a release._

## Features

- Format / resolution selector from yt-dlp discovery
- Audio-only format selection and extraction paths
- FFmpeg-assisted merge / conversion when FFmpeg is available
- Cookie profile import for sites that require authenticated access you are allowed to use
- Clipboard URL monitoring
- Optional tool path overrides and a runtime bootstrapper for missing CLI tools
- Proxy support via standard environment variables (`HTTP_PROXY` / `HTTPS_PROXY`) consumed by yt-dlp

## Prerequisites

- Windows 10/11
- [.NET 10 SDK](https://dotnet.microsoft.com/download) (for building from source)
- [.NET 10 Desktop Runtime](https://dotnet.microsoft.com/download) (for running published builds)
- [Visual C++ Redistributable](https://learn.microsoft.com/en-us/cpp/windows/latest-supported-vc-redist) (often already installed)
- **yt-dlp** and **FFmpeg** — not committed to this repository. The app can download official builds into `%LOCALAPPDATA%\AetherDownloader\tools`, or you can install them yourself and point Settings at the executables / put them on `PATH`.

## Quick Start (users)

1. Open the project’s [GitHub Releases](https://github.com/Aryanbrzn/AetherDownloader/releases) page and download the latest pre-built zip or installer.
2. Extract / install, then run `AetherDownloader.App.exe`.
3. If prompted, allow the app to download official `yt-dlp` / FFmpeg builds, or set paths under **Settings**.
4. Paste a **public-domain or Creative Commons** URL (for example Blender’s Big Buck Bunny sample) and use **Inspect**.

## Developer Build

```powershell
git clone https://github.com/Aryanbrzn/AetherDownloader.git
cd AetherDownloader
dotnet restore
dotnet build AetherDownloader.sln -c Release
dotnet run --project src/AetherDownloader.App/AetherDownloader.App.csproj -c Release
```

Optional: place a local `yt-dlp.exe` under `tools/yt-dlp/` for integration tests (gitignored). See `tools/yt-dlp/README.md`.

```powershell
dotnet test AetherDownloader.sln
```

Publish example:

```powershell
dotnet publish src/AetherDownloader.App/AetherDownloader.App.csproj -c Release -o ./publish
```

Do not commit the `publish/` folder or any `*.exe` / `*.dll` outputs.

## Attribution

- [yt-dlp](https://github.com/yt-dlp/yt-dlp) — Unlicense
- [FFmpeg](https://ffmpeg.org/) — LGPL/GPL depending on the build you install

AetherDownloader is an independent GUI and is not affiliated with those projects.

## Legal Disclaimer

This software is an independent graphical user interface for yt-dlp. It is intended solely for personal archiving, educational use, and downloading public-domain or freely licensed (Creative Commons) content. The maintainers do not endorse copyright infringement or terms of service violations.

The same disclaimer is shown in the app under **About**.

## License

This project is licensed under the **MIT License** — see [LICENSE](LICENSE).

Third-party tools (`yt-dlp`, `FFmpeg`) retain their own licenses and are **not** redistributed in this source repository.

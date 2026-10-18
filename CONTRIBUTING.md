# Contributing to AetherDownloader

Thanks for helping improve this project. By contributing, you agree that your contributions are licensed under the MIT License.

## Before you start

- Use the app only with public-domain, Creative Commons, or otherwise freely licensed content.
- Do not submit code, comments, docs, or sample URLs that encourage copyright infringement or Terms of Service violations.
- Do not commit binaries (`*.exe`, `*.dll`), secrets, cookies, or personal paths.

## Reporting issues

1. Search existing issues first.
2. Include OS version, .NET version, app version/commit, and clear reproduction steps.
3. Prefer public-domain sample URLs (for example Blender Big Buck Bunny streams) in repro steps.
4. Redact cookies, tokens, and personal file paths from logs.

Security vulnerabilities: see [SECURITY.md](SECURITY.md) — do **not** open a public issue for security reports.

## Pull request workflow

1. Fork the repository and create a topic branch from `main` (or the default branch).
2. Keep changes focused; prefer small PRs.
3. Run locally:

   ```powershell
   dotnet restore
   dotnet build AetherDownloader.sln
   dotnet test AetherDownloader.sln
   ```

4. Update docs when behavior or prerequisites change.
5. Open a PR with a short summary of **why** the change exists and how you tested it.

## Code formatting rules

- Match existing style in the file you edit (C# / XAML).
- Match existing C# style: prefer explicit types, `async`/`await`, and no new `.Result` / `.Wait()` on UI paths.
- Avoid `dynamic` and unjustified `object` casts.
- Wrap API / IO / process operations with appropriate error handling.
- Do not invent permissions, magic strings, or download endpoints outside the official yt-dlp / FFmpeg sources already used by `ToolBootstrapper`.

## Development notes

- Third-party CLI tools live outside Git. Use Settings → **Download missing tools**, PATH, or an optional local `tools/yt-dlp/yt-dlp.exe` (gitignored).
- Settings and cookies belong under `%APPDATA%\AetherDownloader\` (and managed tools under `%LOCALAPPDATA%\AetherDownloader\tools\`).

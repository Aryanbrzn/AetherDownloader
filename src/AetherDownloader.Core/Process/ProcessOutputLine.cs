namespace AetherDownloader.Core.Process;

public sealed record ProcessOutputLine(bool IsError, string Text, DateTimeOffset Timestamp);

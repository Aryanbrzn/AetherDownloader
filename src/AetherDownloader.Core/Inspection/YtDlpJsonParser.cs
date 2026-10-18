using System.Text.Json;
using System.Text.Json.Serialization;
using AetherDownloader.Core.Models;

namespace AetherDownloader.Core.Inspection;

public static class YtDlpJsonParser
{
    private const int MaxDescriptionLength = 500;

    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        NumberHandling = JsonNumberHandling.AllowReadingFromString,
    };

    public static MediaMetadata ParseMetadata(string json, string sourceUrl)
    {
        try
        {
            var dto = JsonSerializer.Deserialize<YtDlpMetadataCoreDto>(json, SerializerOptions)
                ?? throw new InvalidOperationException("Metadata JSON was empty.");

            var isPlaylist = string.Equals(dto.Type, "playlist", StringComparison.OrdinalIgnoreCase)
                || (dto.Entries?.Count ?? 0) > 0;

            return new MediaMetadata
            {
                Id = dto.Id ?? string.Empty,
                Title = dto.Title ?? string.Empty,
                Uploader = dto.Uploader ?? dto.Channel ?? dto.ChannelId ?? string.Empty,
                DurationSeconds = dto.Duration,
                ThumbnailUrl = dto.Thumbnail ?? string.Empty,
                Description = TruncateDescription(dto.Description),
                Extractor = dto.Extractor ?? string.Empty,
                IsLive = dto.IsLive ?? false,
                IsPlaylist = isPlaylist,
                SourceUrl = sourceUrl,
            };
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException("Failed to parse yt-dlp metadata JSON.", ex);
        }
    }

    public static IReadOnlyList<FormatOption> ParseFormats(string json)
    {
        try
        {
            var dto = JsonSerializer.Deserialize<YtDlpMetadataDto>(json, SerializerOptions);
            if (dto?.Formats is null || dto.Formats.Count == 0)
            {
                return [];
            }

            return dto.Formats
                .Where(f => !string.IsNullOrWhiteSpace(f.FormatId))
                .Select(MapFormat)
                .ToList();
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException("Failed to parse yt-dlp format list JSON.", ex);
        }
    }

    private static FormatOption MapFormat(YtDlpFormatDto format) =>
        new()
        {
            FormatId = format.FormatId!,
            Extension = format.Ext ?? "unknown",
            Resolution = format.Resolution ?? (format.Height is int h ? $"{format.Width ?? 0}x{h}" : null),
            Fps = format.Fps is double fps ? (int)Math.Round(fps) : null,
            VideoCodec = format.Vcodec,
            AudioCodec = format.Acodec,
            FileSizeApproxBytes = format.Filesize ?? format.FilesizeApprox,
            Note = format.FormatNote,
        };

    private static string? TruncateDescription(string? description)
    {
        if (string.IsNullOrEmpty(description))
        {
            return description;
        }

        return description.Length <= MaxDescriptionLength
            ? description
            : description[..MaxDescriptionLength];
    }

    private sealed class YtDlpMetadataCoreDto
    {
        [JsonPropertyName("_type")]
        public string? Type { get; init; }

        public string? Id { get; init; }
        public string? Title { get; init; }
        public string? Uploader { get; init; }
        public string? Channel { get; init; }
        public string? ChannelId { get; init; }
        public int? Duration { get; init; }
        public string? Thumbnail { get; init; }
        public string? Description { get; init; }
        public string? Extractor { get; init; }

        [JsonPropertyName("is_live")]
        public bool? IsLive { get; init; }

        public List<YtDlpEntryDto>? Entries { get; init; }
    }

    private sealed class YtDlpMetadataDto
    {
        [JsonPropertyName("_type")]
        public string? Type { get; init; }

        public string? Id { get; init; }
        public string? Title { get; init; }
        public string? Uploader { get; init; }
        public string? Channel { get; init; }
        public string? ChannelId { get; init; }
        public int? Duration { get; init; }
        public string? Thumbnail { get; init; }
        public string? Description { get; init; }
        public string? Extractor { get; init; }

        [JsonPropertyName("is_live")]
        public bool? IsLive { get; init; }

        public List<YtDlpEntryDto>? Entries { get; init; }
        public List<YtDlpFormatDto>? Formats { get; init; }
    }

    private sealed class YtDlpEntryDto
    {
        public string? Id { get; init; }
        public string? Title { get; init; }
    }

    private sealed class YtDlpFormatDto
    {
        [JsonPropertyName("format_id")]
        public string? FormatId { get; init; }

        public string? Ext { get; init; }
        public string? Resolution { get; init; }
        public int? Width { get; init; }
        public int? Height { get; init; }
        public double? Fps { get; init; }
        public string? Vcodec { get; init; }
        public string? Acodec { get; init; }
        public long? Filesize { get; init; }

        [JsonPropertyName("filesize_approx")]
        public long? FilesizeApprox { get; init; }

        [JsonPropertyName("format_note")]
        public string? FormatNote { get; init; }
    }
}

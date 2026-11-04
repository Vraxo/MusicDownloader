using MusicMan.Core;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace MusicMan.Infrastructure;

/// <summary>
/// Reads <see cref="CoverFitMode"/> leniently: a typo in settings.json falls back to
/// <see cref="CoverFitMode.Original"/> with a warning instead of discarding the whole settings file.
/// Values are matched without case, separators, or underscores, so <c>square_crop</c> works too.
/// </summary>
internal sealed class CoverFitModeJsonConverter : JsonConverter<CoverFitMode>
{
    public override CoverFitMode Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.String)
        {
            using JsonDocument document = JsonDocument.ParseValue(ref reader);
            return Fallback(document.RootElement.ToString());
        }

        string? raw = reader.GetString();
        string normalized = (raw ?? string.Empty).Replace("-", string.Empty).Replace("_", string.Empty).Trim();

        return Enum.TryParse(normalized, ignoreCase: true, out CoverFitMode mode) && Enum.IsDefined(mode)
            ? mode
            : Fallback(raw ?? "<null>");
    }

    public override void Write(Utf8JsonWriter writer, CoverFitMode value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.ToString());
    }

    private static CoverFitMode Fallback(string value)
    {
        Log.Warning($"Unknown 'CoverFit' value '{value}'. Using '{CoverFitMode.Original}'.");
        return CoverFitMode.Original;
    }
}

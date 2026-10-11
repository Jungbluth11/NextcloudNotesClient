namespace Jungbluth11.NextcloudNotesClient;

public sealed class NextcloudNotesSettings
{
    [JsonPropertyName("fileSuffix")] public string? FileSuffix { get; set; }

    [JsonPropertyName("notesPath")] public string? NotesPath { get; set; }
}
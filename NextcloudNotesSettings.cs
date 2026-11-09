namespace Jungbluth11.NextcloudNotesClient;

/// <summary>Contains the Notes settings for the current Nextcloud user.</summary>
public sealed class NextcloudNotesSettings
{
    /// <summary>File extension used for saved notes.</summary>
    [JsonPropertyName("fileSuffix")] public string? FileSuffix { get; set; }

    /// <summary>Server path where notes are stored.</summary>
    [JsonPropertyName("notesPath")] public string? NotesPath { get; set; }
}

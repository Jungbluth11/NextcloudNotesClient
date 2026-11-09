namespace Jungbluth11.NextcloudNotesClient;

/// <summary>Represents a note from the Nextcloud Notes API.</summary>
public sealed class NextcloudNote
{
    /// <summary>Category of the note.</summary>
    [JsonPropertyName("category")] public string Category { get; set; } = string.Empty;

    /// <summary>Text content of the note.</summary>
    [JsonPropertyName("content")] public string Content { get; set; } = string.Empty;

    /// <summary>ETag of the server version, used to detect concurrent changes.</summary>
    [JsonPropertyName("etag")] public string? ETag { get; set; }

    /// <summary>Indicates whether the note is marked as a favorite.</summary>
    [JsonPropertyName("favorite")] public bool Favorite { get; set; }

    /// <summary>Unique note ID on the Nextcloud server.</summary>
    [JsonPropertyName("id")] public int Id { get; set; }

    /// <summary>Modification time as Unix time in seconds.</summary>
    [JsonPropertyName("modified")] public long Modified { get; set; }

    /// <summary>Modification time as a <see cref="DateTimeOffset"/> in UTC.</summary>
    [JsonIgnore]
    public DateTimeOffset ModifiedAt => DateTimeOffset.FromUnixTimeSeconds(Modified);

    /// <summary>Indicates whether the note is read-only.</summary>
    [JsonPropertyName("readonly")] public bool ReadOnly { get; set; }

    /// <summary>Title of the note.</summary>
    [JsonPropertyName("title")] public string Title { get; set; } = string.Empty;
}

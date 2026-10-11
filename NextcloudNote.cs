namespace Jungbluth11.NextcloudNotesClient;

public sealed class NextcloudNote
{
    [JsonPropertyName("category")] public string Category { get; set; } = string.Empty;

    [JsonPropertyName("content")] public string Content { get; set; } = string.Empty;

    [JsonPropertyName("etag")] public string? ETag { get; set; }

    [JsonPropertyName("favorite")] public bool Favorite { get; set; }

    [JsonPropertyName("id")] public int Id { get; set; }

    [JsonPropertyName("modified")] public long Modified { get; set; }

    [JsonIgnore]
    public DateTimeOffset ModifiedAt => DateTimeOffset.FromUnixTimeSeconds(Modified);

    [JsonPropertyName("readonly")] public bool ReadOnly { get; set; }

    [JsonPropertyName("title")] public string Title { get; set; } = string.Empty;
}
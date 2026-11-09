namespace Jungbluth11.NextcloudNotesClient;

/// <summary>Optional behavior settings for <see cref="NextcloudNotesClient"/>.</summary>
public sealed class NextcloudNotesClientOptions
{
    /// <summary>Enables automatic handling of HTTP 412 conflicts; disabled by default.</summary>
    public bool ResolvePreconditionFailed { get; set; }

    /// <summary>Strategy used when automatic conflict handling is enabled.</summary>
    public ConflictResolutionStrategy ConflictResolutionStrategy { get; set; } = ConflictResolutionStrategy.Ask;

    /// <summary>Asynchronous resolver for <see cref="ConflictResolutionStrategy.Ask"/>; receives the local and current server versions.</summary>
    public Func<NextcloudNote, NextcloudNote, CancellationToken, ValueTask<ConflictResolutionStrategy>>? ConflictResolver { get; set; }
}

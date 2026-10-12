namespace Jungbluth11.NextcloudNotesClient;

/// <summary>Optional behavior settings for <see cref="NextcloudNotesClient"/>.</summary>
public sealed class NextcloudNotesClientOptions
{
    /// <summary>Enables automatic handling of HTTP 412 note update conflicts. Disabled by default.</summary>
    public bool ResolvePreconditionFailed { get; set; }

    /// <summary>Resolution used when <see cref="ResolvePreconditionFailed"/> is enabled.</summary>
    public ConflictResolutionStrategy ConflictResolutionStrategy { get; set; } = ConflictResolutionStrategy.Ask;

    /// <summary>Called for Ask, with local and current server versions; return ServerFirst or ClientFirst.</summary>
    public Func<NextcloudNote, NextcloudNote, CancellationToken, ValueTask<ConflictResolutionStrategy>>? ConflictResolver { get; set; }
}

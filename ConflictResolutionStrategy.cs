namespace Jungbluth11.NextcloudNotesClient;

/// <summary>Specifies how an HTTP 412 conflict during an update is handled.</summary>
public enum ConflictResolutionStrategy
{
    /// <summary>Keeps the current server version.</summary>
    ServerFirst,

    /// <summary>Overwrites the current server version with the local version.</summary>
    ClientFirst,

    /// <summary>Delegates the choice to the application's configured resolver.</summary>
    Ask
}

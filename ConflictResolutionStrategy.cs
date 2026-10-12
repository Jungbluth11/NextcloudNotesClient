namespace Jungbluth11.NextcloudNotesClient;

/// <summary>How to handle an HTTP 412 conflict while updating a note.</summary>
public enum ConflictResolutionStrategy
{
    /// <summary>Keep the current server version.</summary>
    ServerFirst,

    /// <summary>Overwrite the current server version with the local version.</summary>
    ClientFirst,

    /// <summary>Ask the application to choose by using the configured resolver.</summary>
    Ask
}

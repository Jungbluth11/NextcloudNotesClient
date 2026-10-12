# NextcloudNotesClient

A small asynchronous .NET client for the [Nextcloud Notes API](https://github.com/nextcloud/notes). It supports listing, creating, reading, updating, and deleting notes, as well as reading and updating Notes settings.

## Requirements

- .NET 10
- A Nextcloud instance with the Notes app enabled
- A username and app password for that instance

The client builds the API base URL as `https://<server>/index.php/apps/notes/api/v1/`. The constructor requires HTTPS to protect credentials in transit.

## Add the project

Add a project reference to your solution:

```sh
dotnet add reference ../NextcloudNotesClient/NextcloudNotesClient.csproj
```

In C#:

```csharp
using Jungbluth11.NextcloudNotesClient;
```

## Connect

The simplest constructor creates and manages an `HttpClient` internally. Use a Nextcloud app password instead of your regular account password.

```csharp
using var client = new NextcloudNotesClient(
    serverUrl: "https://cloud.example.com",
    username: "alice",
    appPassword: Environment.GetEnvironmentVariable("NEXTCLOUD_APP_PASSWORD")!);
```

Alternatively, pass in your own `HttpClient`. In this case, the caller manages its lifetime and configures the base URL and authentication:

```csharp
using var httpClient = new HttpClient
{
    BaseAddress = new Uri("https://cloud.example.com/index.php/apps/notes/api/v1/")
};
httpClient.DefaultRequestHeaders.Authorization =
    new System.Net.Http.Headers.AuthenticationHeaderValue(
        "Basic",
        Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes("alice:APP_PASSWORD")));

using var client = new NextcloudNotesClient(httpClient);
```

## Work with notes

All network methods are asynchronous and accept a `CancellationToken`.

```csharp
using var cancellation = new CancellationTokenSource(TimeSpan.FromSeconds(20));

// Get all notes
IReadOnlyList<NextcloudNote> notes = await client.GetNotesAsync(
    cancellationToken: cancellation.Token);

// Get notes in a category
IReadOnlyList<NextcloudNote> workNotes = await client.GetNotesAsync(
    category: "Work",
    cancellationToken: cancellation.Token);

// Create a note
NextcloudNote created = await client.CreateNoteAsync(
    title: "Meeting",
    content: "Agenda and next steps",
    category: "Work",
    favorite: true,
    cancellationToken: cancellation.Token);

// Get a single note
NextcloudNote note = await client.GetNoteAsync(created.Id, cancellation.Token);
Console.WriteLine($"{note.Title} (last modified: {note.ModifiedAt})");

// Update a note
note.Title = "Meeting notes";
note.Content += "\n- Send feedback to the team";
NextcloudNote updated = await client.UpdateNoteAsync(
    note,
    cancellationToken: cancellation.Token);

// Delete a note
await client.DeleteNoteAsync(updated.Id, cancellation.Token);
```

`NextcloudNote` includes properties such as `Id`, `Title`, `Content`, `Category`, `Favorite`, `ReadOnly`, `ETag`, `Modified` (Unix time in seconds), and `ModifiedAt` (`DateTimeOffset`). Read-only notes cannot be updated; `UpdateNoteAsync` throws an `InvalidOperationException` for them.

### Concurrent changes (ETag)

ETag checking is enabled by default for `UpdateNoteAsync`. The client sends the note's ETag as an `If-Match` header so that an intervening server-side change is not silently overwritten. If there is a conflict, the client throws a `NextcloudNotesException` with status code `PreconditionFailed` (HTTP 412). Reload the current note and apply your changes to it.

```csharp
try
{
    await client.UpdateNoteAsync(note, useETag: true);
}
catch (NextcloudNotesException ex) when (
    ex.StatusCode == System.Net.HttpStatusCode.PreconditionFailed)
{
    Console.WriteLine("The note was changed on the server in the meantime.");
}
```

ETag checking can be disabled with `useETag: false`. Do this only when overwriting intervening changes is intentional.

Automatic handling of HTTP 412 conflicts is optional and disabled by default. Configure `NextcloudNotesClientOptions` when creating the client. `ServerFirst` returns the latest server version and discards the attempted local update; `ClientFirst` retries the local update using the current server ETag. `Ask` calls an application-provided asynchronous resolver with both versions, so the application can ask its user and return `ServerFirst` or `ClientFirst`:

```csharp
var options = new NextcloudNotesClientOptions
{
    ResolvePreconditionFailed = true,
    ConflictResolutionStrategy = ConflictResolutionStrategy.Ask,
    ConflictResolver = async (local, server, cancellationToken) =>
    {
        // Replace this with the application's UI or decision logic.
        return await AskWhichVersionToKeepAsync(local, server, cancellationToken);
    }
};

using var client = new NextcloudNotesClient(httpClient, options);
```

For a fixed strategy, set `ConflictResolutionStrategy` to `ServerFirst` or `ClientFirst` and omit `ConflictResolver`. If `Ask` is selected without a resolver, the client reports a configuration error after loading the current server version.

## Settings

```csharp
NextcloudNotesSettings settings = await client.GetSettingsAsync();
Console.WriteLine($"Notes path: {settings.NotesPath}");
Console.WriteLine($"File suffix: {settings.FileSuffix}");

settings.NotesPath = "Notes";
NextcloudNotesSettings saved = await client.UpdateSettingsAsync(settings);
```

`NextcloudNotesSettings` exposes `NotesPath` and `FileSuffix`. Whether these values can be changed depends on the Nextcloud Notes API and the user's permissions.

## Handle errors

HTTP errors are thrown as `NextcloudNotesException`. The exception provides a `StatusCode` and the unmodified response text in `ResponseBody`.

```csharp
try
{
    NextcloudNote note = await client.GetNoteAsync(42);
    Console.WriteLine(note.Title);
}
catch (NextcloudNotesException ex)
{
    Console.Error.WriteLine($"Nextcloud returned {(int)ex.StatusCode}: {ex.Message}");
    Console.Error.WriteLine(ex.ResponseBody);
}
```

The client provides helpful messages for common cases, including failed authentication (401), insufficient permissions (403), missing resources (404), and ETag conflicts (412). Network and cancellation errors may also be thrown as `HttpRequestException` or `OperationCanceledException`.

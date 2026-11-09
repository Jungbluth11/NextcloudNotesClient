using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

namespace Jungbluth11.NextcloudNotesClient;

/// <summary>Provides an asynchronous client for the Nextcloud Notes API.</summary>
/// <remarks>The caller manages the lifetime of a supplied <see cref="HttpClient"/>.</remarks>
public sealed class NextcloudNotesClient : IDisposable
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true, DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    private readonly HttpClient _http;
    private readonly bool _ownsClient;
    private readonly NextcloudNotesClientOptions _options;

    /// <summary>Creates a client and manages its <see cref="HttpClient"/> internally.</summary>
    /// <param name="serverUrl">HTTPS URL of the Nextcloud instance, for example https://cloud.example.com.</param>
    /// <param name="username">Nextcloud username.</param>
    /// <param name="appPassword">User's app password.</param>
    /// <param name="options">Optional conflict handling settings.</param>
    /// <exception cref="ArgumentException">An argument is invalid, or the URL does not use HTTPS.</exception>
    public NextcloudNotesClient(string serverUrl, string username, string appPassword, NextcloudNotesClientOptions? options = null)
        : this(CreateHttpClient(serverUrl, username, appPassword), true, options)
    {
    }

    /// <summary>Creates a client using an HTTP client configured by the caller.</summary>
    /// <param name="httpClient">HTTP client with the base address and authentication for the Notes API.</param>
    /// <param name="options">Optional conflict handling settings.</param>
    /// <remarks>This client does not dispose the supplied HTTP client.</remarks>
    /// <exception cref="ArgumentNullException"><paramref name="httpClient"/> is <see langword="null"/>.</exception>
    public NextcloudNotesClient(HttpClient httpClient, NextcloudNotesClientOptions? options = null) : this(httpClient, false, options)
    {
    }

    private NextcloudNotesClient(HttpClient httpClient, bool ownsClient, NextcloudNotesClientOptions? options)
    {
        _http = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _ownsClient = ownsClient;
        _options = options ?? new NextcloudNotesClientOptions();
    }

    /// <summary>Disposes resources created by this client.</summary>
    public void Dispose()
    {
        if (_ownsClient)
        {
            _http.Dispose();
        }
    }

    /// <summary>Creates a note.</summary>
    /// <param name="title">Title of the note.</param><param name="content">Content of the note.</param>
    /// <param name="category">Category; defaults to no category.</param><param name="favorite">Whether the note is a favorite.</param>
    /// <param name="cancellationToken">Token to cancel the HTTP request.</param>
    /// <returns>The note created by the server, including its server ID.</returns>
    /// <exception cref="NextcloudNotesException">The API request failed.</exception>
    public Task<NextcloudNote> CreateNoteAsync(
        string title,
        string content,
        string category = "",
        bool favorite = false,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(title);
        ArgumentNullException.ThrowIfNull(content);
        ArgumentNullException.ThrowIfNull(category);

        NotePayload payload = new() {Title = title, Content = content, Category = category, Favorite = favorite};

        return SendAsync<NextcloudNote>(HttpMethod.Post, "notes", payload, cancellationToken);
    }

    /// <summary>Deletes a note.</summary>
    /// <param name="id">Positive note ID.</param><param name="cancellationToken">Token to cancel the request.</param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="id"/> is not positive.</exception>
    /// <exception cref="NextcloudNotesException">The API request failed.</exception>
    public Task DeleteNoteAsync(int id, CancellationToken cancellationToken = default)
    {
        ValidateId(id);

        return SendAsync(HttpMethod.Delete, $"notes/{id}", cancellationToken);
    }

    /// <summary>Retrieves a single note by its ID.</summary>
    /// <param name="id">Positive note ID.</param><param name="cancellationToken">Token to cancel the request.</param>
    /// <returns>The requested note.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="id"/> is not positive.</exception>
    /// <exception cref="NextcloudNotesException">The API request failed.</exception>
    public Task<NextcloudNote> GetNoteAsync(int id, CancellationToken cancellationToken = default)
    {
        ValidateId(id);

        return SendAsync<NextcloudNote>(HttpMethod.Get, $"notes/{id}", null, cancellationToken);
    }

    /// <summary>Retrieves all notes or notes in a specific category.</summary>
    /// <param name="category">Optional category to filter by.</param><param name="cancellationToken">Token to cancel the request.</param>
    /// <returns>The matching notes.</returns><exception cref="NextcloudNotesException">The API request failed.</exception>
    public async Task<IReadOnlyList<NextcloudNote>> GetNotesAsync(string? category = null, CancellationToken cancellationToken = default)
    {
        string path = "notes";

        if (category is not null)
        {
            path += "?category=" + Uri.EscapeDataString(category);
        }

        return await SendAsync<List<NextcloudNote>>(HttpMethod.Get, path, null, cancellationToken)
            .ConfigureAwait(false);
    }

    /// <summary>Retrieves the Notes settings for the current user.</summary>
    /// <param name="cancellationToken">Token to cancel the request.</param><returns>The current settings.</returns>
    /// <exception cref="NextcloudNotesException">The API request failed.</exception>
    public Task<NextcloudNotesSettings> GetSettingsAsync(
        CancellationToken cancellationToken = default)
    {
        return SendAsync<NextcloudNotesSettings>(HttpMethod.Get, "settings", null, cancellationToken);
    }

    /// <summary>Updates an existing note.</summary>
    /// <param name="note">Note containing the values to save.</param>
    /// <param name="useETag">Uses the note's ETag by default to detect intervening changes.</param>
    /// <param name="cancellationToken">Token to cancel the request.</param>
    /// <returns>The note returned by the server after the update.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="note"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentOutOfRangeException">The note ID is not positive.</exception>
    /// <exception cref="InvalidOperationException">The note is read-only or the conflict configuration is invalid.</exception>
    /// <exception cref="NextcloudNotesException">The API request failed; with ETag protection this may include an HTTP 412 conflict.</exception>
    public Task<NextcloudNote> UpdateNoteAsync(
        NextcloudNote note,
        bool useETag = true,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(note);
        ValidateId(note.Id);

        if (note.ReadOnly)
        {
            throw new InvalidOperationException("This note is read-only.");
        }

        NotePayload payload = new()
        {
            Title = note.Title,
            Content = note.Content,
            Category = note.Category,
            Favorite = note.Favorite,
            Modified = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
        };

        return UpdateNoteCoreAsync(
            note,
            HttpMethod.Put,
            payload,
            cancellationToken,
            useETag ? note.ETag : null);
    }

    private async Task<NextcloudNote> UpdateNoteCoreAsync(NextcloudNote note, HttpMethod method, NotePayload payload,
        CancellationToken cancellationToken, string? ifMatch)
    {
        try
        {
            return await SendAsync<NextcloudNote>(method, $"notes/{note.Id}", payload, cancellationToken, ifMatch)
                .ConfigureAwait(false);
        }
        catch (NextcloudNotesException ex) when (ex.StatusCode == HttpStatusCode.PreconditionFailed && _options.ResolvePreconditionFailed)
        {
            NextcloudNote serverNote = await GetNoteAsync(note.Id, cancellationToken).ConfigureAwait(false);
            ConflictResolutionStrategy strategy = _options.ConflictResolutionStrategy;
            if (strategy == ConflictResolutionStrategy.Ask)
            {
                if (_options.ConflictResolver is null)
                    throw new InvalidOperationException("Ask conflict resolution requires a ConflictResolver callback.", ex);
                strategy = await _options.ConflictResolver(note, serverNote, cancellationToken).ConfigureAwait(false);
            }

            if (strategy == ConflictResolutionStrategy.ServerFirst)
                return serverNote;
            if (strategy == ConflictResolutionStrategy.ClientFirst)
            {
                if (serverNote.ReadOnly)
                    throw new InvalidOperationException("The server version of this note is read-only.", ex);
                note.ETag = serverNote.ETag;
                return await SendAsync<NextcloudNote>(HttpMethod.Put, $"notes/{note.Id}", payload, cancellationToken, serverNote.ETag)
                    .ConfigureAwait(false);
            }

            throw new InvalidOperationException("The conflict resolver must return ServerFirst or ClientFirst.", ex);
        }
    }

    /// <summary>Saves the Notes settings.</summary>
    /// <param name="settings">Settings to save.</param><param name="cancellationToken">Token to cancel the request.</param>
    /// <returns>The settings returned by the server.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="settings"/> is <see langword="null"/>.</exception>
    /// <exception cref="NextcloudNotesException">The API request failed.</exception>
    public Task<NextcloudNotesSettings> UpdateSettingsAsync(NextcloudNotesSettings settings, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(settings);

        return SendAsync<NextcloudNotesSettings>(HttpMethod.Put, "settings", settings, cancellationToken);
    }

    private static HttpClient CreateHttpClient(
        string serverUrl,
        string username,
        string appPassword)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(serverUrl);
        ArgumentException.ThrowIfNullOrWhiteSpace(username);
        ArgumentException.ThrowIfNullOrWhiteSpace(appPassword);

        Uri server = new(serverUrl.TrimEnd('/') + "/");

        if (server.Scheme != Uri.UriSchemeHttps && server.Scheme != Uri.UriSchemeHttp)
        {
            throw new ArgumentException("The server URL must use HTTP or HTTPS.", nameof(serverUrl));
        }

        if (server.Scheme != Uri.UriSchemeHttps)
        {
            throw new ArgumentException("HTTPS is required to transmit credentials securely.", nameof(serverUrl));
        }

        Uri baseUri = new(server, "index.php/apps/notes/api/v1/");

        HttpClient client = new() {BaseAddress = baseUri, Timeout = TimeSpan.FromSeconds(30)};

        string credentials = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{username}:{appPassword}"));
        client.DefaultRequestHeaders.Authorization = new("Basic", credentials);
        client.DefaultRequestHeaders.Accept.Add(new("application/json"));

        return client;
    }

    private static string GetErrorMessage(HttpStatusCode statusCode) =>
        statusCode switch
        {
            HttpStatusCode.Unauthorized => "Authentication failed. Check the username and app password.",
            HttpStatusCode.Forbidden => "The operation is not permitted. The note may be read-only.",
            HttpStatusCode.NotFound => "The requested note or API resource was not found.",
            HttpStatusCode.PreconditionFailed => "The note was modified on the server in the meantime (ETag conflict).",
            HttpStatusCode.BadRequest => "The server rejected the request.",
            _ => $"Nextcloud API error: HTTP {(int)statusCode} ({statusCode})."
        };

    private static void ValidateId(int id)
    {
        if (id <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(id), "The note ID must be positive.");
        }
    }

    private async Task<T> SendAsync<T>(
        HttpMethod method,
        string path,
        object? payload,
        CancellationToken cancellationToken,
        string? ifMatch = null)
    {
        using HttpRequestMessage request = new(method, path);

        if (payload is not null)
        {
            request.Content = JsonContent.Create(payload, options: JsonOptions);
        }

        if (!string.IsNullOrWhiteSpace(ifMatch))
        {
            // Notes API returns the ETag field without HTTP entity-tag quotes,
            // while its If-Match check expects the quoted HTTP syntax.
            string entityTag = ifMatch.Trim();

            if (entityTag.Length < 2 || entityTag[0] != '"' || entityTag[^1] != '"')
            {
                entityTag = $"\"{entityTag}\"";
            }

            request.Headers.TryAddWithoutValidation("If-Match", entityTag);
        }

        using HttpResponseMessage response = await _http.SendAsync(request,
                HttpCompletionOption.ResponseHeadersRead,
                cancellationToken)
            .ConfigureAwait(false);

        string responseBody = await response.Content.ReadAsStringAsync(cancellationToken)
            .ConfigureAwait(false);

        if (!response.IsSuccessStatusCode)
        {
            throw new NextcloudNotesException(response.StatusCode, responseBody, GetErrorMessage(response.StatusCode));
        }

        T? result = JsonSerializer.Deserialize<T>(responseBody, JsonOptions);

        return result ?? throw new InvalidOperationException("The Nextcloud API returned an empty or invalid JSON response.");
    }

    private async Task SendAsync(
        HttpMethod method,
        string path,
        CancellationToken cancellationToken)
    {
        using HttpResponseMessage response = await _http.SendAsync(new(method, path), cancellationToken).ConfigureAwait(false);

        if (!response.IsSuccessStatusCode)
        {
            string body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

            throw new NextcloudNotesException(response.StatusCode, body, GetErrorMessage(response.StatusCode));
        }
    }

    private sealed class NotePayload
    {
        [JsonPropertyName("category")] public string? Category { get; init; }

        [JsonPropertyName("content")] public string? Content { get; init; }

        [JsonPropertyName("favorite")] public bool? Favorite { get; init; }

        [JsonPropertyName("modified")] public long? Modified { get; init; }

        [JsonPropertyName("title")] public string? Title { get; init; }
    }
}

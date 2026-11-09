using System.Net;

namespace Jungbluth11.NextcloudNotesClient;

/// <summary>Represents a failed HTTP request to the Nextcloud Notes API.</summary>
/// <param name="statusCode">HTTP status code of the response.</param><param name="responseBody">Unmodified body of the error response.</param><param name="message">Human-readable error message.</param>
public sealed class NextcloudNotesException(HttpStatusCode statusCode, string responseBody, string message) : Exception(message)
{
    /// <summary>Unmodified response body from the server.</summary>
    public string ResponseBody { get; } = responseBody;
    /// <summary>HTTP status code returned by the server.</summary>
    public HttpStatusCode StatusCode { get; } = statusCode;
}

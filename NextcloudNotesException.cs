using System.Net;

namespace Jungbluth11.NextcloudNotesClient;

public sealed class NextcloudNotesException(HttpStatusCode statusCode, string responseBody, string message) : Exception(message)
{
    public string ResponseBody { get; } = responseBody;
    public HttpStatusCode StatusCode { get; } = statusCode;
}
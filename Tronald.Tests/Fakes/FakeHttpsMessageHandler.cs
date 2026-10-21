using System.Net;
using System.Text;

namespace Tronald.Tests.Fakes;

public class FakeHttpMessageHandler(HttpStatusCode status, string content, string mediaType = "application/json")
    : HttpMessageHandler
{
    public HttpRequestMessage? LastRequest { get; private set; }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        LastRequest = request;
        return Task.FromResult(new HttpResponseMessage(status)
        {
            Content = new StringContent(content, Encoding.UTF8, mediaType)
        });
    }
}

public class ThrowingHttpMessageHandler : HttpMessageHandler
{
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
        throw new HttpRequestException("No network");
}
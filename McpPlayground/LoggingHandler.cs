namespace McpPlayground;

public class LoggingHandler : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        // Journalisation de la requête
        Console.WriteLine($"Request: {request.Method} {request.RequestUri}");

        if (request.Content != null)
        {
            var requestContent = await request.Content.ReadAsStringAsync(cancellationToken);
            Console.WriteLine($"========================");
            Console.WriteLine($"Request Content: {requestContent}");
            Console.WriteLine($"========================");
        }

        // Envoi de la requête
        var response = await base.SendAsync(request, cancellationToken);

        Console.WriteLine($"Response: {request.Method} {request.RequestUri}");

        var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
        Console.WriteLine($"========================");
        Console.WriteLine($"Response Content: {responseContent}");
        Console.WriteLine($"========================");

        return response;

    }
}
namespace SseWpfClient;

public record SseResponse(DateTime Date, string Action, decimal Value);
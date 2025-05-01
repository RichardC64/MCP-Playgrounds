namespace SseWebApi;

public record SseResponse(DateTime Date, string Action, decimal Value);
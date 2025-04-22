using System.Text.Json.Serialization;

namespace AspNetMcpServer;

public class Message
{
    public required string Content { get; set; }
}
[JsonSourceGenerationOptions(WriteIndented = true)]
[JsonSerializable(typeof(Message))]
public partial class JsonContext : JsonSerializerContext
{

}
using System.Text.Json;
using System.Text.Json.Serialization;

[JsonSerializable(typeof(Message))]
public class Message
{
    public required string Content { get; set; }
}

public partial class JsonContext : JsonSerializerContext
{

}
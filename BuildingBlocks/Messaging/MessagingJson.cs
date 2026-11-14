using System.Text.Json;
using System.Text.Json.Serialization;

namespace BuildingBlocks.Messaging
{
    /// <summary>JSON settings used for every integration event (camelCase, enums as strings).</summary>
    public static class MessagingJson
    {
        public static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
        {
            Converters = { new JsonStringEnumConverter() }
        };

        public static string Serialize<TEvent>(TEvent integrationEvent) where TEvent : IntegrationEvent
            => JsonSerializer.Serialize(integrationEvent, Options);

        public static TEvent? Deserialize<TEvent>(string payload) where TEvent : IntegrationEvent
            => JsonSerializer.Deserialize<TEvent>(payload, Options);
    }
}

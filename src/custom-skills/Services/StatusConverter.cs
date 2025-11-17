using System.Text.Json;
using System.Text.Json.Serialization;

namespace PhiDeidPortal.CustomFunctions.Services
{
    public class StatusConverterService : JsonConverter<string>
    {
        public override string Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.Number && reader.TryGetInt32(out int intValue))
            {
                return intValue.ToString();
            }
            else if (reader.TokenType == JsonTokenType.String)
            {
                return reader.GetString();
            }
            throw new JsonException("Unexpected token type for Status field");
        }

        public override void Write(Utf8JsonWriter writer, string value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(value);
        }
    }
}

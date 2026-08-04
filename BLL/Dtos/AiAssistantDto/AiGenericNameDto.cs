using System.Text.Json.Serialization;

namespace BLL.Dtos.AiAssistantDto
{
    public class AiGenericNameDto
    {
        [JsonPropertyName("OriginalName")]
        public string OriginalName { get; set; } = string.Empty;

        [JsonPropertyName("GenericName")]
        public string GenericName { get; set; } = string.Empty;
    }
}

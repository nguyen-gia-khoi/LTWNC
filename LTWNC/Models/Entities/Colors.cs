using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System.Text.Json.Serialization;

namespace LTWNC.Models.Entities
{
    public class Colors
    {
        [BsonId]
        [BsonElement("_id"), BsonRepresentation(BsonType.ObjectId)]
        public string? Id { get; set; }

        [BsonElement("colors_name")]
        [JsonPropertyName("colors_name")]
        public string? ColorsName { get; set; } // ví dụ: "Black"

        [BsonElement("colors_code")]
        [JsonPropertyName("colors_code")]
        public string? ColorsCode { get; set; } // ví dụ: "#000000"
    }
}

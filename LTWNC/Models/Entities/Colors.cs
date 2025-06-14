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
            [JsonPropertyName("colors_name")]  // 👈 JSON cần có field "colors_name"
            public string? ColorsName { get; set; }

            [BsonElement("colors_code")]
            [JsonPropertyName("colors_code")]  // 👈 JSON cần có field "colors_code"
            public string? ColorsCode { get; set; }
        

    }
}

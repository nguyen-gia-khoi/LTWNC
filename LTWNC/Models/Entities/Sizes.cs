using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System.Text.Json.Serialization;

namespace LTWNC.Models.Entities
{
    public class Sizes
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        [JsonPropertyName("id")]
        public string? Id { get; set; }

        [BsonElement("size_name")]
        [JsonPropertyName("size_name")]
        public string? SizesName { get; set; }

        [BsonElement("sizes_code")]
        [JsonPropertyName("sizes_code")]
        public string? SizesCode { get; set; }
    }
}

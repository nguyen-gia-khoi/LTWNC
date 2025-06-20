using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System.Text.Json.Serialization;

namespace LTWNC.Models.Entities
{
    public class Categories
    {
        [BsonId]
        [BsonElement("_id"), BsonRepresentation(BsonType.ObjectId)]
        public string? Id { get; set; }

        [BsonElement("categories_name")]
        [JsonPropertyName("categories_name")]  // 👈 JSON cần có field "colors_name"
        public string? CategoriesName { get; set; }

        [BsonElement("categories_code")]
        [JsonPropertyName("categories_code")]  // 👈 JSON cần có field "colors_code"
        public string? CategoriesCode { get; set; }
    }
}

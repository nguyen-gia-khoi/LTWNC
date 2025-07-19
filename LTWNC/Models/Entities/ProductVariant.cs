using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System.Text.Json.Serialization;

namespace LTWNC.Models.Entities
{
    public class ProductVariant
    {
        [BsonElement("colorId")]
        [BsonRepresentation(BsonType.ObjectId)]
        [JsonPropertyName("colorId")]
        public string? ColorId { get; set; }

        [BsonElement("sizeId")]
        [BsonRepresentation(BsonType.ObjectId)]
        [JsonPropertyName("sizeId")]
        public string? SizeId { get; set; }

        [BsonElement("quantity")]
        [JsonPropertyName("quantity")]
        public int Quantity { get; set; }

        [BsonElement("price")]
        [JsonPropertyName("price")]
        public decimal Price { get; set; }
    }
}

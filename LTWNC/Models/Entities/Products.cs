using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace LTWNC.Models.Entities
{
    public class Product
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? Id { get; set; }

        [BsonElement("name")]
        public string? Name { get; set; }

        [BsonElement("price")]
        public decimal Price { get; set; }

        [BsonElement("description")]
        public string? Description { get; set; }

        [BsonElement("images")]
        public List<string>? Images { get; set; } // URLs từ Cloudinary

        [BsonElement("variants")]
        public List<ProductVariant>? Variants { get; set; } // size + color + quantity

        [BsonElement("categoryId")]
        public string? CategoryId { get; set; } // ID của category
        [BsonElement("createdAt")]
        [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}

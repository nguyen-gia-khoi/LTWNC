using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace LTWNC.Entities
{
    public class Sizes
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? Id { get; set; }

        [BsonElement("size_name")]
        public string? SizesName { get; set; }

        [BsonElement("sizes_code")]
        public string? SizesCode { get; set; }
    }
}

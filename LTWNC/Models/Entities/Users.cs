using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System.Text.Json.Serialization;

namespace LTWNC.Models.Entities
{
    [BsonIgnoreExtraElements]
    public class Users
    {
        [BsonId]
        [BsonElement("_id"), BsonRepresentation(BsonType.ObjectId)]
        public string? Id { get; set; }

        [BsonElement("customer_name")]
        [JsonPropertyName("customer_name")]
        public string? CustomerName { get; set; }

        [BsonElement("age")]
        public int Age { get; set; }

        [BsonElement("email")]
        public string? Email { get; set; }

        [BsonElement("password")]
        public string? Password { get; set; }

        [BsonElement("role")]
        [BsonRepresentation(BsonType.String)]
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public Role Role { get; set; }

        [BsonElement("gender")]
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public Gender Gender { get; set; }

        [BsonElement("phoneNumber")]
        [JsonPropertyName("phoneNumber")]
        public string? PhoneNumber { get; set; }
    }

    public enum Role
    {
        Admin,
        User,
        Guest
    }

    public enum Gender
    {
        Male,
        Female,
        Undefined
    }
}

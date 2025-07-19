using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;

public class Order
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }

    [BsonElement("customerID")]
    public string? CustomerID { get; set; }

    [BsonElement("customerPhone")]
    public string? CustomerPhone { get; set; }

    [BsonElement("customerAddress")]
    public string? CustomerAddress { get; set; }

    [BsonElement("price")]
    public decimal Price { get; set; }

    [BsonElement("items")]
    public List<OrderItem> Items { get; set; }

    [BsonElement("payingStatus")]
    public string payingStatus { get; set; } = "pending";

    [BsonElement("createdAt")]
    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class OrderItem
{
    public string? ProductId { get; set; }
    public string? ColorId { get; set; }
    public string? SizeId { get; set; }
    public decimal Price { get; set; }
    public int Quantity { get; set; }
    public string? CategoryId { get; set; }
}

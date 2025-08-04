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
    public List<OrderItem> items { get; set; }

    [BsonElement("deliveryStatus")]  // Mới: Thêm trường trạng thái giao hàng
    public string deliveryStatus { get; set; } = "pending";

    [BsonElement("payingStatus")]
    public string payingStatus { get; set; } = "pending";

    [BsonElement("payPalOrder")]
    public string? PayPalOrderId { get; set; }

    [BsonElement("createdAt")]
    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class OrderItem
{
    public string? ProductId { get; set; }
    public string? ColorId { get; set; }
    public string? SizeId { get; set; }
    
    public int Quantity { get; set; }
    public string? CategoryId { get; set; }
}

using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Models;

public class Product
{
    [BsonId]
    [BsonRepresentation(BsonType.String)]
    public Guid Id { get; set; }
    public string Name { get; set; }
    public string? Description { get; set; }
    public decimal StartPrice { get; set; }
    public decimal? currentbid { get; set; }
    public string? Brand { get; set; }
    public string? Image { get; set; }
    public DateTime EndOfAuction { get; set; } = DateTime.Now.AddDays(30);
    public int? CurrentBidder { get; set; }
    public List<BidHistory>? BidHistory { get; set; } = new();

    public bool IsApproved { get; set; } = false;
        
    public int CustomerNumber { get; set; }

}

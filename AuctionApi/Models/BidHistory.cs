namespace Models;

public class BidHistory
{
    public string ProductId { get; set; }
    public int BidderId { get; set; }
    public decimal BidAmount { get; set; }
    public DateTime BidTime { get; set; }
}


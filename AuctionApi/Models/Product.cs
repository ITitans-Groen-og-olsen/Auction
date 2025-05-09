namespace Models;

public class Product
{
	public Guid Id { get; set; }
	public string? Name { get; set; }
	public string? Description { get; set; }
	public decimal StartPrice { get; set; }
	public decimal currentPrice { get; set; }
	public string? Brand { get; set; }
	public string? ImageUrl { get; set; }
    public DateTime? EndOfAuction {get; set;}

}
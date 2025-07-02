public class Ticket {
    public Guid Id { get; set; }
    public int Event { get; set; }
    public required string Ticketholder { get; set; }
    public required string Seating { get; set; }
    public Boolean? Reserved { get; set; }
    public DateTime? Expiry { get; set; }
}
public class Event {
    public int? Id { get; set; }
    public required string Name { get; set; }
    public required string Start { get; set; }
    public required string Venue { get; set; }
    public required string Description { get; set; }
    public required int Capacity { get; set; }
    public required int Sold { get; set; }
}
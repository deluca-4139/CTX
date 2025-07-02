public class Event {
    public int? Id { get; set; }
    public required string Name { get; set; }
    public required string Start { get; set; }
    public required string Venue { get; set; }
    public required string Description { get; set; }
    public required int Capacity { get; set; }
    public required int Sold { get; set; }

    // For some reason, when I add the non-required Id parameter,
    // having this constructor breaks everything. So I'm commenting
    // it out for now, but leaving it in case I need it later
    // public Event(
    //     string name,
    //     string start,
    //     string venue,
    //     string description,
    //     int capacity,
    //     int sold
    // ) {
    //     Name = name;
    //     Start = start;
    //     Venue = venue;
    //     Description = description;
    //     Capacity = capacity;
    //     Sold = sold;
    // }
}
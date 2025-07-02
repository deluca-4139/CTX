public class Event {
    public string Name { get; set; }
    public string Start { get; set; }
    public string Venue { get; set; }
    public string Description { get; set; }
    public int Capacity { get; set; }
    public int Sold { get; set; }

    public Event(
        string name,
        string start,
        string venue,
        string description,
        int capacity,
        int sold
    ) {
        Name = name;
        Start = start;
        Venue = venue;
        Description = description;
        Capacity = capacity;
        Sold = sold;
    }
}
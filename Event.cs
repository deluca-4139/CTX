public class Event {
    public string Name { get; set; }
    public int Capacity { get; set; }

    public Event(string name, int capacity) {
        Name = name;
        Capacity = capacity;
    }
}
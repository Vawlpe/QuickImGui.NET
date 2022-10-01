namespace QuickImGuiNET;

public class Event
{
    public Event(Dictionary<string, Event>? children = null)
    {
        Children = children ?? new();
        Hook += (a) => { return null; };
    }
    public delegate dynamic? Signature(params dynamic[]? args);
    public event Signature Hook;
    public dynamic? Invoke(params dynamic[]? args) => Hook(args);
    public readonly Dictionary<string,Event> Children;
    public Event this[string idx] => Children[idx];
}
namespace QuickImGuiNET;

public class Event
{
    public delegate dynamic? Signature(params dynamic[]? args);

    public readonly Dictionary<string, Event> Children;

    public Event(Dictionary<string, Event>? children = null)
    {
        Children = children ?? new Dictionary<string, Event>();
        Hook += args => { return null; };
    }

    public Event this[string idx] => Children[idx];
    public event Signature Hook;

    public dynamic? Invoke(params dynamic[]? args)
    {
        return Hook(args);
    }
}
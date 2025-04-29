public interface IEventWriter
{
    void Write(
        string actorName,
        string code = "",
        string name = "",
        string desc = "",
        string severity = ""
        );
}

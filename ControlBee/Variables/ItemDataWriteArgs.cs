namespace ControlBee.Variables;

public class ItemDataWriteArgs(object[] location, object newValue)
{
    public object[] Location { get; } = location;
    public object NewValue { get; } = newValue;
}

namespace ControlBee.Variables;

public class ValueChangedEventArgs(object? location, object? oldValue, object? newValue) : EventArgs
{
    public object? Location { get; } = location;
    public object? OldValue { get; } = oldValue;
    public object? NewValue { get; } = newValue;
}

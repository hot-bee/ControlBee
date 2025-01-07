namespace ControlBee.Variables;

public class ValueChangedEventArgs : EventArgs
{
    public ValueChangedEventArgs(object? location, object? oldValue, object? newValue)
    {
        Location = location;
        OldValue = oldValue;
        NewValue = newValue;
    }

    public object? Location { get; }
    public object? OldValue { get; }
    public object? NewValue { get; }
}

using System.Diagnostics.CodeAnalysis;
using ControlBeeAbstract.Exceptions;

namespace ControlBee.Variables;

public class ItemDataWriteArgs
{
    public ItemDataWriteArgs()
    {
    }

    [SetsRequiredMembers]
    public ItemDataWriteArgs(object[] location, object newValue)
    {
        Location = location;
        NewValue = newValue;
    }

    [SetsRequiredMembers]
    public ItemDataWriteArgs(ItemDataWriteArgs other)
    {
        Location = other.Location.ToArray();
        NewValue = other.NewValue;
        MinValue = other.MinValue;
        MaxValue = other.MaxValue;
    }

    public required object[] Location { get; init; }
    public required object NewValue { get; init; }
    public double? MinValue { get; init; }
    public double? MaxValue { get; init; }

    public void EnsureNewValueInRange()
    {
        double? doubleValue = NewValue switch
        {
            double d => d,
            int i => i,
            _ => null
        };
        if (MaxValue < doubleValue) throw new ValueError($"New value({doubleValue}) is greater than MaxValue({MaxValue})");
        if (doubleValue < MinValue) throw new ValueError($"New value({doubleValue}) is less than MinValue({MinValue})");
    }
}
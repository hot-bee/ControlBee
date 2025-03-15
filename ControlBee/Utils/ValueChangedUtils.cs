using System.Runtime.CompilerServices;
using ControlBee.Variables;

namespace ControlBee.Utils;

public class ValueChangedUtils
{
    public static bool SetField<T>(
        ref T field,
        T value,
        Action<ValueChangedArgs> onValueChanged,
        [CallerMemberName] string? propertyName = null
    )
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
            return false;
        var oldValue = field;
        field = value;
        onValueChanged(new ValueChangedArgs([propertyName!], oldValue, value));
        return true;
    }
}

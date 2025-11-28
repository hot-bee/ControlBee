using ControlBee.Interfaces;
using ControlBee.Variables;

namespace ControlBee.Services;

public class VariableFactory
{
    // Written by GPT.
    public static IVariable CreateVariable(Type valueType)
    {
        if (valueType == null)
            throw new ArgumentNullException(nameof(valueType));

        // Enforce the new() constraint at runtime
        if (valueType.GetConstructor(Type.EmptyTypes) == null && !valueType.IsValueType)
            throw new ArgumentException($"{valueType} must have a public parameterless constructor.",
                nameof(valueType));

        // Build Variable<valueType>
        var genericType = typeof(Variable<>).MakeGenericType(valueType);

        // Create instance: this calls the parameterless ctor of Variable<T>
        var instance = Activator.CreateInstance(genericType);

        return (IVariable)instance!;
    }
}
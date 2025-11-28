using System.Text.Json.Serialization;

namespace ControlBee.Interfaces;

public interface IIndex1D
{
    [JsonIgnore]
    public int Size { get; }
    object? GetValue(int index);
    void SetValue(int index, object value);
}
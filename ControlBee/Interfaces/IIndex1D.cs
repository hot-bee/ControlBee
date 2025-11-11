namespace ControlBee.Interfaces;

public interface IIndex1D
{
    [System.Text.Json.Serialization.JsonIgnore]
    [Newtonsoft.Json.JsonIgnore]
    public int Size { get; }
    object? GetValue(int index);
}
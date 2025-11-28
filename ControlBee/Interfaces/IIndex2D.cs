namespace ControlBee.Interfaces;

public interface IIndex2D
{
    object? GetValue(int index1, int index2);
    void SetValue((int, int) index, object value);
    void SetValue(int index1, int index2, object value);
}

using ControlBee.Variables;

namespace ControlBee.Interfaces;

public interface INotifyValueChanged
{
    public event EventHandler<ValueChangedArgs>? ValueChanging;
    public event EventHandler<ValueChangedArgs>? ValueChanged;
}
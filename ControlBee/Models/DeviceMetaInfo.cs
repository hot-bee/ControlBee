using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace ControlBee.Models;

public class DeviceMetaInfo : INotifyPropertyChanged
{
    private bool _aborted;

    public bool Aborted
    {
        get => _aborted;
        private set => SetField(ref _aborted, value);
    }

    public void Abort()
    {
        Aborted = true;
    }

    public void ResetAbort()
    {
        Aborted = false;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    protected bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
            return false;
        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }
}

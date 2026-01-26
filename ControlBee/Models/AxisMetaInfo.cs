using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace ControlBee.Models;

public class AxisMetaInfo : INotifyPropertyChanged
{
    private bool _aborted;

    private bool _initialized;

    public bool Aborted
    {
        get => _aborted;
        set => SetField(ref _aborted, value);
    }

    public bool Initialized
    {
        get => _initialized;
        set => SetField(ref _initialized, value);
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

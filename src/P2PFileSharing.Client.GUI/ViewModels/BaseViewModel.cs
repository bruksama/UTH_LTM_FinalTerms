using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace P2PFileSharing.Client.GUI.ViewModels;

/// <summary>
/// Base ViewModel class implementing INotifyPropertyChanged
/// TODO: Implement property change notification helpers if needed
/// </summary>
public abstract class BaseViewModel : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>
    /// Raises PropertyChanged event
    /// TODO: Implement helper method to set property and raise PropertyChanged
    /// </summary>
    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    /// <summary>
    /// Sets property value and raises PropertyChanged if value changed
    /// TODO: Implement SetProperty helper method
    /// </summary>
    protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
            return false;

        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }
}


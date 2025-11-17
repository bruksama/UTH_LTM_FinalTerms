using System.Windows.Input;

namespace P2PFileSharing.Client.GUI.ViewModels;

/// <summary>
/// Simple RelayCommand implementation for ICommand
/// </summary>
public class RelayCommand : ICommand
{
    private readonly Func<object?, Task> _executeAsync;
    private readonly Func<bool>? _canExecute;

    public RelayCommand(Func<object?, Task> executeAsync, Func<bool>? canExecute = null)
    {
        _executeAsync = executeAsync ?? throw new ArgumentNullException(nameof(executeAsync));
        _canExecute = canExecute;
    }

    public RelayCommand(Action execute, Func<bool>? canExecute = null)
        : this(_ => { execute(); return Task.CompletedTask; }, canExecute)
    {
    }

    public event EventHandler? CanExecuteChanged
    {
        add { CommandManager.RequerySuggested += value; }
        remove { CommandManager.RequerySuggested -= value; }
    }

    public bool CanExecute(object? parameter)
    {
        return _canExecute?.Invoke() ?? true;
    }

    public async void Execute(object? parameter)
    {
        await _executeAsync(parameter);
    }
}


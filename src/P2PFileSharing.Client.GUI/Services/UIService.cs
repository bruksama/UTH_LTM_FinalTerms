namespace P2PFileSharing.Client.GUI.Services;

/// <summary>
/// Service để xử lý các tương tác UI như dialogs, notifications
/// TODO: Implement UI dialogs and notifications
/// </summary>
public class UIService
{
    /// <summary>
    /// Hiển thị thông báo lỗi
    /// TODO: Show error message dialog or toast notification
    /// </summary>
    public void ShowError(string message, string title = "Error")
    {
        // TODO: Show MessageBox or custom error dialog
        System.Windows.MessageBox.Show(message, title, System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
    }

    /// <summary>
    /// Hiển thị thông báo thành công
    /// TODO: Show success notification
    /// </summary>
    public void ShowSuccess(string message, string title = "Success")
    {
        // TODO: Show MessageBox or toast notification
        System.Windows.MessageBox.Show(message, title, System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
    }

    /// <summary>
    /// Hiển thị dialog xác nhận
    /// TODO: Show confirmation dialog
    /// </summary>
    public bool ShowConfirmation(string message, string title = "Confirm")
    {
        // TODO: Show MessageBox with Yes/No buttons
        var result = System.Windows.MessageBox.Show(message, title, System.Windows.MessageBoxButton.YesNo, System.Windows.MessageBoxImage.Question);
        return result == System.Windows.MessageBoxResult.Yes;
    }

    /// <summary>
    /// Hiển thị dialog chọn file
    /// TODO: Show OpenFileDialog
    /// </summary>
    public string[]? ShowOpenFileDialog(string? initialDirectory = null, string filter = "All Files|*.*")
    {
        // TODO: Show Microsoft.Win32.OpenFileDialog
        // TODO: Return selected file paths
        return null;
    }

    /// <summary>
    /// Hiển thị dialog chọn thư mục
    /// TODO: Show FolderBrowserDialog or use Windows API
    /// </summary>
    public string? ShowFolderDialog(string? initialDirectory = null)
    {
        // TODO: Show folder selection dialog
        // TODO: Return selected folder path
        return null;
    }

    /// <summary>
    /// Hiển thị thông báo toast (non-blocking)
    /// TODO: Implement toast notification system
    /// </summary>
    public void ShowToast(string message, ToastType type = ToastType.Info)
    {
        // TODO: Show toast notification (could use a custom control or library)
    }
}

/// <summary>
/// Loại toast notification
/// </summary>
public enum ToastType
{
    Info,
    Success,
    Warning,
    Error
}


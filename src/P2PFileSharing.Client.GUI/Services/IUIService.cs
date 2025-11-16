using System.Threading.Tasks;

namespace P2PFileSharing.Client.GUI.Services;

/// <summary>
/// Interface (hợp đồng) để xử lý các tương tác UI như dialogs, notifications.
/// </summary>
public interface IUIService
{
    /// <summary>
    /// Hiển thị thông báo lỗi
    /// </summary>
    void ShowError(string message, string title = "Error");

    /// <summary>
    /// Hiển thị thông báo thành công
    /// </summary>
    void ShowSuccess(string message, string title = "Success");

    /// <summary>
    /// Hiển thị dialog xác nhận (Yes/No)
    /// Chúng ta đổi nó thành Async để hỗ trợ hộp thoại tùy chỉnh (non-blocking)
    /// </summary>
    Task<bool> ShowConfirmationAsync(string message, string title = "Confirm");

    /// <summary>
    /// Hiển thị dialog chọn file
    /// </summary>
    string[]? ShowOpenFileDialog(string? initialDirectory = null, string filter = "All Files|*.*");

    /// <summary>
    /// Hiển thị dialog chọn thư mục
    /// </summary>
    string? ShowFolderDialog(string? initialDirectory = null);

    /// <summary>
    /// Hiển thị thông báo toast (non-blocking)
    /// </summary>
    void ShowToast(string message, ToastType type = ToastType.Info);

    /// <summary>
    /// Hiển thị hộp thoại nhận file tùy chỉnh
    /// </summary>
    Task<bool> ShowFileTransferRequestAsync(string fromPeer, string fileName, string fileSize);
}

/// <summary>
/// Loại toast notification (giữ nguyên bên ngoài interface)
/// </summary>
public enum ToastType
{
    Info,
    Success,
    Warning,
    Error
}
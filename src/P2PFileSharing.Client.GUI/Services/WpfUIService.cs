using P2PFileSharing.Client.GUI.Services;
using P2PFileSharing.Client.GUI.Views;
using System;
using System.IO; // Thêm
using System.Threading.Tasks;
using System.Windows;
using Microsoft.Win32; // Thêm

namespace P2PFileSharing.Client.GUI
{
    /// <summary>
    /// Lớp triển khai IUIService, chịu trách nhiệm hiển thị các dialog WPF
    /// </summary>
    public class WpfUIService : IUIService
    {
        /// <summary>
        /// Helper để đảm bảo một hành động được chạy trên UI thread
        /// </summary>
        private void RunOnUIThread(Action action)
        {
            if (Application.Current != null)
            {
                Application.Current.Dispatcher.Invoke(action);
            }
            else
            {
                action.Invoke();
            }
        }

        /// <summary>
        /// Hiển thị dialog xác nhận Yes/No đơn giản
        /// </summary>
        public async Task<bool> ShowConfirmationAsync(string message, string title = "Confirm")
        {
            bool result = false;
            
            await Task.Run(() =>
            {
                RunOnUIThread(() =>
                {
                    // Constructor (title, message)
                    var dialogContent = new ConfirmationDialog(title, message); 
                
                    var shell = new DialogShell(dialogContent) { Title = title };
                    shell.ShowDialog();
                    result = dialogContent.Result;
                });
            });
            
            return result;
        }

        /// <summary>
        /// Hiển thị dialog nhận file tùy chỉnh
        /// </summary>
        public async Task<bool> ShowFileTransferRequestAsync(string fromPeer, string fileName, string fileSize)
        {
            bool result = false;
            
            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                string title = "File Transfer Request";
                string message = "Do you want to accept this file?";
                
                // Constructor (title, message, from, file, size)
                var dialogContent = new ConfirmationDialog(title, message, fromPeer, fileName, fileSize);
                
                var shell = new DialogShell(dialogContent) { Title = title };
                shell.ShowDialog();
                result = dialogContent.Result;
            });

            return result;
        }

        // Triển khai các phương thức còn lại (dùng MessageBox tạm thời)
        
        public void ShowError(string message, string title = "Error")
        {
            RunOnUIThread(() => MessageBox.Show(message, title, 
                MessageBoxButton.OK, MessageBoxImage.Error));
        }
        
        public void ShowSuccess(string message, string title = "Success")
        {
            RunOnUIThread(() => MessageBox.Show(message, title, 
                MessageBoxButton.OK, MessageBoxImage.Information));
        }
        
        public void ShowToast(string message, ToastType type = ToastType.Info)
        {
             RunOnUIThread(() => MessageBox.Show(message, type.ToString(), 
                MessageBoxButton.OK, MessageBoxImage.Information));
        }
        
        /// <summary>
        /// Hiển thị dialog chọn file
        /// </summary>
        public string[]? ShowOpenFileDialog(string? initialDirectory = null, string filter = "All Files|*.*")
        {
            var dialog = new OpenFileDialog
            {
                Title = "Select Files",
                Filter = filter,
                InitialDirectory = initialDirectory,
                Multiselect = true
            };

            if (dialog.ShowDialog() == true)
            {
                return dialog.FileNames;
            }
            return null;
        }

        /// <summary>
        /// Hiển thị dialog chọn thư mục (KHÔNG DÙNG WinForms)
        /// </summary>
        public string? ShowFolderDialog(string? initialDirectory = null)
        {
            var dialog = new OpenFileDialog
            {
                Title = "Select a Folder",
                InitialDirectory = initialDirectory,
                
                // Cấu hình "hack" để chọn thư mục
                CheckFileExists = false,
                CheckPathExists = true,
                ValidateNames = false,
                FileName = "Folder Selection" // Tên này sẽ bị ẩn đi
            };

            if (dialog.ShowDialog() == true)
            {
                // Lấy đường dẫn thư mục từ tên file
                return Path.GetDirectoryName(dialog.FileName);
            }
            return null;
        }
    }
}
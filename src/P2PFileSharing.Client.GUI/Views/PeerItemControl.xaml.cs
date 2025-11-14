using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using P2PFileSharing.Client.GUI.ViewModels;

namespace P2PFileSharing.Client.GUI.Views;

/// <summary>
/// Interaction logic for PeerItemControl.xaml
/// Xử lý drag & drop để gửi file cho peer
/// </summary>
public partial class PeerItemControl : UserControl
{
    public PeerItemControl()
    {
        InitializeComponent();

        // Cho phép drop trên toàn control
        AllowDrop = true;

        DragEnter += OnDragEnter;
        DragLeave += OnDragLeave;
        DragOver  += OnDragOver;
        Drop      += OnDrop;
    }

    /// <summary>
    /// Xử lý khi file được drag over control
    /// </summary>
    private void OnDragOver(object sender, DragEventArgs e)
    {
        if (e.Data.GetDataPresent(DataFormats.FileDrop))
        {
            e.Effects = DragDropEffects.Copy;
        }
        else
        {
            e.Effects = DragDropEffects.None;
        }

        e.Handled = true;
    }

    /// <summary>
    /// Xử lý khi file được drop vào control
    /// </summary>
    private async void OnDrop(object sender, DragEventArgs e)
    {
        try
        {
            if (!e.Data.GetDataPresent(DataFormats.FileDrop))
                return;

            var files = e.Data.GetData(DataFormats.FileDrop) as string[];
            if (files is not { Length: > 0 })
                return;

            // VM của từng peer
            if (DataContext is not PeerViewModel peerVm)
                return;

            // Lấy MainViewModel từ MainWindow để nó quản lý Transfer list
            if (Application.Current.MainWindow?.DataContext is MainViewModel mainVm)
            {
                await mainVm.HandleFileDropAsync(peerVm, files);
            }
            else
            {
                // Fallback: gửi trực tiếp qua PeerViewModel
                await peerVm.SendFilesAsync(files);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error dropping files: {ex.Message}",
                "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            ResetHighlight();
            e.Handled = true;
        }
    }

    /// <summary>
    /// Drag enter – đổi màu để user thấy vùng drop hợp lệ
    /// </summary>
    private void OnDragEnter(object sender, DragEventArgs e)
    {
        if (!e.Data.GetDataPresent(DataFormats.FileDrop))
            return;

        SetHighlight();
        e.Handled = true;
    }

    /// <summary>
    /// Drag leave – trả UI về bình thường
    /// </summary>
    private void OnDragLeave(object sender, DragEventArgs e)
    {
        ResetHighlight();
        e.Handled = true;
    }

    private void SetHighlight()
    {
        // Nhớ thêm x:Name="DropZoneBorder" cho Border drop zone trong XAML
        if (FindName("DropZoneBorder") is Border border)
        {
            border.BorderBrush = Brushes.DodgerBlue;
            border.Background  = new SolidColorBrush(Color.FromArgb(40, 30, 144, 255));
        }
    }

    private void ResetHighlight()
    {
        if (FindName("DropZoneBorder") is Border border)
        {
            border.BorderBrush = Brushes.Blue;
            border.Background  = Brushes.LightBlue;
        }
    }
}

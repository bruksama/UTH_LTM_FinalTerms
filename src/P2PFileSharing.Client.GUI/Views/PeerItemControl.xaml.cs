using System.Windows;
using System.Windows.Controls;
using P2PFileSharing.Client.GUI.ViewModels;

namespace P2PFileSharing.Client.GUI.Views;

/// <summary>
/// Interaction logic for PeerItemControl.xaml
/// TODO: Implement drag & drop handlers for file transfer
/// </summary>
public partial class PeerItemControl : UserControl
{
    public PeerItemControl()
    {
        InitializeComponent();
        
        // TODO: Subscribe to drag & drop events
        this.DragOver += OnDragOver;
        this.Drop += OnDrop;
    }

    /// <summary>
    /// Xử lý khi file được drag over control
    /// TODO: Validate file types and show visual feedback
    /// </summary>
    private void OnDragOver(object sender, System.Windows.DragEventArgs e)
    {
        // TODO: Check if data contains files
        if (e.Data.GetDataPresent(System.Windows.DataFormats.FileDrop))
        {
            e.Effects = System.Windows.DragDropEffects.Copy;
            e.Handled = true;
            
            // TODO: Change border color to indicate valid drop zone
        }
        else
        {
            e.Effects = System.Windows.DragDropEffects.None;
            e.Handled = true;
        }
    }

    /// <summary>
    /// Xử lý khi file được drop vào control
    /// TODO: Extract file paths and trigger file transfer
    /// </summary>
    private async void OnDrop(object sender, System.Windows.DragEventArgs e)
    {
        // TODO: Get dropped files
        if (e.Data.GetDataPresent(System.Windows.DataFormats.FileDrop))
        {
            string[] files = (string[])e.Data.GetData(System.Windows.DataFormats.FileDrop);
            var peerViewModel = DataContext as PeerViewModel;
            
            if (peerViewModel != null && files != null && files.Length > 0)
            {
                // TODO: Call MainViewModel.HandleFileDropAsync() or peerViewModel.SendFilesAsync()
                // TODO: Show progress indicator
                // TODO: Handle errors
                await peerViewModel.SendFilesAsync(files);
            }
        }
        
        e.Handled = true;
    }

    /// <summary>
    /// Xử lý khi drag enter (optional - for visual feedback)
    /// TODO: Change appearance when dragging over
    /// </summary>
    private void OnDragEnter(object sender, System.Windows.DragEventArgs e)
    {
        // TODO: Highlight drop zone
    }

    /// <summary>
    /// Xử lý khi drag leave (optional - for visual feedback)
    /// TODO: Restore normal appearance
    /// </summary>
    private void OnDragLeave(object sender, System.Windows.DragEventArgs e)
    {
        // TODO: Remove highlight
    }
}


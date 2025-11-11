using System.Collections.ObjectModel;
using System.Windows.Input;
using P2PFileSharing.Common.Models;

namespace P2PFileSharing.Client.GUI.ViewModels;

/// <summary>
/// ViewModel cho một Peer item trong danh sách
/// TODO: Implement peer information display and file sharing functionality
/// </summary>
public class PeerViewModel : BaseViewModel
{
    private readonly PeerInfo _peerInfo;
    private bool _isOnline;
    private string _status = "Online";

    public PeerViewModel(PeerInfo peerInfo)
    {
        _peerInfo = peerInfo;
        SharedFiles = new ObservableCollection<SharedFileViewModel>();
        
        // TODO: Initialize SharedFiles from peerInfo.SharedFiles
        
        // TODO: Initialize commands
        SendFileCommand = new RelayCommand(async (param) => 
        {
            if (param is string filePath)
                await SendFileAsync(filePath);
        });
    }

    /// <summary>
    /// Username của peer
    /// TODO: Bind to UI
    /// </summary>
    public string Username => _peerInfo.Username;

    /// <summary>
    /// Địa chỉ IP và Port của peer
    /// TODO: Format as "IP:Port" and bind to UI
    /// </summary>
    public string Address => $"{_peerInfo.IpAddress}:{_peerInfo.ListenPort}";

    /// <summary>
    /// Trạng thái online/offline
    /// TODO: Update based on LastSeen and timeout
    /// </summary>
    public bool IsOnline
    {
        get => _isOnline;
        set => SetProperty(ref _isOnline, value);
    }

    /// <summary>
    /// Status text hiển thị trên UI
    /// TODO: Update based on connection status
    /// </summary>
    public string Status
    {
        get => _status;
        set => SetProperty(ref _status, value);
    }

    /// <summary>
    /// Danh sách file mà peer đang chia sẻ
    /// TODO: Populate from PeerInfo.SharedFiles
    /// </summary>
    public ObservableCollection<SharedFileViewModel> SharedFiles { get; }

    /// <summary>
    /// Command để gửi file đến peer này
    /// TODO: Implement file sending logic
    /// </summary>
    public ICommand SendFileCommand { get; }

    /// <summary>
    /// Gửi file đến peer này
    /// TODO: Integrate with FileTransferManager.SendFileAsync()
    /// TODO: Show progress dialog
    /// TODO: Handle errors and show notifications
    /// </summary>
    public async Task<bool> SendFileAsync(string filePath)
    {
        // TODO: Validate file exists
        // TODO: Call FileTransferManager.SendFileAsync(_peerInfo.IpAddress, _peerInfo.ListenPort, filePath)
        // TODO: Update UI with transfer progress
        // TODO: Return success/failure
        await Task.CompletedTask;
        return false;
    }

    /// <summary>
    /// Gửi nhiều file cùng lúc
    /// TODO: Implement batch file sending
    /// </summary>
    public async Task SendFilesAsync(IEnumerable<string> filePaths)
    {
        // TODO: Iterate through files and send each one
        // TODO: Show overall progress
        await Task.CompletedTask;
    }

    /// <summary>
    /// Cập nhật thông tin peer từ PeerInfo mới
    /// TODO: Update properties when peer info changes
    /// </summary>
    public void UpdateFromPeerInfo(PeerInfo peerInfo)
    {
        // TODO: Update all properties from new PeerInfo
        // TODO: Update SharedFiles collection
        // TODO: Raise PropertyChanged events
    }
}


/*using System;
using System.Collections.Generic;
using System.Threading.Tasks;*/
using System.Collections.ObjectModel;
using System.Windows.Input;
using P2PFileSharing.Common.Models;

namespace P2PFileSharing.Client.GUI.ViewModels;

/// <summary>
/// ViewModel cho một Peer item trong danh sách
/// Hiển thị thông tin peer và hỗ trợ gửi file tới peer đó.
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

        // Khởi tạo SharedFiles từ PeerInfo.SharedFiles
        if (peerInfo.SharedFiles != null)
        {
            foreach (var file in peerInfo.SharedFiles)
            {
                SharedFiles.Add(new SharedFileViewModel(file));
            }
        }

        // Khởi tạo IsOnline dựa trên LastSeen
        _isOnline = peerInfo.IsOnline(TimeSpan.FromMinutes(5));
        _status = _isOnline ? "Online" : "Offline";

        // Command gửi file
        SendFileCommand = new RelayCommand(
            async param =>
            {
                if (param is string filePath)
                    await SendFileAsync(filePath);
            });
    }

    #region Properties

    /// <summary>
    /// Username của peer
    /// </summary>
    public string Username => _peerInfo.Username;

    /// <summary>
    /// Địa chỉ IP và Port của peer (format "IP:Port")
    /// </summary>
    public string Address => $"{_peerInfo.IpAddress}:{_peerInfo.ListenPort}";

    /// <summary>
    /// Trạng thái online/offline
    /// </summary>
    public bool IsOnline
    {
        get => _isOnline;
        set => SetProperty(ref _isOnline, value);
    }

    /// <summary>
    /// Status text hiển thị trên UI
    /// </summary>
    public string Status
    {
        get => _status;
        set => SetProperty(ref _status, value);
    }

    /// <summary>
    /// Danh sách file mà peer đang chia sẻ
    /// </summary>
    public ObservableCollection<SharedFileViewModel> SharedFiles { get; }

    /// <summary>
    /// Command để gửi file đến peer này
    /// </summary>
    public ICommand SendFileCommand { get; }

    #endregion

    #region Methods

    /// <summary>
    /// Gửi file đến peer này
    /// TODO: tích hợp FileTransferManager.SendFileAsync(...) thực tế
    /// </summary>
    public async Task<bool> SendFileAsync(string filePath)
    {
        try
        {
            if (!System.IO.File.Exists(filePath))
                return false;

            // TODO: Call FileTransferManager.SendFileAsync(_peerInfo.IpAddress, _peerInfo.ListenPort, filePath)
            // Hiện tại mô phỏng transfer cho mục đích test GUI
            await Task.Delay(1000);

            return true; // Placeholder: sau này trả về kết quả thực
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Gửi nhiều file cùng lúc
    /// </summary>
    public async Task SendFilesAsync(IEnumerable<string> filePaths)
    {
        foreach (var filePath in filePaths)
        {
            await SendFileAsync(filePath);
        }
    }

    /// <summary>
    /// Cập nhật thông tin peer từ PeerInfo mới (khi refresh/scan)
    /// </summary>
    public void UpdateFromPeerInfo(PeerInfo peerInfo)
    {
        // Update online status
        IsOnline = peerInfo.IsOnline(TimeSpan.FromMinutes(5));
        Status = IsOnline ? "Online" : "Offline";

        // Update shared files
        SharedFiles.Clear();
        if (peerInfo.SharedFiles != null)
        {
            foreach (var file in peerInfo.SharedFiles)
            {
                SharedFiles.Add(new SharedFileViewModel(file));
            }
        }

        // Notify UI cập nhật lại address/username nếu thay đổi
        OnPropertyChanged(nameof(Username));
        OnPropertyChanged(nameof(Address));
    }

    #endregion
}

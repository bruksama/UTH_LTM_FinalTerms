# P2PFileSharing.Client.GUI

WPF Desktop Application cho P2P File Sharing với tính năng Drag & Drop.

## Cấu trúc Project

```
P2PFileSharing.Client.GUI/
├── Views/                    # WPF Views (XAML)
│   ├── MainWindow.xaml       # Main window layout
│   ├── PeerItemControl.xaml  # Peer item với drag & drop zone
│   └── TransferItemControl.xaml  # Transfer progress display
├── ViewModels/               # MVVM ViewModels
│   ├── BaseViewModel.cs      # Base class với INotifyPropertyChanged
│   ├── MainViewModel.cs      # Main window ViewModel
│   ├── PeerViewModel.cs      # Peer item ViewModel
│   ├── SharedFileViewModel.cs # Shared file ViewModel
│   ├── TransferViewModel.cs  # Transfer progress ViewModel
│   └── RelayCommand.cs       # ICommand implementation
└── Services/                 # Business logic services
    ├── PeerClientService.cs  # Wrapper around PeerClient
    └── UIService.cs          # UI dialogs và notifications
```

## Kiến trúc MVVM

Project sử dụng MVVM pattern để tách biệt UI và business logic:

- **Models**: Sử dụng từ `P2PFileSharing.Common.Models`
- **Views**: XAML files với code-behind tối thiểu
- **ViewModels**: Chứa UI logic và bindings
- **Services**: Wrapper cho business logic từ Client project

## Tính năng chính

### Đã hoàn thành
- ✅ Cấu trúc MVVM
- ✅ Base ViewModels với INotifyPropertyChanged
- ✅ RelayCommand implementation

### Cần implement
- ✅ MainWindow layout với connection panel
- ✅ Peer list với drag & drop
- ✅ File transfer progress tracking
- ✅ Integration với PeerClient business logic
- ✅ Error handling và notifications

## Drag & Drop Flow

1. User kéo file từ Windows Explorer
2. Drop vào PeerItemControl
3. `PeerItemControl.xaml.cs` xử lý Drop event
4. Gọi `PeerViewModel.SendFileAsync()` hoặc `MainViewModel.HandleFileDropAsync()`
5. Tạo `TransferViewModel` và thêm vào `Transfers` collection
6. Update progress trong real-time

## Build và Chạy

### Build
```bash
cd src/P2PFileSharing.Client.GUI
dotnet build
```

### Chạy
```bash
dotnet run
```

Hoặc từ solution root:
```bash
dotnet run --project src/P2PFileSharing.Client.GUI
```

## Dependencies

- `P2PFileSharing.Common` - Models, Protocol, Utilities
- `P2PFileSharing.Client` - Business logic (cần reference hoặc copy code)

## TODO Checklist

Xem `docs/PROJECT_SETUP.md` section "P2PFileSharing.Client.GUI" để biết chi tiết các công việc cần làm.

## Notes

- Tất cả các file đều có TODO comments để guide implementation
- ViewModels sử dụng async/await pattern
- UI updates phải được thực hiện trên UI thread (Dispatcher)
- File transfers sẽ được track qua TransferViewModel với progress updates


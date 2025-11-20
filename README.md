# P2P File Sharing Application

Ứng dụng Chia sẻ File P2P Lai qua mạng LAN (Hybrid Peer-to-Peer File Sharing Application over LAN)

## Mô tả

Dự án này là một ứng dụng chia sẻ file Peer-to-Peer (P2P) lai hoạt động trong mạng LAN. Hệ thống sử dụng một máy chủ Registry để điều phối việc đăng ký và khám phá các peer, trong khi việc truyền file được thực hiện trực tiếp giữa các peer qua kết nối TCP.

## Yêu cầu Hệ thống

- .NET 8.0 SDK hoặc mới hơn
- Windows/Linux/macOS
- Mạng LAN (cùng subnet)

## Cấu trúc Dự án

```
UTH_LTM_FinalTerms/
├── src/
│   ├── P2PFileSharing.Common/     # Thư viện chung (models, protocol, utilities)
│   ├── P2PFileSharing.Server/     # Ứng dụng Server (Registry)
│   └── P2PFileSharing.Client/     # Ứng dụng Client (Peer)
├── docs/                           # Tài liệu
└── P2PFileSharing.sln             # Solution file
```

## Cài đặt và Chạy

### 1. Clone repository

```bash
git clone <repository-url>
cd UTH_LTM_FinalTerms
```

### 2. Restore dependencies

```bash
dotnet restore
```

### 3. Build solution

```bash
dotnet build
```

### 4. Chạy Server

```bash
cd src/P2PFileSharing.Server
dotnet run
```

Server mặc định lắng nghe trên port 5000.

### 5. Chạy Client

Mở terminal mới:

```bash
cd src/P2PFileSharing.Client
dotnet run
```

## Cấu hình

### Server

Server có thể được cấu hình qua command-line arguments hoặc file config:
- Port: Port để lắng nghe (mặc định: 5000)
- Timeout: Thời gian timeout cho peer heartbeat (mặc định: 5 phút)

### Client

Client có thể được cấu hình:
- Server IP: Địa chỉ IP của Registry Server
- Server Port: Port của Registry Server (mặc định: 5000)
- Listen Port: Port để lắng nghe kết nối P2P (mặc định: 5001)
- Shared Directory: Thư mục chứa các file chia sẻ

## Sử dụng

### Server Commands

- Server chạy liên tục và hiển thị log các sự kiện
- Nhấn Ctrl+C để dừng server

### Client Commands

- `list` - Hiển thị danh sách các peer đang online và file họ chia sẻ
- `scan` - Quét mạng LAN bằng UDP broadcast để tìm peer
- `send <peer_name> <file_name>` - Gửi file đến peer
- `quit` hoặc `exit` - Thoát ứng dụng

## Kiến trúc

### Server (Registry)
- Quản lý danh sách peer đang online
- Xử lý đăng ký và hủy đăng ký peer
- Cung cấp danh sách peer khi được yêu cầu
- Không tham gia vào quá trình truyền file

### Client (Peer)
- Đăng ký với Server khi khởi động
- Khám phá peer qua Server hoặc UDP broadcast
- Gửi và nhận file trực tiếp với các peer khác qua TCP
- Đo lường hiệu năng truyền file

## Phát triển

### Công cụ Phát triển

- IDE: JetBrains Rider hoặc Visual Studio 2022
- .NET SDK: 8.0 hoặc mới hơn
- Công cụ phân tích mạng: Wireshark (để kiểm thử)

### Build và Test

```bash
# Build toàn bộ solution
dotnet build

# Chạy tests (nếu có)
dotnet test
```

## Tài liệu

Xem thêm thông tin ở `docs/` để biết chi tiết về đặc tả yêu cầu phần mềm.

## License

Dự án này được phát triển cho mục đích học tập.

## Nhóm Phát triển

Dự án được phát triển bởi nhóm 5 thành viên.


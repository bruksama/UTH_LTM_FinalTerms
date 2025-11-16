using System.Windows;
using System.Windows.Controls;

namespace P2PFileSharing.Client.GUI.Views
{
    public partial class ConfirmationDialog : UserControl
    {
        // true = Accept, false = Decline
        public bool Result { get; private set; } = false;

        // Constructor mặc định – để designer/preview xài
        public ConfirmationDialog()
        {
            InitializeComponent();

            // Text mẫu cho preview
            TitleText.Text    = "File transfer request";
            MessageText.Text  = "Do you want to accept this file?";
            FromText.Text     = "From: PreviewPeer";
            FileNameText.Text = "File: preview.zip";
            FileSizeText.Text = "Size: 10.5 MB";
        }

        // Constructor đơn giản: chỉ title + message
        public ConfirmationDialog(string title, string message)
            : this()
        {
            TitleText.Text   = title;
            MessageText.Text = message;
        }

        // Constructor đầy đủ nếu m muốn truyền thêm info
        public ConfirmationDialog(
            string title,
            string message,
            string fromPeer,
            string fileName,
            string fileSize)
            : this(title, message)
        {
            if (!string.IsNullOrWhiteSpace(fromPeer))
                FromText.Text = $"From: {fromPeer}";
            else
                FromText.Visibility = Visibility.Collapsed;

            if (!string.IsNullOrWhiteSpace(fileName))
                FileNameText.Text = $"File: {fileName}";
            else
                FileNameText.Visibility = Visibility.Collapsed;

            if (!string.IsNullOrWhiteSpace(fileSize))
                FileSizeText.Text = $"Size: {fileSize}";
            else
                FileSizeText.Visibility = Visibility.Collapsed;
        }

        private void CloseDialog(bool result)
        {
            Result = result;
            Window.GetWindow(this)?.Close();
        }

        private void YesButton_Click(object sender, RoutedEventArgs e)
        {
            CloseDialog(true);
        }

        private void NoButton_Click(object sender, RoutedEventArgs e)
        {
            CloseDialog(false);
        }
    }
}

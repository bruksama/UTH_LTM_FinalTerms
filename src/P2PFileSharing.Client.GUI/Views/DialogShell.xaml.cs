using System.Windows;
using System.Windows.Controls;

namespace P2PFileSharing.Client.GUI
{
    public partial class DialogShell : Window
    {
        public DialogShell(UserControl content)
        {
            InitializeComponent();
            
            // Đặt chủ sở hữu là MainWindow
            // Điều này đảm bảo nó luôn ở trên MainWindow
            this.Owner = Application.Current.MainWindow;
            
            // Chèn UserControl (ConfirmationDialog) vào
            DialogContent.Content = content;
        }
    }
}
using CustomCareConverter.ViewModels;
using MahApps.Metro.Controls;

namespace CustomCareConverter
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : MetroWindow
    {
        public MainWindow()
        {
            InitializeComponent();
            DataContext = new ConvertFileViewModel();
        }
    }
}

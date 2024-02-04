using MahApps.Metro.Controls;
using System.Windows;

namespace LethalMenuAssistant
{
    /// <summary>
    /// Interaction logic for Loading.xaml
    /// </summary>
    public partial class Loading : MetroWindow
    {

        public Loading()
        {
            InitializeComponent();
            Loaded += MetroWindow_Loaded;
        }

        private void MetroWindow_Loaded(object sender, RoutedEventArgs e)
        {
            _ = LMAssistant.Init();
        }

    }
}

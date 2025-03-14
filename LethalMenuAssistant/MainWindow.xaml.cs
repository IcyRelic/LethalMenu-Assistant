using DarkByteLauncher.Properties;
using DarkByteLib;
using DarkByteInjector;
using MahApps.Metro.Controls;
using Microsoft.Win32;
using System.Collections;
using System.ComponentModel;
using System.Diagnostics;
using System.Net.Http;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using System.Collections.ObjectModel;
using System.Windows.Input;
using System.Configuration;
using DarkByteLauncher.Commands;
using System.Windows.Media;
using System.Windows.Documents;
using System.Windows.Data;
using System.Windows.Shapes;


namespace DarkByteLauncher
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : MetroWindow
    {

        public Game SelectedGame { get; set; } = DarkByte.Games[0];
        public ObservableCollection<GameMenuItemModel> MenuItems { get; set; } = new ObservableCollection<GameMenuItemModel>();
        public bool CanInject { get => SelectedGame.IsRunning && !Injected && !AutoInjectStarted; }
        public bool CanAutoInject { get => SelectedGame.HasExecutable && !Injected && !SelectedGame.IsRunning && !AutoInjectStarted; }


        public bool Injected { get; set; } = false;
        public bool AutoInjectStarted { get; set; } = false;

        public string MenuStatus { get { return Injected ? "Injected" : AutoInjectStarted ? "Injecting..." : "Ejected"; } }

        public bool IsSidebarOpen { get; set; }

        public string LauncherHomeText { get => DarkByte.LauncherHomeText; }

        private void ToggleSidebar(object sender, RoutedEventArgs e)
        {
            IsSidebarOpen = !IsSidebarOpen;
            UpdateBindings();
        }



        public MainWindow()
        {
            DataContext = this;
            InitializeComponent();
            SetupControls();


            _ = UpdateBindingsTask();
        }

        private async Task UpdateBindingsTask()
        {
           while(true)
            {
                UpdateBindings();
                await Task.Delay(5000);
            }
        }

        private void UpdateBindings()
        {
            FindVisualChildren<TextBlock>(this).ToList().ForEach(x =>
            {
                x.GetBindingExpression(TextBlock.TextProperty)?.UpdateTarget();

                x.Inlines.OfType<Run>().ToList().ForEach(y =>
                {
                    BindingExpression bindingExpression = BindingOperations.GetBindingExpression(y, Run.TextProperty);
                    if (bindingExpression != null) bindingExpression.UpdateTarget();
                });
            });
            FindVisualChildren<Panel>(this).ToList().ForEach(x =>
            {
                if (x.GetBindingExpression(Panel.VisibilityProperty) == null) return;
                
                x.GetBindingExpression(Panel.VisibilityProperty)?.UpdateTarget();
            });

            FindVisualChildren<Button>(this).ToList().ForEach(x =>
            {
                x.GetBindingExpression(Button.IsEnabledProperty)?.UpdateTarget();
            });

            FindVisualChildren<Run>(this).ToList().ForEach(x =>
            {
                
                BindingExpression bindingExpression = BindingOperations.GetBindingExpression(x, Run.TextProperty);
                bindingExpression?.UpdateTarget();
            });

            ((Flyout)FindName("SidebarFlyout")).GetBindingExpression(Flyout.IsOpenProperty).UpdateTarget();
        }

        private static IEnumerable<T> FindVisualChildren<T>(DependencyObject parent) where T : DependencyObject
        {
            if (parent == null) yield break;

            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                if (child is T tChild)
                    yield return tChild;

                foreach (var descendant in FindVisualChildren<T>(child))
                    yield return descendant;
            }
        }

        private void SetupVersionSelect()
        {
            ComboBox cb = (ComboBox)FindName("GameVersionSelect");
            cb.SelectedItem = null;
            cb.Items.Clear();
            cb.Items.Add("Latest");

            SelectedGame.Versions.ForEach(v =>
            {
                cb.Items.Add(v);
                //if(v.Equals(Settings.Default.LMVersion)) cb.SelectedItem = v;

            });

            if (cb.SelectedItem == null) cb.SelectedItem = "Latest";
        }

        private void SetupControls()
        {
            SetupVersionSelect();
            NumericUpDown nud = (NumericUpDown)FindName("AutoInjectDelayNumeric");
            nud.Value = Settings.Default.AutoInjectDelay;

            DarkByte.Games.ForEach(g =>
            {
                MenuItems.Add(
                    new GameMenuItemModel { 
                        Game = g,
                        Command = new RelayCommand(_ =>
                        {
                            SelectedGame = g;
                            IsSidebarOpen = false;
                            UpdateBindings(); 
                            SetupVersionSelect();
                        } )
                    }
                );
            });
        }

        private string GetSelectedVersionURL()
        {
            ComboBox cb = (ComboBox)FindName("GameVersionSelect");
            
            string urlBase = "https://icyrelic.com/release/lethalmenu/";
            string selected = cb.SelectedItem.ToString();
            

            string url = selected.ToLower().Equals("latest") ? $"{urlBase}{SelectedGame.DownloadSettings.DLLFileName}.dll" : $"{urlBase}{SelectedGame.DownloadSettings.DLLFileName}_{selected.Replace(".", "_")}.dll";
            
            Debug.WriteLine(url);
            return url;
        }   
        private void LaunchGithub(object sender, RoutedEventArgs e)
        {
            if (String.IsNullOrEmpty(SelectedGame.GitHubURL)) return;
            Process.Start(new ProcessStartInfo(SelectedGame.GitHubURL) { UseShellExecute = true } );
        }

        private void LaunchDiscord(object sender, RoutedEventArgs e)
        {
            Process.Start(new ProcessStartInfo("discord://discord.com/invite/D6wuXEnfhP") { UseShellExecute = true });
        }

        private void LaunchUC(object sender, RoutedEventArgs e)
        {
            if (String.IsNullOrEmpty(SelectedGame.UnknownCheatsURL)) return;
            Process.Start(new ProcessStartInfo(SelectedGame.UnknownCheatsURL) { UseShellExecute = true });
        }

        private void OpenGameFolder(object sender, RoutedEventArgs e)
        {
            Process.Start("explorer.exe", SelectedGame.SteamInstallFolder);
        }

        public void FindGameFolder(object sender, RoutedEventArgs e)
        {
            OpenFolderDialog ofd = new OpenFolderDialog();
            var result = ofd.ShowDialog(this);

            if (result == true)
            {
                string path = ofd.FolderName;
                bool valid = SelectedGame.SetExecutable(path);

                if(valid)
                {
                    //Settings.Default.LCPath = path;
                    //Settings.Default.Save();
                    UpdateBindings();
                }
                else MessageBox.Show($"{SelectedGame.ProcessName} Not Found!", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void AutoInjectDelayPreview(object sender, TextCompositionEventArgs e)
        {
            e.Handled = true;
        }

        private void AutoInjectDelayChanged(object sender, RoutedPropertyChangedEventArgs<double?> e)
        {
            Console.WriteLine(e.NewValue);
            Settings.Default.AutoInjectDelay = (int) e.NewValue;
            Settings.Default.Save();
        }

        private void VersionSelected(object sender, SelectionChangedEventArgs e)
        {
            if (((ComboBox)sender).SelectedItem == null) return;
            Console.WriteLine(((ComboBox)sender).SelectedItem.ToString());
            //Settings.Default.LMVersion = ((ComboBox)sender).SelectedItem.ToString();

            Settings.Default.Save();
        }

        private void TabSelected(object sender, SelectionChangedEventArgs e) => UpdateBindings();


        private void Inject(object sender, RoutedEventArgs e)
        {
            if (!CanInject) return;
            Inject();
            Injected = true;
            UpdateBindings();
            MessageBox.Show($"Injected {SelectedGame.DownloadSettings.DLLFileName}", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            Close();
        }

        private void AutoInject(object sender, RoutedEventArgs e)
        {
            if(!CanAutoInject) return;
            AutoInjectStarted = true;
            _ = AutoInject();
        }

        private IntPtr Inject()
        {
            Process? p = SelectedGame.GetProcess() ;
            if (p == null) return IntPtr.Zero;

            Injector injector = new Injector(p);
            HttpClient client = new HttpClient();
            string url = GetSelectedVersionURL();

            var data = client.GetByteArrayAsync(url).Result;

            return injector.Inject(data, SelectedGame.InjectionSettings.Namespace, SelectedGame.InjectionSettings.Class, SelectedGame.InjectionSettings.Method);
        }

        private async Task AutoInject()
        {
            LaunchGame();

            await Task.Delay((int) ((NumericUpDown)FindName("AutoInjectDelay")).Value * 1000);
            Inject();
            Injected = true;
            UpdateBindings();
            MessageBox.Show($"Auto-Injected {SelectedGame.DownloadSettings.DLLFileName}", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            Close();
        }
        private void LaunchGame(object sender = null, RoutedEventArgs e = null)
        {
            if (SelectedGame.IsRunning) return;

            if (SelectedGame.FoundSteamInstall) Process.Start(new ProcessStartInfo($"steam://rungameid/{SelectedGame.GameID}") { UseShellExecute = true });
            else Process.Start(SelectedGame.GameExe);
        }
    }

    public class GameMenuItemModel
    {
        public Game Game { get; set; }
        public ICommand Command { get; set; }
        public BitmapImage IconImage => DarkByte.Base64ToImage(Game.IconBase64);
    }

}

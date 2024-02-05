
using LethalMenuAssistant;
using LethalMenuAssistant.Properties;
using LMAssistantLib;
using LMInjector;
using MahApps.Metro.Controls;
using Microsoft.Win32;
using Newtonsoft.Json.Linq;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Windows;
using System.Windows.Controls;


namespace LethalMenuAssistant
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : MetroWindow
    {
        public bool CanInject { get => LCRunning && !Injected && !AutoInjectStarted; }
        public bool CanAutoInject { get => FoundLC && !Injected && !LCRunning && !AutoInjectStarted; }
        public bool LCRunning { get { return GetLethalCompanyProcess() != null; } }
        public bool FoundLC { get => Steam.HasLCPath(); }
        public bool Injected { get; set; } = false;
        public bool AutoInjectStarted { get; set; } = false;
        public string LCStatus { get { return LCRunning ? "Running" : "Not Running"; } }
        public string LMStatus { get { return Injected ? "Injected" : AutoInjectStarted ? "Injecting..." : "Ejected"; } }
        public string Changelog { get { return String.Join("\n", LMAssistant.changelog); } }


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
            StackPanel autoInject = (StackPanel)FindName("AutoInjectPanel");
            StackPanel autoInjectNoLC = (StackPanel)FindName("AutoInjectNoLCPanel");
            Grid lcFoundPanel = (Grid)FindName("LCFoundPanel");
            StackPanel lcNotFoundPanel = (StackPanel)FindName("LCNotFoundPanel");
            Button manualInjectBtn = (Button)FindName("ManualInjectBtn");
            Button autoInjectBtn = (Button)FindName("AutoInjectBtn");
            TextBlock lcStatus = (TextBlock)FindName("LCStatusText");
            TextBlock lmStatus = (TextBlock)FindName("LMStatusText");

            autoInject.GetBindingExpression(StackPanel.VisibilityProperty).UpdateTarget();
            autoInjectNoLC.GetBindingExpression(StackPanel.VisibilityProperty).UpdateTarget();
            lcFoundPanel.GetBindingExpression(Grid.VisibilityProperty).UpdateTarget();
            lcNotFoundPanel.GetBindingExpression(StackPanel.VisibilityProperty).UpdateTarget();
            manualInjectBtn.GetBindingExpression(Button.IsEnabledProperty).UpdateTarget();
            autoInjectBtn.GetBindingExpression(Button.IsEnabledProperty).UpdateTarget();
            lcStatus.GetBindingExpression(TextBlock.TextProperty).UpdateTarget();
            lmStatus.GetBindingExpression(TextBlock.TextProperty).UpdateTarget();
        }

        private void SetupControls()
        {
            ComboBox cb = (ComboBox)FindName("LMVersionSelect");
            cb.Items.Add("Latest");
            
            LMAssistant.versions.ForEach(v =>
            {
                cb.Items.Add(v);
                if(v.Equals(Settings.Default.LMVersion)) cb.SelectedItem = v;

            });
            
            if(cb.SelectedItem == null) cb.SelectedItem = "Latest";

            NumericUpDown nud = (NumericUpDown)FindName("AutoInjectDelayNumeric");
            nud.Value = Settings.Default.AutoInjectDelay;
        }

        private string GetSelectedVersionURL()
        {
            ComboBox cb = (ComboBox)FindName("LMVersionSelect");
            
            string urlBase = "https://icyrelic.com/release/lethalmenu/";
            string selected = cb.SelectedItem.ToString();

            string url = selected.ToLower().Equals("latest") ? $"{urlBase}LethalMenu.dll" : $"{urlBase}LethalMenu_{selected.Replace(".", "_")}.dll";
            
            Console.WriteLine(url);
            return url;
        }   
        private void LaunchGithub(object sender, RoutedEventArgs e)
        {
            Process.Start(new ProcessStartInfo("https://github.com/IcyRelic/LethalMenu") { UseShellExecute = true } );
        }

        private void LaunchDiscord(object sender, RoutedEventArgs e)
        {
            Process.Start(new ProcessStartInfo("discord://discord.com/invite/D6wuXEnfhP") { UseShellExecute = true });
        }

        private void LaunchUC(object sender, RoutedEventArgs e)
        {
            Process.Start(new ProcessStartInfo("https://www.unknowncheats.me/forum/lethal-company/615575-lethal-menu-lethal-company-cheat.html") { UseShellExecute = true });
        }

        public void LaunchDonate(object sender, RoutedEventArgs e)
        {
            Process.Start(new ProcessStartInfo("https://buymeacoffee.com/icyrelic") { UseShellExecute = true });
        }

        private void LaunchLethalCompany(object sender, RoutedEventArgs e)
        {
            LaunchLC();
        }

        private void OpenLCFolder(object sender, RoutedEventArgs e)
        {
            Process.Start("explorer.exe", Steam.LCFolder);
        }

        public void FindLCFolder(object sender, RoutedEventArgs e)
        {
            OpenFolderDialog ofd = new OpenFolderDialog();
            var result = ofd.ShowDialog(this);

            if (result == true)
            {
                string path = ofd.FolderName;
                bool valid = Steam.SetLCPath(path);

                if(valid)
                {
                    Settings.Default.LCPath = path;
                    Settings.Default.Save();
                    UpdateBindings();
                }
                else MessageBox.Show("Lethal Company.exe Not Found!", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void AutoInjectDelayPreview(object sender, System.Windows.Input.TextCompositionEventArgs e)
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
            Console.WriteLine(((ComboBox)sender).SelectedItem.ToString());
            Settings.Default.LMVersion = ((ComboBox)sender).SelectedItem.ToString();
            
            Settings.Default.Save();
        }

        private void Inject(object sender, RoutedEventArgs e)
        {
            if (!CanInject) return;
            Inject();
            Injected = true;
            UpdateBindings();
            MessageBox.Show("Injected LethalMenu", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
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
            Process? p = GetLethalCompanyProcess();
            if (p == null) return IntPtr.Zero;

            Injector injector = new Injector(p);
            HttpClient client = new HttpClient();
            string url = GetSelectedVersionURL();

            var data = client.GetByteArrayAsync(url).Result;
            
            return injector.Inject(data, "LethalMenu", "Loader", "Init");
        }

        private async Task AutoInject()
        {
            LaunchLC();

            await Task.Delay((int) ((NumericUpDown)FindName("AutoInjectDelay")).Value * 1000);
            Inject();
            Injected = true;
            UpdateBindings();
            MessageBox.Show("Auto-Injected LethalMenu", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            Close();
        }
        private void LaunchLC()
        {
            if (LCRunning) return;

            if (Steam.FoundWithSteam) Process.Start(new ProcessStartInfo($"steam://rungameid/{Steam.LC_APPID}") { UseShellExecute = true });
            else Process.Start(Steam.LCExecutable);
        }
        private static Process? GetLethalCompanyProcess() => Process.GetProcesses().Where(p => p.ProcessName == "Lethal Company").FirstOrDefault();
    }

}

using LethalMenuAssistant.Properties;
using LMAssistantLib;
using System.IO;
using System.Windows;

namespace LethalMenuAssistant
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private Loading _loading;

        private void Application_Startup(object sender, StartupEventArgs e)
        {
            _loading = new Loading();
            _loading.Show();

         

            _ = OpenMainWindow();

        }

        private async Task OpenMainWindow()
        {
            await WaitForInitializationAsync();



            if (!Steam.HasLCPath() && !string.IsNullOrEmpty(Settings.Default.LCPath)) Steam.SetLCPath(Settings.Default.LCPath);

            MainWindow mainWindow = new MainWindow();
            mainWindow.Show();

            _loading.Close();
        }

        private Task WaitForInitializationAsync()
        {
            if (LMAssistant.Initialized)
            {
                return Task.CompletedTask;
            }

            var tcs = new TaskCompletionSource<object>();

            var checkTimer = new System.Timers.Timer(500);
            checkTimer.Elapsed += (sender, args) =>
            {
                if (LMAssistant.Initialized)
                {
                    checkTimer.Stop();
                    tcs.TrySetResult(null);
                }
            };

            checkTimer.Start();

            return tcs.Task;
        }
    }

}

using DarkByteLib;
using Newtonsoft.Json.Linq;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Windows;
using System.Windows.Media.Imaging;

namespace DarkByteLauncher
{
    public class DarkByte
    {
        private const string _releaseUrl = "https://icyrelic.com/release/";
        private const string gamesFile = "games.json";

        public static bool Initialized = false;
        public static List<Game> Games = new List<Game>();
        public static string LauncherHomeText = "";


        public static async Task Init()
        {
            await Task.Run(() =>
            {
                FetchGames();
                LauncherHomeText = FetchURL(_releaseUrl + "darkbytelauncher/home.txt");
                Initialized = true;
            });
        }
        
        private static string FetchURL(string url)
        {
            HttpClient client = new HttpClient();
            var data = client.GetStreamAsync(url).Result;

            StreamReader reader = new StreamReader(data);

            return reader.ReadToEnd();
        }

        private static void FetchGames()
        {
            Games.Clear();
            string rawJSON = FetchURL(_releaseUrl + gamesFile);
            JObject jsonObject = JObject.Parse(rawJSON);
            JArray gamesArray = (JArray)jsonObject["Games"];

            gamesArray.ToList().ForEach(x =>
            {
                Debug.WriteLine("Game: " + x.ToString());
                var game = x.ToObject<Game>();

                if (game != null)
                {
                    Games.Add(game);
                    game.Init();
                }
                    
            });
        }

        public static BitmapImage Base64ToImage(string base64String)
        {
            byte[] imageBytes = Convert.FromBase64String(base64String);
            using (MemoryStream ms = new MemoryStream(imageBytes))
            {
                BitmapImage image = new BitmapImage();
                image.BeginInit();
                image.CacheOption = BitmapCacheOption.OnLoad;
                image.StreamSource = ms;
                image.EndInit();
                image.Freeze();
                return image;
            }
        }


    }
}

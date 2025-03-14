using Gameloop.Vdf;
using Gameloop.Vdf.JsonConverter;
using Microsoft.Win32;
using Newtonsoft.Json.Linq;
using System.Diagnostics;

namespace DarkByteLib
{
    public class Steam
    {

        public static bool TryFindGame(Game game)
        {
            Debug.WriteLine("Trying to find game: " + game.GameName);
            string steam32 = "HKEY_LOCAL_MACHINE\\SOFTWARE\\VALVE\\Steam";
            string steam64 = "HKEY_LOCAL_MACHINE\\SOFTWARE\\Wow6432Node\\Valve\\Steam";

            var steamPath32 = Registry.GetValue(steam32, "InstallPath", null);
            var steamPath64 = Registry.GetValue(steam64, "InstallPath", null);

            bool found = false;

            if (steamPath32 != null) found = FindInstall(steamPath32.ToString(), game);
            if (steamPath64 != null) found = FindInstall(steamPath64.ToString(), game);

            return found;
        }

        private static bool FindInstall(string steampath, Game game)
        {
            var configpath = steampath + "/steamapps/libraryfolders.vdf";
            var config = VdfConvert.Deserialize(File.ReadAllText(configpath));
            var json = config.ToJson();
            var obj = json.First?.ToObject<JObject>();


            obj?.Values().ToList().ForEach(x =>
            {
                x["apps"]?.SelectTokens($"$.{game.GameID}").ToList().ForEach(y =>
                {
                    string folder = $"{x["path"]}\\steamapps\\common\\{game.GameName}";
                    string exe = $"{folder}\\{game.ProcessName}";

                    if (File.Exists(exe))
                    {
                        game.SteamInstallFolder = folder;
                        game.GameExe = exe;
                        game.FoundSteamInstall = true;
                    }
                });
            });
            return !string.IsNullOrEmpty(game.GameExe);
        }
    }
}

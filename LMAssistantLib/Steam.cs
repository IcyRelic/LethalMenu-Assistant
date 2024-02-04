using Gameloop.Vdf;
using Gameloop.Vdf.JsonConverter;
using Microsoft.Win32;
using Newtonsoft.Json.Linq;

namespace LMAssistantLib
{
    public class Steam
    {
        public const string LC_APPID = "1966720";
        public static string LCFolder = string.Empty;
        public static string LCExecutable = string.Empty;
        public static bool FoundWithSteam = false;


        public static bool TryFindLethalCompany()
        {
            string steam32 = "HKEY_LOCAL_MACHINE\\SOFTWARE\\VALVE\\Steam";
            string steam64 = "HKEY_LOCAL_MACHINE\\SOFTWARE\\Wow6432Node\\Valve\\Steam";

            var steamPath32 = Registry.GetValue(steam32, "InstallPath", null);
            var steamPath64 = Registry.GetValue(steam64, "InstallPath", null);

            bool found = false;

            if (steamPath32 != null) found = FindLCInstall(steamPath32.ToString());
            if (steamPath64 != null) found = FindLCInstall(steamPath64.ToString());

            return found;
        }

        private static bool FindLCInstall(string steampath)
        {
            var configpath = steampath + "/steamapps/libraryfolders.vdf";
            var config = VdfConvert.Deserialize(File.ReadAllText(configpath));

            var json = config.ToJson();

            var obj = json.First?.ToObject<JObject>();

   

            obj?.Values().ToList().ForEach(x =>
            {
                if (x["apps"]?[LC_APPID] != null)
                {
                    //Console.WriteLine("APP FOUND");

                    string folder = $"{x["path"]}\\steamapps\\common\\Lethal Company";
                    string exe = $"{folder}\\Lethal Company.exe";

                    if(File.Exists(exe)) {
                        LCFolder = folder;
                        LCExecutable = exe;
                        FoundWithSteam = true;
                    }

                }
            });

            return !LCExecutable.Equals(string.Empty);
        }

        public static bool HasLCPath()
        {
            return !string.IsNullOrEmpty(LCExecutable) && File.Exists(LCExecutable);
        }
        public static bool SetLCPath(string folder)
        {
            string exe = $"{folder}\\Lethal Company.exe";

            if(File.Exists(exe))
            {
                LCFolder = folder;
                LCExecutable = exe;
                return true;
            }

            return false;
        }
    }
}

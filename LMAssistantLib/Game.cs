using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;

namespace DarkByteLib
{
    public class Game
    {
        private readonly string _baseUrl = "https://icyrelic.com/release/";
        public string ProcessName { get; private set; }
        public string GameName { get; set; }
        public int GameID { get; private set; }
        public List<string> Versions { get; private set; }

        [JsonProperty("ChangeURL")]
        public string ChangeURL { get; private set; } = "";

        [JsonProperty("GitHubURL")]
        public string GitHubURL { get; private set; } = "";

        [JsonProperty("UnknownCheatsURL")]
        public string UnknownCheatsURL { get; private set; } = "";

        [JsonProperty("DownloadSettings")]
        public DownloadSettings DownloadSettings { get; private set; }

        [JsonProperty("InjectionSettings")]
        public InjectionSettings InjectionSettings { get; private set; }

        [JsonIgnore]
        public string IconBase64 { get; private set; }

        [JsonIgnore]
        public string SteamInstallFolder { get; set; }
        [JsonIgnore]
        public string GameExe { get; set; }
        [JsonIgnore]
        public bool FoundSteamInstall { get; set; }

        [JsonIgnore]
        public List<string> ChangeLogList = new List<string>();

        [JsonIgnore]
        public bool HasExecutable => !string.IsNullOrEmpty(GameExe) && File.Exists(GameExe);

        [JsonIgnore]
        public string Status => IsRunning ? "Running" : "Not Running";
        [JsonIgnore]
        public bool IsRunning => GetProcess() != null;

        [JsonIgnore]
        public string ChangeLog => String.Join("\n", ChangeLogList);
        public Game(string processName, string gameName, int gameId, List<string> versions, string dllFileName, string releaseFolder, string injectMethod, string injectClass, string injectNamespace, string injectTrigger)
        {
            ProcessName = processName;
            GameName = gameName;
            GameID = gameId;
            Versions = versions;
            DownloadSettings = new DownloadSettings { DLLFileName = dllFileName, ReleaseFolder = releaseFolder };
            InjectionSettings = new InjectionSettings { Method = injectMethod, Class = injectClass, Namespace = injectNamespace, AutoInjectTrigger = injectTrigger };
        }

        public void Init()
        {
            HttpClient client = new HttpClient();
            string iconURL = _baseUrl + DownloadSettings.ReleaseFolder + "/icon.png";
            
            //log to console
            Debug.WriteLine("Fetching icon from: " + iconURL);

            byte[] iconBytes = client.GetByteArrayAsync(iconURL).Result;
            IconBase64 = Convert.ToBase64String(iconBytes);

            Steam.TryFindGame(this);

            ChangeLogList.Clear();
            Debug.WriteLine($"Fetching change log from: {ChangeURL}" + (string.IsNullOrEmpty(ChangeURL) ? "No change log URL provided." : ""));
            if(!string.IsNullOrEmpty(ChangeURL))
            {
                var changes = client.GetStreamAsync(ChangeURL).Result;

                var reader = new StreamReader(changes);
                while (!reader.EndOfStream)
                {
                    Debug.WriteLine("Reading change log line: " + reader.ReadLine());
                    ChangeLogList.Add(reader.ReadLine());
                }
            }

            Debug.WriteLine($"Game: {GameName} | Found Steam Install: {FoundSteamInstall} | Game Exe: {GameExe}");
        }

        public string GetDownloadUrl(string version = "")
        {
            if (string.IsNullOrEmpty(version) || !IsValidVersion(version))
                return _baseUrl + DownloadSettings.ReleaseFolder + "/" + DownloadSettings.DLLFileName + ".dll";

            return _baseUrl + DownloadSettings.ReleaseFolder + "/" + DownloadSettings.DLLFileName + version.Replace(".", "_") + ".dll";
        }

        

        public bool SetExecutable(string folder)
        {
            GameExe = $"{folder}\\{ProcessName}.exe";
            FoundSteamInstall = File.Exists(GameExe);

            return FoundSteamInstall;
        }

        private bool IsValidVersion(string version)
        {
            return Versions.Contains(version);
        }

        public Process? GetProcess() => Process.GetProcesses().Where(p => p.ProcessName == GameName).FirstOrDefault();
        
        
        public string ToJson()
        {
            return JsonConvert.SerializeObject(this, Formatting.Indented);
        }

        public static Game FromJson(string json)
        {
            return JsonConvert.DeserializeObject<Game>(json);
        }
    }

    public class DownloadSettings
    {
        [JsonProperty("DLLFileName")]
        public string DLLFileName { get; set; }

        [JsonProperty("ReleaseFolder")]
        public string ReleaseFolder { get; set; }
    }

    public class InjectionSettings
    {
        [JsonProperty("Method")]
        public string Method { get; set; }

        [JsonProperty("Class")]
        public string Class { get; set; }

        [JsonProperty("Namespace")]
        public string Namespace { get; set; }

        [JsonProperty("AutoInjectTrigger")]
        public string AutoInjectTrigger { get; set; }
    }
}

using LMAssistantLib;
using Newtonsoft.Json.Linq;
using System.IO;
using System.Net.Http;

namespace LethalMenuAssistant
{
    public class LMAssistant
    {
        private const string _changelogUrl = "https://raw.githubusercontent.com/IcyRelic/LethalMenu/master/LethalMenu/Resources/Changelog.txt";
        private const string _versionUrl = "https://icyrelic.com/release/lethalmenu/version.json";
        public static List<string> versions = new List<string>();
        public static List<string> changelog = new List<string>();
        public static bool Initialized = false;


        public static async Task Init()
        {
            await Task.Run(() =>
            {
                FetchVersions();
                FetchChangelog();
                Steam.TryFindLethalCompany();
                Initialized = true;
            });
        }

        private static void FetchVersions()
        {
            versions.Clear();
            HttpClient client = new HttpClient();

            var data = client.GetStreamAsync(_versionUrl).Result;

            StreamReader reader = new StreamReader(data);

            string json = reader.ReadToEnd();

            JArray tokens = JArray.Parse(json);

            tokens.ToList().ForEach(t => versions.Add(t.ToString()));
        }

        private static void FetchChangelog()
        {
            changelog.Clear();
            HttpClient client = new HttpClient();

            var data = client.GetStreamAsync(_changelogUrl).Result;

            StreamReader reader = new StreamReader(data);

            List<string> changes = new List<string>();
            while (!reader.EndOfStream)
            {
                changes.Add(reader.ReadLine());
            }

            changelog.AddRange(changes);
        }
    }
}

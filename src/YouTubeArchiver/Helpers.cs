using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Web;
using Common;
using Common.Models;
using Newtonsoft.Json;
using Serilog;

namespace YouTubeArchiver
{
    public static class Helpers
    {
        public static Dictionary<string, string> GetScrapeHeaders()
        {
            var youtubeScrapeFile = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Personal), ".youtube-archiver-scrapping.json");

            if (!File.Exists(youtubeScrapeFile))
            {
                Log.Error("No auth found for scrapping, run \"youtube-archiver auth scrapping\".");
                Environment.Exit(1);
            }

            return JsonConvert.DeserializeObject<Dictionary<string, string>>(File.ReadAllText(youtubeScrapeFile));
        }

        public static string GetApiKey()
        {
            var youtubeAuthFile = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Personal), ".youtube-archiver-auth.json");

            if (!File.Exists(youtubeAuthFile))
            {
                Log.Error("No token found for requests, run \"youtube-archiver auth key\".");
                Environment.Exit(1);
            }

            var key = File.ReadAllText(youtubeAuthFile).TrimEnd(Environment.NewLine.ToCharArray());
            
            if (string.IsNullOrEmpty(key))
            {
                Log.Logger.Error("Invalid API key.");
                Environment.Exit(1);
            }

            return key;
        }
        
        public static string GetIndexDirectory(string indexDirectory)
        {
            if (string.IsNullOrEmpty(indexDirectory))
            {
                indexDirectory = Directory.GetCurrentDirectory();
            }
            indexDirectory = Path.GetFullPath(indexDirectory);
            
            Log.Logger.Information("Index directory: {directory}", indexDirectory);

            return indexDirectory;
        }
        
        public static IndexWorkspace GetWorkspace(string indexDirectory)
        {
            var index = IndexWorkspace.Create(GetIndexDirectory(indexDirectory));
            
            if (index == null)
            {
                Log.Logger.Error("The index.json file doesn't exist. Run \"index\" first.");
                Environment.Exit(1);
            }

            return index;
        }

        public static string GetRequestBody(string url)
        {
            using (var client = new HttpClient())
            {
                foreach (var header in Helpers.GetScrapeHeaders())
                {
                    client.DefaultRequestHeaders.Add(header.Key, header.Value);
                }
                
                var response = client.GetAsync(url).GetAwaiter().GetResult();
                response.EnsureSuccessStatusCode();
                return response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
            }
        }
        
        public static bool ParseCurlCommand(string curlCommand, out Dictionary<string, string> headers)
        {
            if (string.IsNullOrEmpty(curlCommand))
            {
                headers = null;
                return false;
            }
            
            headers = new Dictionary<string, string>();
            var currentIndex = curlCommand.IndexOf("-H", StringComparison.Ordinal);

            while (currentIndex != -1)
            {
                var openingIndex = curlCommand.IndexOf("'", currentIndex, StringComparison.Ordinal);
                var closingIndex = curlCommand.IndexOf("'", openingIndex + 1, StringComparison.Ordinal);

                var header = curlCommand.Substring(openingIndex + 1, closingIndex - openingIndex - 1);
                
                headers.Add(header.Substring(0, header.IndexOf(":", StringComparison.Ordinal)).Trim(), header.Substring(header.IndexOf(":", StringComparison.Ordinal) + 1).Trim());

                currentIndex = curlCommand.IndexOf("-H", closingIndex, StringComparison.Ordinal);
            }

            return true;
        }

        public static GetVideoPlayerObject GetVideoPlayerInfoForYouTubeVideo(string videoId)
        {
            var getVideoResponse =
                GetRequestBody($"https://www.youtube.com/get_video_info?html5=1&video_id={videoId}");

            var keys = getVideoResponse.Split("&").Select(x =>
            {
                var split = x.Split("=");
                return new Tuple<string, string>(split[0], HttpUtility.UrlDecode(split[1]));
            }).ToDictionary(x => x.Item1, x => x.Item2);

            return JsonConvert.DeserializeObject<GetVideoPlayerObject>(keys["player_response"]);
        }
    }
}
using System;
using System.CommandLine;
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

        public static Option BuildIndexOption()
        {
            var indexOption = new Option(new []{"-i", "--index-directory"}, "The directory where the index exists.")
            {
                Name = "index-directory",
                Argument = new Argument<string>
                {
                    Arity = ArgumentArity.ExactlyOne
                }
            };
            indexOption.Argument.SetDefaultValue(Directory.GetCurrentDirectory());
            return indexOption;
        }
        
        public static string GetRequestBody(string url)
        {
            using (var client = new HttpClient())
            {
                var response = client.GetAsync(url).GetAwaiter().GetResult();
                response.EnsureSuccessStatusCode();
                return response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
            }
        }

        public static GetVideoPlayerObject GetVideoPlayerInfoForYouTubeVideo(string videoId)
        {
            var getVideoResponse =
                Helpers.GetRequestBody($"https://www.youtube.com/get_video_info?html5=1&video_id={videoId}");

            var keys = getVideoResponse.Split("&").Select(x =>
            {
                var split = x.Split("=");
                return new Tuple<string, string>(split[0], HttpUtility.UrlDecode(split[1]));
            }).ToDictionary(x => x.Item1, x => x.Item2);

            return JsonConvert.DeserializeObject<GetVideoPlayerObject>(keys["player_response"]);
        }
    }
}
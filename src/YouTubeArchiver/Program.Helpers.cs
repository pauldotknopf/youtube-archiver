using System;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using CommandLine;
using Common;
using Common.Models;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Google.Apis.YouTube.v3;
using Newtonsoft.Json;
using Serilog;

namespace YouTubeArchiver
{
    partial class Program
    {
        class BaseIndexOptions
        {
            [Option('i', "index-directory")]
            public string IndexDirectory { get; set; }

            public void Init()
            {
                if (string.IsNullOrEmpty(IndexDirectory))
                {
                    IndexDirectory = ".";
                }

                IndexDirectory = Path.GetFullPath(IndexDirectory);
                Log.Logger.Information("Index directory: {directory}...", IndexDirectory);
            }

            public IndexWorkspace GetWorkspace()
            {
                var index = IndexWorkspace.Create(IndexDirectory);
                
                if (index == null)
                {
                    Log.Logger.Error("The index.json file doesn't exist. Run \"index\" first.");
                    Environment.Exit(1);
                }

                return index;
            }
        }
        
        private static string GetRequestBody(string url)
        {
            using (var client = new HttpClient())
            {
                var response = client.GetAsync(url).GetAwaiter().GetResult();
                response.EnsureSuccessStatusCode();
                return response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
            }
        }
        
        private static ClientSecrets GetSecrets()
        {
            var youtubeDumpAuthFile = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Personal), ".youtube-dump-auth.json");
            if (!File.Exists(youtubeDumpAuthFile))
            {
                Log.Logger.Error("You must run \"auth\" first.");
                Environment.Exit(1);
            }

            var secrets = JsonConvert.DeserializeObject<ClientSecrets>(File.ReadAllText(youtubeDumpAuthFile));

            if (string.IsNullOrEmpty(secrets.ClientId) || string.IsNullOrEmpty(secrets.ClientSecret))
            {
                Log.Logger.Error("Invalid client id/secret.");
                Environment.Exit(1);
            }
            
            return secrets;
        }

        private static async Task<YouTubeService> GetYouTubeService()
        {
            var secrets = GetSecrets();
            
            var credential = await GoogleWebAuthorizationBroker.AuthorizeAsync(
                secrets,
                new[] { YouTubeService.Scope.YoutubeForceSsl },
                "user",
                CancellationToken.None
            );
            
            return new YouTubeService(new BaseClientService.Initializer
            {
                HttpClientInitializer = credential,
                ApplicationName = "youtube-dump"
            });
        }
    }
}
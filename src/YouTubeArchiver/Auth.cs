using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.IO;
using System.Text;
using Google.Apis.Auth.OAuth2;
using Newtonsoft.Json;
using Serilog;

namespace YouTubeArchiver
{
    public class Auth
    {
        public static Command Create()
        {
            var command = new Command("auth")
            {
                new Command("oauth")
                {
                    Handler = CommandHandler.Create(typeof(Auth).GetMethod(nameof(OAuth)))
                },
                new Command("scraping")
                {
                    Handler = CommandHandler.Create(typeof(Auth).GetMethod(nameof(Scrapping)))
                }
            };

            return command;
        }
        
        // ReSharper disable once MemberCanBePrivate.Local
        public static void OAuth()
        {
            var clientId = ReadLine.Read("Client id:");

            if (string.IsNullOrEmpty(clientId))
            {
                Log.Logger.Error("You must provide a client id.");
                Environment.Exit(1);
            }
        
            var clientSecret = ReadLine.Read("Client secret:");

            if (string.IsNullOrEmpty(clientSecret))
            {
                Log.Logger.Error("You must provide a client secret.");
                Environment.Exit(1);
            }
        
            var youtubeAuthFile = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Personal), ".youtube-archiver-auth.json");

            if (File.Exists(youtubeAuthFile))
            {
                File.Delete(youtubeAuthFile);
            }
        
            File.WriteAllText(youtubeAuthFile, JsonConvert.SerializeObject(new ClientSecrets
            {
                ClientId = clientId,
                ClientSecret = clientSecret
            }, Formatting.Indented));
        
            Log.Logger.Information("Saved!");
        }

        public static void Scrapping()
        {
            var steps = new StringBuilder();
            steps.AppendLine("Steps to authenticate:");

            steps.AppendLine("Step 1: With Chrome, authenticate with YouTube and then navigate to: https://youtube.com/");
            steps.AppendLine("Step 2: Login to your account.");
            steps.AppendLine("Step 3: Open the Network tab on the developer tools");
            steps.AppendLine("Step 4: Filter requests for \"browse_ajax\"");
            steps.AppendLine("Step 5: Navigate to a YouTube channel and scroll the videos until you see a network request with \"browse_ajax\".");
            steps.AppendLine("Step 6: Right click the request and click \"Copy -> Copy as cURL\"");
            steps.Append("Step 7: Paste the contents of your clipboard below");
            Console.WriteLine(steps);
            
            Console.Out.Write("Result: ");
            var curlCommand = Console.In.ReadLine();

            Dictionary<string, string> headers;
            while (!Helpers.ParseCurlCommand(curlCommand, out headers))
            {
                Console.WriteLine();
                Console.Write("Invalid fetch command, try again:");
                curlCommand = Console.In.ReadLine();
            }

            var youtubeScrapeFile = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Personal), ".youtube-archiver-scrapping.json");

            if (File.Exists(youtubeScrapeFile))
            {
                File.Delete(youtubeScrapeFile);
            }
            
            File.WriteAllText(youtubeScrapeFile, JsonConvert.SerializeObject(headers, Formatting.Indented));
            
            Console.WriteLine("Saved!");
        }
    }
}
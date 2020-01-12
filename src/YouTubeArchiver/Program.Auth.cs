using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.IO;
using Google.Apis.Auth.OAuth2;
using Newtonsoft.Json;
using Serilog;

namespace YouTubeArchiver
{
    partial class Program
    {
        class AuthCommand
        {
            public static Command Create()
            {
                var command = new Command("auth")
                {
                    Handler = CommandHandler.Create(typeof(AuthCommand).GetMethod(nameof(Run)))
                };

                return command;
            }
            
            // ReSharper disable once MemberCanBePrivate.Local
            public static void Run()
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
            
                var youtubeAuthFile = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Personal), ".youtube-dump-auth.json");

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
        }
    }
}
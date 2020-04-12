using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Common;
using Octokit;
using Serilog;

namespace YouTubeArchiver.Index
{
    public class UploadVideos
    {
        public static Command Create()
        {
            var githubRelease = new Command("github-release")
            {
                new Option("--username")
                {
                    Argument = new Argument<string>
                    {
                        Arity = ArgumentArity.ExactlyOne
                    }
                },
                new Option("--password")
                {
                    Argument = new Argument<string>
                    {
                        Arity = ArgumentArity.ExactlyOne
                    }
                },
                new Option("--api-token")
                {
                    Argument = new Argument<string>
                    {
                        Arity = ArgumentArity.ExactlyOne
                    }
                },
                new Option("--owner")
                {
                    Argument = new Argument<string>
                    {
                        Arity = ArgumentArity.ExactlyOne
                    }
                },
                new Option("--repository")
                {
                    Argument = new Argument<string>
                    {
                        Arity = ArgumentArity.ExactlyOne
                    }
                },
                new Option("--tag")
                {
                    Argument = new Argument<string>
                    {
                        Arity = ArgumentArity.ExactlyOne
                    }
                }
            };
            
            githubRelease.Handler = CommandHandler.Create(typeof(UploadVideos).GetMethod(nameof(UploadGitHub)));
            
            var command = new Command("upload-videos")
            {
                githubRelease
            };
            
            return command;
        }

        public static async Task UploadGitHub(string indexDirectory,
            string username,
            string password,
            string apiToken,
            string owner,
            string repository,
            string tag)
        {
            var workspace = Helpers.GetWorkspace(indexDirectory);

            if (string.IsNullOrEmpty(username))
            {
                username = Environment.GetEnvironmentVariable("GITHUB_USERNAME");
            }

            if (string.IsNullOrEmpty(password))
            {
                password = Environment.GetEnvironmentVariable("GITHUB_PASSWORD");
            }

            if (string.IsNullOrEmpty(apiToken))
            {
                apiToken = Environment.GetEnvironmentVariable("GITHUB_API_TOKEN");
            }

            if (string.IsNullOrEmpty(apiToken) && string.IsNullOrEmpty(username) && string.IsNullOrEmpty(password))
            {
                Log.Error("You must provide a either an api token or username/password paid.");
                Environment.Exit(1);
            }

            if (string.IsNullOrEmpty(apiToken))
            {
                if (string.IsNullOrEmpty(username))
                {
                    Log.Error("You must provide a user name.");
                    Environment.Exit(1);
                }

                if (string.IsNullOrEmpty(password))
                {
                    Log.Error("You must provide a password.");
                    Environment.Exit(1);
                }
            }

            var client = new GitHubClient(new ProductHeaderValue("youtube-archiver"));
            client.SetRequestTimeout(TimeSpan.FromMinutes(60));
            if (!string.IsNullOrEmpty(apiToken))
            {
                client.Credentials = new Credentials(apiToken);
            }
            else
            {
                client.Credentials = new Credentials(username, password);
            }

            var release = await client.Repository.Release.Get(owner, repository, tag);
            if (release == null)
            {
                Log.Error("The release doesn't exist.");
                Environment.Exit(1);
            }

            foreach (var video in workspace.GetVideos())
            {
                var videoPath = workspace.GetVideoPath(video.Id);

                if (videoPath is VideoPathLocal videoPathLocal)
                {
                    var asset = release.Assets.SingleOrDefault(x => x.Name == $"{video.Id}.mp4");
                    if (asset != null)
                    {
                        // It exists remotely, but we don't know about it locally.
                        Log.Warning("The video {videoId} exists remotely, but local didn't know, updating local...", video.Id);
                        workspace.UpdateVideoPath(video.Id, VideoPath.External(asset.BrowserDownloadUrl));
                        continue;
                    }
                    
                    Log.Information("Uploading {video}...", video.Id);
                    
                    await using (var stream = File.OpenRead(videoPathLocal.Path))
                    {
                        asset = await client.Repository.Release.UploadAsset(release,
                            new ReleaseAssetUpload($"{video.Id}.mp4", "video/mp4", stream,
                                TimeSpan.FromMinutes(60)));
                    }

                    Log.Information("Uploaded video, updating local index.");
                    
                    workspace.UpdateVideoPath(video.Id, VideoPath.External(asset.BrowserDownloadUrl));
                }
            }
            
            Log.Information("Done!");
        }
    }
}
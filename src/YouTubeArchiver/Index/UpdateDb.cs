using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Common;
using Common.Models;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Google.Apis.YouTube.v3;
using Google.Apis.YouTube.v3.Data;
using Newtonsoft.Json;
using Serilog;

namespace YouTubeArchiver.Index
{
    public class UpdateDb
    {
        public static Command Create()
        {
            var command = new Command("update-db")
            {
                new Option(new []{"--channel-id", "-c"})
                {
                    Argument = new Argument<string>()
                }
            };

            command.Handler = CommandHandler.Create(typeof(UpdateDb).GetMethod(nameof(Run)));

            return command;
        }
        
        public static async Task Run(string indexDirectory, string channelId)
        {
            indexDirectory = Helpers.GetIndexDirectory(indexDirectory);

            if (string.IsNullOrEmpty(channelId))
            {
                // Maybe we are updating an existing index?
                var index = IndexWorkspace.Create(indexDirectory);
                if (index != null)
                {
                    channelId = index.Index.Channel.Id;
                }
            }

            if (string.IsNullOrEmpty(channelId))
            {
                Log.Error("You must provide a channel id.");
                Environment.Exit(1);
            }
            
            var youtubeService = await GetYouTubeService();

            Log.Logger.Information("Getting channel info for {channelId}...", channelId);
            var channel = await GetChannel(channelId, youtubeService);

            Log.Logger.Information("Getting uploaded videos for channel {channel}...", channel.Title);
            var videos = await GetVideos(channel, youtubeService);

            Log.Logger.Information("Saving channel info...");
            var indexFile = Path.Combine(indexDirectory, "index.json");

            if (File.Exists(indexFile))
            {
                File.Delete(indexFile);
            }
            
            await File.WriteAllTextAsync(indexFile, JsonConvert.SerializeObject(new ChannelIndex
            {
                Channel = channel,
            }, Formatting.Indented));
            
            Log.Information("Saving videos...");

            var videosDirectory = Path.Combine(indexDirectory, "videos");
            if (!Directory.Exists(videosDirectory))
            {
                Directory.CreateDirectory(videosDirectory);
            }
            
            foreach (var video in videos)
            {
                Log.Information("Saving {video}...", video.Title);
                var videoFile = Path.Combine(videosDirectory, $"{video.Id}.json");
                if (File.Exists(videoFile))
                {
                    File.Delete(videoFile);
                }
                await File.WriteAllTextAsync(videoFile, JsonConvert.SerializeObject(video, Formatting.Indented));
            }
            
            // TODO: Check if videos are deleted...
        }
        
        private static async Task<Common.Models.Channel> GetChannel(string channelId, YouTubeService youTubeService)
        {
            var channelRequest = youTubeService.Channels.List("contentDetails,snippet");
            channelRequest.Id = channelId;

            var channelResponse = (await channelRequest.ExecuteAsync());

            if (channelResponse.Items == null || channelResponse.Items.Count != 1)
            {
                Log.Logger.Error("Couldn't get channel by the given id.");
                Environment.Exit(1);
            }

            var channel = channelResponse.Items.Single();

            return new Common.Models.Channel
            {
                Id = channel.Id,
                Title = channel.Snippet.Title,
                UploadPlaylistId = channel.ContentDetails.RelatedPlaylists.Uploads
            };
        }

        private static async Task<List<Common.Models.Video>> GetVideos(Common.Models.Channel channel, YouTubeService youTubeService)
        {
            var videosRequest = youTubeService.PlaylistItems.List("snippet,contentDetails");
            videosRequest.PlaylistId = channel.UploadPlaylistId;
            videosRequest.MaxResults = 50;

            var videos = new List<PlaylistItem>();
            var videosResponse = await videosRequest.ExecuteAsync();

            while (videosResponse.Items.Count > 0)
            {
                videos.AddRange(videosResponse.Items);

                if (!string.IsNullOrEmpty(videosResponse.NextPageToken))
                {
                    videosRequest.PageToken = videosResponse.NextPageToken;
                    videosResponse = await videosRequest.ExecuteAsync();
                }
                else
                {
                    videosResponse.Items.Clear();
                }
            }

            return videos.Select(x => new Common.Models.Video
            {
                Id = x.ContentDetails.VideoId,
                Title = x.Snippet.Title,
                UploadedOn = x.Snippet.PublishedAt.HasValue ? new DateTimeOffset(x.Snippet.PublishedAt.Value) : (DateTimeOffset?)null
            }).ToList();
        }
        
        private static async Task<YouTubeService> GetYouTubeService()
        {
            return new YouTubeService(new BaseClientService.Initializer
            {
                ApplicationName = "youtube-archiver",
                ApiKey = Helpers.GetApiKey()
            });
        }
    }
}
using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web;
using Common;
using Common.Models;
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
            
            Log.Logger.Information("Getting channel info for {channelId}...", channelId);
            var channel = await GetChannel(channelId);

            Log.Logger.Information("Getting uploaded videos for channel {channel}...", channel.Title);
            var videos = await GetVideos(channel.UploadPlaylistId);

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

            Log.Information("Discovering existing videos...");
            var existingVideos = IndexWorkspace.Create(indexDirectory).GetVideos();
            
            Log.Information("Saving videos...");

            var videosDirectory = Path.Combine(indexDirectory, "videos");
            if (!Directory.Exists(videosDirectory))
            {
                Directory.CreateDirectory(videosDirectory);
            }
            
            foreach (var video in videos)
            {
                if (existingVideos.Contains(video))
                {
                    continue;
                }
                Log.Information("Saving {video}...", video.Title);
                var videoFile = Path.Combine(videosDirectory, $"{video.Id}.json");
                if (File.Exists(videoFile))
                {
                    File.Delete(videoFile);
                }
                await File.WriteAllTextAsync(videoFile, JsonConvert.SerializeObject(video, Formatting.Indented));
            }
            
            // TODO: Check if videos are deleted...
            
            Log.Information("Done!");
        }
        
        private static async Task<Channel> GetChannel(string channelId)
        {
            var builder = new UriBuilder("https://www.googleapis.com/youtube/v3/channels");

            var query = HttpUtility.ParseQueryString(builder.Query);
            query["part"] = "contentDetails,snippet";
            query["id"] = channelId;
            query["key"] = Helpers.GetApiKey();

            builder.Query = query.ToString();
            
            var httpClient = new HttpClient();
            var responseMessage = await httpClient.GetAsync(builder.ToString());
            
            if (responseMessage.StatusCode != HttpStatusCode.OK)
            {
                Log.Error("Status code: {statusCode}: error: {error}", responseMessage.StatusCode, await responseMessage.Content.ReadAsStringAsync());
                Environment.Exit(1);
            }

            var channelResponse = JsonConvert.DeserializeObject<ChannelsResponse>(await responseMessage.Content.ReadAsStringAsync());
            
            if (channelResponse.Items == null || channelResponse.Items.Count != 1)
            {
                Log.Logger.Error("Couldn't get channel by the given id.");
                Environment.Exit(1);
            }

            var channel = channelResponse.Items.Single();

            return new Channel
            {
                Id = channel.Id,
                Title = channel.Snippet.Title,
                UploadPlaylistId = channel.ContentDetails.RelatedPlaylists.Uploads
            };
        }

        private static async Task<List<Video>> GetVideos(string playlistId)
        {
            var videos = new List<Video>();
            
            var playlistItems = await GetPlaylistItems(playlistId, null);

            while (playlistItems.Items.Count > 0)
            {
                videos.AddRange(playlistItems.Items.Select(x => new Video
                {
                    Id = x.ContentDetails.VideoId,
                    Title = x.Snippet.Title,
                    UploadedOn = x.ContentDetails.VideoPublishedAt
                }));

                if (!string.IsNullOrEmpty(playlistItems.NextPageToken))
                {
                    playlistItems = await GetPlaylistItems(playlistId, playlistItems.NextPageToken);
                }
                else
                {
                    playlistItems.Items.Clear();
                }
            }

            return videos;
        }
        
        private static async Task<PlaylistItemsResponse> GetPlaylistItems(string playlistId, string pageToken)
        {
            var builder = new UriBuilder("https://www.googleapis.com/youtube/v3/playlistItems");
            
            var query = HttpUtility.ParseQueryString(builder.Query);
            query["part"] = "contentDetails,snippet";
            query["maxResults"] = "50";
            query["playlistId"] = playlistId;
            query["key"] = Helpers.GetApiKey();
            if (!string.IsNullOrEmpty(pageToken))
            {
                query["pageToken"] = pageToken;
            }

            builder.Query = query.ToString();
            
            var httpClient = new HttpClient();
            var responseMessage = await httpClient.GetAsync(builder.ToString());

            if (responseMessage.StatusCode != HttpStatusCode.OK)
            {
                Log.Error("Status code: {statusCode}: error: {error}", responseMessage.StatusCode, await responseMessage.Content.ReadAsStringAsync());
                Environment.Exit(1);
            }

            return JsonConvert.DeserializeObject<PlaylistItemsResponse>(await responseMessage.Content.ReadAsStringAsync());
        }

        private class PlaylistItemsResponse
        {
            public string NextPageToken { get; set; }
            
            [JsonProperty("items")]
            public List<PlaylistItemResponse> Items { get; set; }
            
            public class PlaylistItemResponse
            {
                [JsonProperty("snippet")]
                public SnippetResponse Snippet { get; set; }
                
                public class SnippetResponse
                {
                    [JsonProperty("title")]
                    public string Title { get; set; }
                }

                [JsonProperty("contentDetails")]
                public ContentDetailsResponse ContentDetails { get; set; }
                
                public class ContentDetailsResponse
                {
                    [JsonProperty("videoId")]
                    public string VideoId { get; set; }
                    
                    [JsonProperty("videoPublishedAt")]
                    public DateTimeOffset? VideoPublishedAt { get; set; }
                }
            }
        }
        
        private class ChannelsResponse
        {
            [JsonProperty("items")]
            public List<ChannelResponse> Items { get; set; }
            
            public class ChannelResponse
            {
                [JsonProperty("id")]
                public string Id { get; set; }
                
                [JsonProperty("snippet")]
                public SnippetResponse Snippet { get; set; }
                
                public class SnippetResponse
                {
                    [JsonProperty("title")]
                    public string Title { get; set; }
                }
                
                [JsonProperty("contentDetails")]
                public ContentDetailsResponse ContentDetails { get; set; }

                public class ContentDetailsResponse
                {
                    [JsonProperty("relatedPlaylists")]
                    public RelatedPlaylistsResponse RelatedPlaylists { get; set; }
                    
                    public class RelatedPlaylistsResponse
                    {
                        [JsonProperty("uploads")]
                        public string Uploads { get; set; }
                    }
                }
            }
        }
    }
}    
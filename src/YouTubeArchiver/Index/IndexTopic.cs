using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Common.Models;
using Newtonsoft.Json;
using Serilog;

namespace YouTubeArchiver.Index
{
    public class IndexTopic
    {
        public static Command Create()
        {
            var command = new Command("index-topic")
            {
                Helpers.BuildIndexOption(),
                new Option(new[]{"-q", "--query"})
                {
                    Required = true,
                    Argument = new Argument<string>()
                }
            };

            command.Handler = CommandHandler.Create(typeof(IndexTopic).GetMethod(nameof(Run)));

            return command;
        }

        public static void Run(string indexDirectory, string query)
        {
            var workspace = Helpers.GetWorkspace(indexDirectory);
            
            Log.Logger.Information("Discovering captions...");
            workspace.DiscoverCaptions();

            if (workspace.CaptionsFiles.Count == 0)
            {
                Log.Logger.Information("No captions are present.");
                return;
            }
            
            Log.Logger.Information("Discovering current topics...");
            workspace.DiscoverTopics();
            
            var result = new TopicSearch();
            result.Topic = query;
            result.Results = new List<TopicSearch.VideoResult>();
            
            Log.Logger.Information("Searching captions for {count} videos...", workspace.CaptionsFiles.Count);

            var index = 0;
            foreach (var caption in workspace.CaptionsFiles)
            {
                index++;
                Log.Logger.Information("Searching video {current} of {total}...", index, workspace.CaptionsFiles.Count);

                var videoId = caption.Key;
                var captionFile = caption.Value;
                
                var captions = JsonConvert.DeserializeObject<List<Caption>>(File.ReadAllText(captionFile));

                var segments = SearchEngine.Search(captions, query);

                if (segments.Count == 0)
                {
                    continue;
                }
                
                result.Results.Add(new TopicSearch.VideoResult
                {
                    Id = videoId,
                    Segments = segments.Select(x => new TopicSearch.VideoResult.Segment
                    {
                        Text = x.Text,
                        Location = x.Location
                    }).ToList()
                });
            }
            
            workspace.SaveTopic(query, path =>
            {
                if (File.Exists(path))
                {
                    File.Delete(path);
                }
                
                File.WriteAllText(path, JsonConvert.SerializeObject(result, Formatting.Indented));
            });
        }
    }
}
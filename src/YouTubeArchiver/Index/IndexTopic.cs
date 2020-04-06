using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
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
                new Option(new[]{"-q", "--query"})
                {
                    Required = true,
                    Argument = new Argument<string>()
                    {
                        Arity = ArgumentArity.OneOrMore
                    }
                }
            };

            command.Handler = CommandHandler.Create(typeof(IndexTopic).GetMethod(nameof(Run)));

            return command;
        }

        public static void Run(string indexDirectory, List<string> query)
        {
            var workspace = Helpers.GetWorkspace(indexDirectory);

            var queryFull = string.Join(" ", query).Trim();
            if (string.IsNullOrEmpty(queryFull))
            {
                Log.Error("No query given.");
                Environment.Exit(1);
            }
            
            Log.Logger.Information("Indexing topic {topic}...", queryFull);
            
            Log.Logger.Information("Discovering captions...");
            var captionEntries = workspace.GetCaptions();

            if (captionEntries.Count == 0)
            {
                Log.Logger.Information("No captions are present.");
                return;
            }
            
            Log.Logger.Information("Discovering current topics...");

            string topic;
            List<string> aliases;
            ParseTopic(queryFull, out topic, out aliases);
            
            var result = new TopicSearch();
            result.Topic = topic;
            result.Aliases = aliases;
            result.Results = new List<TopicSearch.VideoResult>();
            
            Log.Logger.Information("Searching captions for {count} videos...", captionEntries.Count);

            foreach (var captions in captionEntries)
            {
                var segments = SearchEngine.Search(captions.Value, aliases ?? new List<string>{topic});

                if (segments.Count == 0)
                {
                    continue;
                }
                
                result.Results.Add(new TopicSearch.VideoResult
                {
                    Id = captions.Key,
                    Segments = segments.Select(x => new TopicSearch.VideoResult.Segment
                    {
                        Text = x.Text,
                        Location = x.Location
                    }).ToList()
                });
            }

            workspace.SaveTopic(result);
            
            Log.Information("Done!");
        }

        private static void ParseTopic(string topic, out string name, out List<string> aliases)
        {
            var match = Regex.Match(topic, @"(.*)\((.*)\)");
            if (match.Success)
            {
                name = match.Groups[1].Value;
                aliases = match.Groups[2].Value.Split(",").Select(x => x.Trim()).ToList();
            }
            else
            {
                name = topic;
                aliases = new List<string>();
            }

            if (aliases.Count == 0)
            {
                aliases = null;
            }
        }
    }
}
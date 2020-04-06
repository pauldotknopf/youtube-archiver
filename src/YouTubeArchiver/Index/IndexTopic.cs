using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Common;
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
                    Name = "query",
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

            if (query == null || query.Count == 0 || query.Any(string.IsNullOrEmpty))
            {
                Log.Error("No query given.");
                Environment.Exit(1);
            }
            
            Log.Logger.Information("Indexing topic(s) {@topic}...", query);
            
            Log.Logger.Information("Discovering captions...");
            var captionEntries = workspace.GetCaptions();
            
            if (captionEntries.Count == 0)
            {
                Log.Logger.Information("No captions are present.");
                return;
            }

            string captionHash;
            {
                var captionKeys = captionEntries.Select(x => x.Key).ToList();
                captionKeys.Sort();
                captionHash = string.Join("", captionKeys);
            }

            foreach (var q in query)
            {
                ParseTopic(q, out var topicName, out var topicAliases);

                var topicHash = (captionHash + topicName +
                                 (topicAliases != null ? string.Join("", topicAliases) : "")).CalculateMD5Hash()
                    .Substring(0, 5);

                {
                    // Check to see if we already indexed this topic.
                    var existingTopic = workspace.FindTopic(topicName);
                    if (existingTopic != null && existingTopic.Hash == topicHash)
                    {
                        Log.Warning("Already indexed {topic}, skipping...", topicName);
                        continue;
                    }
                }
                
                Log.Information("Indexing {topic}...", topicName);

                var result = new TopicSearch();
                result.Hash = topicHash;
                result.Topic = topicName;
                result.Aliases = topicAliases;
                result.Results = new List<TopicSearch.VideoResult>();

                foreach (var captions in captionEntries)
                {
                    var segments = SearchEngine.Search(captions.Value, topicAliases ?? new List<string> {topicName});
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
            }

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

            name = name.Trim();
            
            if (string.IsNullOrEmpty(name))
            {
                Log.Error("Invalid topic name.");
                Environment.Exit(1);
            }
            
            if (aliases != null && aliases.Any(string.IsNullOrEmpty))
            {
                Log.Error("Invalid topic name.", topic);
                Environment.Exit(1);
            }
        }
    }
}
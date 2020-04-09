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

            foreach (var q in query)
            {
                ParseTopic(q, out var topicName, out var topicAliases);

                Log.Information("Indexing {topic}...", topicName);

                var topic = workspace.FindTopic(topicName);

                if (topic == null)
                {
                    topic = new TopicSearch();
                    topic.Topic = topicName;
                    topic.Aliases = topicAliases;
                    topic.Results = new List<TopicSearch.VideoResult>();
                }
                else
                {
                    // If this topic has updated aliases, we need to reindex everything.
                    string Hash(string t, List<string> a)
                    {
                        var hash = (t ?? "").CalculateMD5Hash();
                        if (a != null)
                        {
                            hash += string.Join("", a.Select(x => x.CalculateMD5Hash()));
                        }
                        return hash;
                    }

                    if (Hash(topic.Topic, topic.Aliases) != Hash(topicName, topicAliases))
                    {
                        topic.Indexed = null;
                    }
                }

                var indexedVideos = new List<string>();
                if (!string.IsNullOrEmpty(topic.Indexed))
                {
                    indexedVideos = topic.Indexed.Split(",").ToList();
                }
                else
                {
                    indexedVideos = new List<string>();
                }

                foreach (var captions in captionEntries)
                {
                    if (indexedVideos.Contains(captions.Key))
                    {
                        continue;
                    }
                    indexedVideos.Add(captions.Key);
                    
                    var segments = SearchEngine.Search(captions.Value, topicAliases ?? new List<string> {topicName});
                    if (segments.Count == 0)
                    {
                        continue;
                    }

                    topic.Results.Add(new TopicSearch.VideoResult
                    {
                        Id = captions.Key,
                        Segments = segments.Select(x => new TopicSearch.VideoResult.Segment
                        {
                            Text = x.Text,
                            Location = x.Location
                        }).ToList()
                    });
                }

                topic.Indexed = string.Join(",", indexedVideos);
                workspace.SaveTopic(topic);
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
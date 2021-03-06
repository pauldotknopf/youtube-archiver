using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Common.Models;
using Lucene.Net.Analysis.Standard;
using Lucene.Net.Index;
using Lucene.Net.Search.Highlight;
using Lucene.Net.Search.Spans;
using Lucene.Net.Util;
using Newtonsoft.Json;
using Serilog;

namespace YouTubeArchiver.Index
{
    public class SearchCaptions
    {
        public static Command Create()
        {
            var command = new Command("search-captions")
            {
                new Option(new[]{"-q", "--query"})
                {
                    Name = "query",
                    Required = true,
                    Argument = new Argument
                    {
                        Arity = ArgumentArity.ExactlyOne
                    }
                }
            };

            command.Handler = CommandHandler.Create(typeof(SearchCaptions).GetMethod(nameof(Run)));

            return command;
        }

        public static void Run(string indexDirectory, string query)
        {
            var workspace = Helpers.GetWorkspace(indexDirectory);
                
            Log.Logger.Information("Querying for {query}...", query);
            
            Log.Logger.Information("Discovering captions...");
            var captionEntries = workspace.GetCaptions();

            if (captionEntries.Count == 0)
            {
                Log.Logger.Information("No captions are present.");
                Environment.Exit(1);
            }

            var videoCaptions = workspace.GetCaptions();
            var videosWithCaptions = workspace.GetVideos().Where(x => videoCaptions.ContainsKey(x.Id)).ToList();
            
            Log.Information("Searching captions for {count} videos...", videosWithCaptions.Count);
            
            var topic = new TopicSearch();
            topic.Topic = query;

            foreach (var video in videosWithCaptions)
            {
                var captionText = string.Join(" ", captionEntries[video.Id].Select(x => $"[@{x.Start}] {x.Value}"));

                var terms = new List<SpanQuery>();
                foreach (var term in query.Trim().Split(" "))
                {
                    terms.Add(new SpanTermQuery(new Term("content", term)));
                }
                var spanNearQuery = new SpanNearQuery(terms.ToArray(), 25, false);
                
                var queryScorer = new QueryScorer(spanNearQuery);
                var highlighter = new Highlighter(new MarkerFormatter(), queryScorer)
                {
                    TextFragmenter = new NullFragmenter()
                };
                var tokenStream = new StandardAnalyzer(LuceneVersion.LUCENE_48).GetTokenStream("content", captionText);

                var searchResult = highlighter.GetBestFragment(tokenStream, captionText);

                if (string.IsNullOrEmpty(searchResult))
                {
                    continue;
                }

                var model = new TopicSearch.VideoResult();
                model.Id = video.Id;
                model.Segments = new List<TopicSearch.VideoResult.Segment>();
                
                foreach (Match match in Regex.Matches(searchResult, @"\[\@([0-9\.]*)\].+?(?=\[\@[0-9\.]*\]|$)"))
                {
                    var segment = match.Groups[0].Value;
                    if (!segment.Contains("!!!! "))
                    {
                        continue;
                    }

                    segment = segment.Replace("!!!! ", "");
                    segment = Regex.Replace(segment, @"\[\@[0-9\.]*\]", "");
                    segment = segment.Trim();

                    var timeStamp = (int)Math.Max(Math.Floor(decimal.Parse(match.Groups[1].Value)), 0);
                    
                    model.Segments.Add(new TopicSearch.VideoResult.Segment
                    {
                        Text = segment,
                        Location = timeStamp
                    });
                }
                
                topic.Results.Add(model);
            }
            
            Log.Information("Found {total} videos, with {total} segments.", topic.Results.Count, topic.Results.Sum(x => x.Segments.Count));
            
            Console.WriteLine(JsonConvert.SerializeObject(topic, Formatting.Indented));
            
            Log.Logger.Information("Done!");
        }
        
        private class MarkerFormatter : IFormatter
        {
            public string HighlightTerm(string originalText, TokenGroup tokenGroup)
            {
                if (tokenGroup.TotalScore <= 0.0)
                    return originalText;

                return $"!!!! *{originalText}*";
            }
        }
    }
}
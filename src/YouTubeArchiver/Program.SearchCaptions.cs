using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using CommandLine;
using Common.Models;
using Lucene.Net.Analysis.Standard;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.Search.Highlight;
using Lucene.Net.Search.Spans;
using Lucene.Net.Store;
using Lucene.Net.Util;
using Newtonsoft.Json;
using Serilog;
using Directory = Lucene.Net.Store.Directory;

namespace YouTubeArchiver
{
    partial class Program
    {
        [Verb("search-captions")]
        class SearchCaptionsOptions : BaseIndexOptions
        {
            [Option('q', "query", Required = true)]
            public string Query { get; set; }
        }

        private static int SearchCaptions(SearchCaptionsOptions options)
        {
            if (string.IsNullOrEmpty(options.Query))
            {
                Log.Logger.Error("You must provide a query.");
                return 1;
            }
            
            options.Init();

            var workspace = options.GetWorkspace();

            Log.Logger.Information("Discovering captions...");
            workspace.DiscoverCaptions();

            if (workspace.CaptionsFiles.Count == 0)
            {
                Log.Logger.Information("No captions are present.");
                return 0;
            }
            
            Log.Logger.Information("Searching captions for {count} videos...", workspace.CaptionsFiles.Count);
            
            var topic = new TopicSearch();
            topic.Topic = options.Query;

            var index = 0;
            foreach (var caption in workspace.CaptionsFiles)
            {
                index++;
                Log.Logger.Information("Searching video {current} of {total}...", index, workspace.CaptionsFiles.Count);
                
                var video = workspace.Index.Videos.Single(x => x.Id == caption.Key);
                var captions = JsonConvert.DeserializeObject<List<Caption>>(File.ReadAllText(caption.Value));
                
                var captionText = string.Join(" ", captions.Select(x => $"[@{x.Start}] {x.Value}"));

                var terms = new List<SpanQuery>();
                foreach (var term in options.Query.Trim().Split(" "))
                {
                    terms.Add(new SpanTermQuery(new Term("content", term)));
                }
                var spanNearQuery = new SpanNearQuery(terms.ToArray(), 10, true);
                
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
                model.Title = video.Title;
                model.Url = $"https://www.youtube.com/watch?v={video.Id}";
                model.UploadedOn = video.UploadedOn?.ToString("d");
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
                        Location = timeStamp,
                        DirectUrl = $"https://www.youtube.com/watch?v={video.Id}&t={timeStamp}s"
                    });
                }
                
                topic.Results.Add(model);
            }
            
            Log.Logger.Information("Found {total} videos, with {total} segments.", topic.Results.Count, topic.Results.Sum(x => x.Segments.Count));
            
            Console.WriteLine(JsonConvert.SerializeObject(topic, Formatting.Indented));
            
            return 0;
        }

        public class CaptionSearchResultModel
        {
            [JsonProperty("id")]
            public string Id { get; set; }
            
            [JsonProperty("title")]
            public string Title { get; set; }
            
            [JsonProperty("url")]
            public string Url { get; set; }
            
            [JsonProperty("uploadedOn")]
            public string UploadedOn { get; set; }

            [JsonProperty("segments")]
            public List<SegmentModel> Segments { get; set; }
            
            public class SegmentModel
            {
                [JsonProperty("text")]
                public string Text { get; set; }
                
                [JsonProperty("directUrl")]
                public string DirectUrl { get; set; }
            }
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
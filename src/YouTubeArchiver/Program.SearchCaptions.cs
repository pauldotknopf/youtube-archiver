using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using CommandLine;
using Lucene.Net.Analysis;
using Lucene.Net.Analysis.Standard;
using Lucene.Net.Codecs.Lucene41;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.Index.Extensions;
using Lucene.Net.Index.Memory;
using Lucene.Net.QueryParsers.Classic;
using Lucene.Net.Search;
using Lucene.Net.Search.Highlight;
using Lucene.Net.Search.Spans;
using Lucene.Net.Store;
using Lucene.Net.Util;
using Newtonsoft.Json;
using Serilog;
using YouTubeArchiver.Models;
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

            var index = options.GetIndex();

            Log.Logger.Information("Indexing the captions...");

            var results = new List<CaptionSearchResultModel>();
            
            foreach (var video in index.Videos)
            {
                var captionPath = Path.Combine(options.IndexDirectory, "captions", $"{video.Id}.json");
                if (!File.Exists(captionPath))
                {
                    continue;
                }

                var captions = JsonConvert.DeserializeObject<List<Caption>>(File.ReadAllText(captionPath));
                
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

                var model = new CaptionSearchResultModel();
                model.Id = video.Id;
                model.Title = video.Title;
                model.Url = $"https://www.youtube.com/watch?v={video.Id}";
                model.UploadedOn = video.UploadedOn?.ToString("d");
                model.Segments = new List<CaptionSearchResultModel.SegmentModel>();
                
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

                    var timeStamp = decimal.Parse(match.Groups[1].Value);
                    
                    model.Segments.Add(new CaptionSearchResultModel.SegmentModel
                    {
                        Text = segment,
                        DirectUrl = $"https://www.youtube.com/watch?v={video.Id}&t={Math.Max(Math.Floor(timeStamp), 0)}s"
                    });
                }
                
                results.Add(model);
            }
            
            Console.WriteLine(JsonConvert.SerializeObject(results, Formatting.Indented));
            
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

        private static Directory IndexCaptions(List<Tuple<Video, List<Caption>>> captions)
        {
            var directory = new RAMDirectory();
            
            var iwc = new IndexWriterConfig(LuceneVersion.LUCENE_48, new StandardAnalyzer(LuceneVersion.LUCENE_48));
            using (var writer = new IndexWriter(directory, iwc))
            {
                foreach (var pair in captions)
                {
                    var document = new Document();
                    document.Add(new StringField("videoId", pair.Item1.Id, Field.Store.YES));
                    var text = string.Join(" ", pair.Item2.Select(x => $"[@{x.Duration}] {x.Value}"));
                    document.Add(new TextField("text", text, Field.Store.YES));
                    writer.AddDocument(document);
                }
            }

            return directory;
        }

        private static List<string> QueryCaptions(Directory directory, string keywords)
        {
            return null;
            using (var reader = DirectoryReader.Open(directory))
            {
                // var indexSearcher = new IndexSearcher(reader);
                //
                // var analyzer = new StandardAnalyzer(LuceneVersion.LUCENE_48);
                // var queryParser = new QueryParser(LuceneVersion.LUCENE_48, "text", analyzer);
                // var query = queryParser.Parse(keywords);
                //
                // var results = indexSearcher.Search(query, null, int.MaxValue);
                //
                // var docs = results.ScoreDocs.Select(x => reader.Document(x.Doc)).ToList();

                //return docs.Select(x => x.Get("videoId")).ToList();
                
                // var indexSearcher = new IndexSearcher(reader);
                // var terms = new List<SpanQuery>();
                // foreach (var term in options.Query.Trim().Split(" "))
                // {
                //     terms.Add(new SpanTermQuery(new Term("content", term)));
                // }
                // var spanNearQuery = new SpanNearQuery(terms.ToArray(), 10, true);
                //
                // spanNearQuery.GetSpans(indexSearcher.IndexReader, new FixedBitSet(0),
                //     new Dictionary<Term, TermContext>());


            }
        }
    }
}
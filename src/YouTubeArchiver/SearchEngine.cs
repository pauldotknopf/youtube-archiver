using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Common.Models;
using Lucene.Net.Analysis.Standard;
using Lucene.Net.Index;
using Lucene.Net.Search.Highlight;
using Lucene.Net.Search.Spans;
using Lucene.Net.Util;

namespace YouTubeArchiver
{
    public class SearchEngine
    {
        public static List<SegmentResult> Search(List<Caption> captions, List<string> queries)
        {
            if (queries.Count == 0)
            {
                throw new Exception("You must provide queries.");
            }

            queries = queries.Select(x => (x ?? "").Trim()).ToList();
            if (queries.Any(string.IsNullOrEmpty))
            {
                throw new Exception("The queries must not be empty.");
            }
            
            var captionText = string.Join(" ", captions.Select(x => $"[@{x.Start}] {x.Value}"));

            var nearQueries = new List<SpanQuery>();

            foreach (var query in queries)
            {
                var terms = new List<SpanQuery>();
                foreach (var term in query.Trim().Split(" "))
                {
                    terms.Add(new SpanTermQuery(new Term("content", term)));
                }
                nearQueries.Add(new SpanNearQuery(terms.ToArray(), 25, false));
            }
            
            var queryScorer = new QueryScorer(new SpanOrQuery(nearQueries.ToArray()));
            var highlighter = new Highlighter(new MarkerFormatter(), queryScorer)
            {
                TextFragmenter = new NullFragmenter()
            };
            var tokenStream = new StandardAnalyzer(LuceneVersion.LUCENE_48).GetTokenStream("content", captionText);

            var searchResult = highlighter.GetBestFragment(tokenStream, captionText);

            var result = new List<SegmentResult>();
            
            if (string.IsNullOrEmpty(searchResult))
            {
                return result;
            }

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
                
                result.Add(new SegmentResult
                {
                    Text = segment,
                    Location = timeStamp
                });
            }

            return result;
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

        public class SegmentResult
        {
            public string Text { get; set; }
            
            public int Location { get; set; }
        }
    }
}
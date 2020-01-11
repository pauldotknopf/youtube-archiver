using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web;
using System.Xml;
using CommandLine;
using Common.Models;
using Newtonsoft.Json;
using Serilog;

namespace YouTubeArchiver
{
    partial class Program
    {
        [Verb("get-captions")]
        class GetCaptionsOptions : BaseIndexOptions
        {
            
        }

        private static int GetCaptions(GetCaptionsOptions options)
        {
            options.Init();
            
            var workspace = options.GetWorkspace();
            
            Log.Logger.Information("Discovering already downloaded captions...");
            workspace.DiscoverCaptions();
            
            foreach (var video in workspace.Index.Videos)
            {
                Log.Logger.Information("Downloading captions for {videoId}...", video.Id);

                if (workspace.CaptionsFiles.ContainsKey(video.Id))
                {
                    Log.Logger.Information("Already downloaded, skipping...");
                    continue;
                }

                workspace.DownloadCaption(video, path =>
                {
                    if (File.Exists(path))
                    {
                        File.Delete(path);
                    }
                    
                    try
                    {
                        var getVideoResponse =
                            GetRequestBody($"https://www.youtube.com/get_video_info?html5=1&video_id={video.Id}");

                        var keys = getVideoResponse.Split("&").Select(x =>
                        {
                            var split = x.Split("=");
                            return new Tuple<string, string>(split[0], HttpUtility.UrlDecode(split[1]));
                        }).ToDictionary(x => x.Item1, x => x.Item2);

                        var playerResponse = JsonConvert.DeserializeObject<GetVideoPlayerObject>(keys["player_response"]);

                        var caption = playerResponse.Captions?.PlayerCaptionsTracklistRenderer?.CaptionTracks
                            .FirstOrDefault();

                        if (caption == null)
                        {
                            Log.Logger.Warning("No caption present.");
                            return;
                        }

                        var captionXml = GetRequestBody(caption.BaseUrl);

                        var xmlDoc = new XmlDocument();
                        xmlDoc.LoadXml(captionXml);

                        var captions = new List<Caption>();
                        foreach (XmlElement item in xmlDoc.GetElementsByTagName("text"))
                        {
                            var innerText = item.InnerText;
                            if (!string.IsNullOrEmpty(innerText))
                            {
                                innerText = HttpUtility.HtmlDecode(innerText);
                            }

                            innerText = Regex.Replace(innerText, @"<[^>]*>", "");
                            
                            captions.Add(new Caption
                            {
                                Start = double.Parse(item.GetAttribute("start")),
                                Duration = double.Parse(item.GetAttribute("dur")),
                                Value = innerText
                            });
                        }
                        
                        File.WriteAllText(path, JsonConvert.SerializeObject(captions, Newtonsoft.Json.Formatting.Indented));
                        
                        Log.Logger.Information("Saved!");
                    }
                    catch (Exception ex)
                    {
                        Log.Logger.Error(ex, "Couldn't get timed text for {videoId}. " + ex.Message, video.Id);
                    }
                });
            }

            return 0;
        }
    }
}
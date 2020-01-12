using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web;
using System.Xml;
using Common.Models;
using Newtonsoft.Json;
using Serilog;

namespace YouTubeArchiver
{
    partial class Program
    {
        class CaptionsCommand
        {
            public static Command Create()
            {
                var command = new Command("index-captions")
                {
                   Helpers.BuildIndexOption()
                };

                command.Handler = CommandHandler.Create(typeof(CaptionsCommand).GetMethod(nameof(Run)));

                return command;
            }

            public static void Run(string indexDirectory)
            {
                var workspace = Helpers.GetWorkspace(indexDirectory);
                
                Log.Logger.Information("Discovering already downloaded captions...");
                workspace.DiscoverCaptions();
                
                var videosWithoutCaptions = workspace.Index.Videos.Where(x => !workspace.CaptionsFiles.ContainsKey(x.Id))
                    .ToList();
                
                Log.Logger.Information("Downloading {total} captions...", videosWithoutCaptions.Count);

                int index = 0;
                foreach (var video in videosWithoutCaptions)
                {
                    index++;
                    Log.Logger.Information("Downloading captions for {videoId} ({current} of {total})...", video.Id, index, videosWithoutCaptions.Count);

                    workspace.DownloadCaption(video, path =>
                    {
                        if (File.Exists(path))
                        {
                            File.Delete(path);
                        }
                        
                        try
                        {

                            var player = Helpers.GetVideoPlayerInfoForYouTubeVideo(video.Id);

                            var caption = (player.Captions?.PlayerCaptionsTracklistRenderer?.CaptionTracks ?? new List<GetVideoPlayerObject.PlayerCaptionsTracklistRendererDataCaptionTrack>())
                                .FirstOrDefault();

                            if (caption == null)
                            {
                                Log.Logger.Warning("No caption present.");
                                return;
                            }

                            var captionXml = Helpers.GetRequestBody(caption.BaseUrl);

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
            }
        }
    }
}
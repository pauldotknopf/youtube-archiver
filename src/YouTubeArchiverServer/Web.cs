using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc.Razor.RuntimeCompilation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Statik.Files;
using Statik.Mvc;
using Statik.Web;
using YouTubeArchiverServer.Models;
using YouTubeArchiverServer.State;

namespace YouTubeArchiverServer
{
    public class Web
    {
        public static IWebBuilder GetBuilder(
            List<ChannelModel> channels, Config config)
        {
            var builder = Statik.Statik.GetWebBuilder();
            
            builder.RegisterServices(services =>
            {
                services.AddSingleton(channels);
                services.AddSingleton(config);
            });
            
            builder.RegisterMvcServices();
            builder.RegisterServices(services =>
            {
                services.AddRazorPages()
                    .AddRazorRuntimeCompilation();
                services.Configure<MvcRazorRuntimeCompilationOptions>(options =>
                {
                    options.FileProviders.Add(new Statik.Embedded.EmbeddedFileProvider(typeof(Program).Assembly, "YouTubeArchiverServer.Resources"));
                    //options.FileProviders.Add(new PhysicalFileProvider("/home/pknopf/git/youtube-archiver/src/YouTubeArchiverServer/Resources"));
                });
            });
            
            builder.RegisterFileProvider(new Statik.Embedded.EmbeddedFileProvider(typeof(Program).Assembly, "YouTubeArchiverServer.Resources.wwwroot"));
            //builder.RegisterFileProvider(new PhysicalFileProvider("/home/pknopf/git/youtube-archiver/src/YouTubeArchiverServer/Resources/wwwroot"));
            
            builder.RegisterMvc("/", new
            {
                controller = "Videos",
                action = "All"
            });
            
            builder.RegisterMvc("/channels", new
            {
                controller = "Channel",
                action = "List"
            });

            foreach (var channel in channels)
            {
                builder.RegisterMvc($"/channel/{channel.Channel.Id}", new
                {
                    controller = "Videos",
                    action = "ForChannel",
                    channelId = channel.Channel.Id
                });
            }
            
            RegisterVideos(builder, channels);
            RegisterTopics(builder, channels);

            return builder;
        }

        private static void RegisterTopics(IWebBuilder builder, List<ChannelModel> channels)
        {
            builder.RegisterMvc("/topics", new
            {
                controller = "Topic",
                action = "List"
            });

            foreach (var channel in channels)
            {
                builder.RegisterMvc($"/topics/{channel.Channel.Id}", new
                {
                    controller = "Topic",
                    action = "ListByChannel",
                    channelId = channel.Channel.Id
                });
            }
            
            foreach (var topic in channels.SelectMany(x => x.Topics)
                .GroupBy(x => x.Id)
                .Select(x => x.First()))
            {
                builder.RegisterMvc($"/topic/{topic.Id}", new
                {
                    controller = "Topic",
                    action = "TopicVideos",
                    topicId = topic.Id
                }, new PageState(topic.Topic));
            }
            
            foreach (var channel in channels)
            {
                foreach (var topic in channel.Topics)
                {
                    if (topic.Videos.Count > 0)
                
                    {
                        builder.RegisterMvc($"/topic/{topic.Id}/channel/{channel.Channel.Id}", new
                        {
                            controller = "Topic",
                            action = "TopicVideosForChannel",
                            topicId = topic.Id,
                            channelId = channel.Channel.Id
                        }, new PageState($"{topic.Topic} - {channel.Channel.Title}"));
                    }
                }
            }
        }

        private static void RegisterVideos(IWebBuilder builder, List<ChannelModel> channels)
        {
            foreach (var channel in channels)
            {
                foreach (var video in channel.Videos)
                {
                    builder.RegisterMvc($"/video/{video.Video.Id}", new
                    {
                        controller = "Video",
                        action = "Index",
                        videoId = video.Video.Id,
                        channelId = channel.Channel.Id
                    });
                }
            }
        }
    }
}
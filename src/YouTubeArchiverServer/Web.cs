using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc.Razor.RuntimeCompilation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Statik.Files;
using Statik.Mvc;
using Statik.Web;
using YouTubeArchiverServer.Models;

namespace YouTubeArchiverServer
{
    public class Web
    {
        public static IWebBuilder GetBuilder(
            List<ChannelModel> channels)
        {
            var videosModel = new VideosModel();
            foreach (var channel in channels)
            {
                foreach (var video in channel.Videos)
                {
                    videosModel.Videos.Add(video);
                }
            }
            
            var builder = Statik.Statik.GetWebBuilder();
            
            builder.RegisterServices(services =>
            {
                services.AddSingleton(channels);
            });
            
            builder.RegisterMvcServices();
            builder.RegisterServices(services =>
            {
                services.AddRazorPages()
                    .AddRazorRuntimeCompilation();
                services.Configure<MvcRazorRuntimeCompilationOptions>(options =>
                {
                    //options.FileProviders.Add(new Statik.Embedded.EmbeddedFileProvider(typeof(Program).Assembly, "YouTubeArchiverServer.Resources"));
                    options.FileProviders.Add(new PhysicalFileProvider("/home/pknopf/git/youtube-archiver/src/YouTubeArchiverServer/Resources"));
                });
            });
            
            //builder.RegisterFileProvider(new Statik.Embedded.EmbeddedFileProvider(typeof(Program).Assembly, "YouTubeArchiverServer.Resources.wwwroot"));
            builder.RegisterFileProvider(new PhysicalFileProvider("/home/pknopf/git/youtube-archiver/src/YouTubeArchiverServer/Resources/wwwroot"));
            
            builder.RegisterMvc("/", new
            {
                controller = "Videos",
                action = "Index",
                data = videosModel
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
                    controller = "Channel",
                    action = "Index",
                    channelId = channel.Channel.Id
                });
            }
            
            RegisterVideos(builder, channels);
            RegisterTopics(builder, channels);

            return builder;
        }

        private static void RegisterTopics(IWebBuilder builder, List<ChannelModel> channels)
        {
            var topicsModel = new TopicsModel();

            foreach (var topic in channels.SelectMany(x => x.Topics))
            {
                var current = topicsModel.Topics.SingleOrDefault(x => x.Id == topic.Id);
                if (current == null)
                {
                    current = new TopicModel();
                    topicsModel.Topics.Add(current);
                }

                current.Id = topic.Id;
                current.Topic = topic.Topic;
                current.Videos.AddRange(topic.Videos);
            }
            
            builder.RegisterMvc("/topics", new
            {
                controller = "Topic",
                action = "List",
                data = topicsModel
            });
            
            foreach (var topic in topicsModel.Topics)
            {
                builder.RegisterMvc($"/topic/{topic.Id}", new
                {
                    controller = "Topic",
                    action = "Index",
                    data = topic
                });
            }

            if (topicsModel.Topics.Any())
            {
                builder.RegisterServices(services =>
                {
                    services.Configure<Config>(x => x.HasTopics = true);
                });
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
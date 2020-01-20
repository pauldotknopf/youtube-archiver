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
                controller = "Channel",
                action = "Index",
                channelId = channels[0].Channel.Id
            });
            
            RegisterVideos(builder, channels);
            RegisterTopics(builder, channels);

            return builder;
        }

        private static void RegisterTopics(IWebBuilder builder, List<ChannelModel> channels)
        {
            bool hasTopic = false;
            foreach (var topic in channels.SelectMany(x => x.Topics))
            {
                hasTopic = true;
                builder.RegisterMvc($"/topic/{topic.Id}", new
                {
                    controller = "Topic",
                    action = "Index",
                    topic
                });
            }

            if (hasTopic)
            {
                builder.RegisterServices(services =>
                    {
                        services.Configure<Config>(config => { config.HasTopics = true; });
                    });
                builder.RegisterMvc("/topics", new
                {
                    controller = "Topic",
                    action = "List",
                    /*TODO: support multiple channels.*/
                    channel = channels[0]
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
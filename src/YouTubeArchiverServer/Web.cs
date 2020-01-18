using Microsoft.AspNetCore.Mvc.Razor.RuntimeCompilation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Statik.Files;
using Statik.Mvc;
using Statik.Web;
using YouTubeArchiverServer.Services;

namespace YouTubeArchiverServer
{
    public class Web
    {
        public static IWebBuilder GetBuilder(
            Config config,
            ChannelService channelService)
        {
            var builder = Statik.Statik.GetWebBuilder();
            
            builder.RegisterServices(services =>
            {
                services.AddSingleton(config);
                services.AddSingleton(channelService);
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
                channelId = channelService.GetChannels()[0].Id
            });
            
            RegisterVideos(builder, channelService);

            return builder;
        }

        private static void RegisterVideos(IWebBuilder builder, ChannelService channelService)
        {
            foreach (var channel in channelService.GetChannels())
            {
                foreach (var video in channelService.GetChannelVideos(channel.Id))
                {
                    builder.RegisterMvc($"/video/{video.Id}", new
                    {
                        controller = "Video",
                        action = "Index",
                        videoId = video.Id,
                        channelId = channel.Id
                    });
                }
            }
        }
    }
}
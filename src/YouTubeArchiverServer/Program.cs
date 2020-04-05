using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.IO;
using System.Linq;
using Common;
using Serilog;
using Serilog.Events;
using Statik.Hosting;
using Statik.Web;

namespace YouTubeArchiverServer
{
    partial class Program
    {
        static int Main(string[] args)
        {
            Log.Logger = new LoggerConfiguration()
                .WriteTo.Console(standardErrorFromLevel: LogEventLevel.Verbose)
                .CreateLogger();
            
            var rootCommand = new RootCommand
            {
                ServeCommand.Create(),
                GenerateCommand.Create()
            };

            rootCommand.Name = "youtube-archiver-server";
            rootCommand.Description = "YouTube Archiver Server";

            try
            {
                return rootCommand.InvokeAsync(args).Result;
            }
            catch (Exception ex)
            {
                Log.Logger.Error(ex, ex.Message);
                return 1;
            }
        }

        static IWebBuilder BuildWeb(string configPath)
        {
            ServeConfig config;

            if (string.IsNullOrEmpty(configPath))
            {
                Log.Error("You must provide a config path.");
                Environment.Exit(1);
            }
            
            if (!File.Exists(configPath))
            {
                Log.Error("The config path doesn't exist.");
                Environment.Exit(1);
            }
            
            config = ServeConfig.Load(configPath);

            if (config.Indexes == null || config.Indexes.Count == 0)
            {
                Log.Error("You must provide at least one index.");
                Environment.Exit(1);
            }
            
            if (string.IsNullOrEmpty(config.Title))
            {
                config.Title = "YouTube Archive";
            }
            
            config.Indexes = config.Indexes.Distinct().ToList();
            
            var indexes = config.Indexes.Select(IndexWorkspace.Create).ToList();

            if (indexes.Count == 0)
            {
                Log.Error("You must provide at least one index.");
                Environment.Exit(1);
            }
                
            return Web.GetBuilder(
                ModelBuilder.BuildChannelModels(
                    indexes.Cast<IIndexWorkspace>().ToList()), new Config
                {
                    SiteTitle = config.Title,
                    FooterHtml = config.FooterHtml
                });
        }
    }
}

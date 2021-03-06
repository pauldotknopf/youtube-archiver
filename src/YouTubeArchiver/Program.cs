﻿using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using Serilog;
using Serilog.Events;

namespace YouTubeArchiver
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
                Index.Base.Create(),
                Auth.Create(),
                DownloadVideo.Create(),  
            };

            rootCommand.Name = "youtube-archiver";
            rootCommand.Description = "Archive YouTube channels.";

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
    }
}

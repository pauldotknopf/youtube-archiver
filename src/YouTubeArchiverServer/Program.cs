using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using Serilog;
using Serilog.Events;

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
            };

            rootCommand.Description = "YouTube Archiver";

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

using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Common;
using Serilog;

namespace YouTubeArchiverServer
{ 
    partial class Program
    {
        class ServeCommand
        {
            public static Command Create()
            {
                var command = new Command("serve")
                {
                    new Option("port")
                    {
                        Argument = new Argument<int>(() => 8000)
                        {
                            Arity = ArgumentArity.ExactlyOne
                        }
                    },
                    new Option("app-base")
                    {
                        Argument = new Argument<string>
                        {
                            Name = "appBase",
                            Arity = ArgumentArity.ExactlyOne
                        }
                    },
                    new Option(new []{"-i", "--index-directory"}, "The directory where the index exists.")
                    {
                        Name = "index-directory",
                        Argument = new Argument<string>
                        {
                            Arity = ArgumentArity.OneOrMore
                        }
                    }
                };

                command.Handler = CommandHandler.Create(typeof(ServeCommand).GetMethod(nameof(Run)));

                return command;
            }
            
            public static Task Run(int port, string appBase, List<string> indexDirectory)
            {
                var indexes = indexDirectory.Select(IndexWorkspace.Create).ToList();

                if (indexes.Count == 0)
                {
                    Log.Error("You must provide at least 1 index directory.");
                    Environment.Exit(1);
                }
                
                var builder = Web.GetBuilder(
                    ModelBuilder.BuildChannelModels(
                        indexes.Cast<IIndexWorkspace>().ToList()));
                
                using (var host = builder.BuildWebHost(appBase, port))
                {
                    host.Listen();
                    Log.Logger.Information($"Listening on port {port}...");
                    Log.Logger.Information($"Press [enter] to quit.");
                    Console.ReadLine();
                }

                return Task.CompletedTask;
            }
        }
    }
}
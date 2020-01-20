using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.IO;
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
                    }
                };

                command.Handler = CommandHandler.Create(typeof(ServeCommand).GetMethod(nameof(Run)));

                return command;
            }
            
            public static async Task Run(int port, string appBase)
            {
                var channelIndex = IndexWorkspace.Create(Directory.GetCurrentDirectory());
                
                var builder = Web.GetBuilder(
                    ModelBuilder.BuildChannelModels(
                        new List<IndexWorkspace>
                        {
                            channelIndex
                        }));
                
                using (var host = builder.BuildWebHost(appBase, port))
                {
                    host.Listen();
                    Log.Logger.Information($"Listening on port {port}...");
                    Log.Logger.Information($"Press [enter] to quit.");
                    Console.ReadLine();
                }
            }
        }
    }
}
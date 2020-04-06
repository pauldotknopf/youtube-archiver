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
                    new Option("--port")
                    {
                        Argument = new Argument<int>(() => 8000)
                        {
                            Arity = ArgumentArity.ExactlyOne
                        }
                    },
                    new Option("--app-base")
                    {
                        Argument = new Argument<string>
                        {
                            Name = "app-base",
                            Arity = ArgumentArity.ExactlyOne
                        }
                    },
                    new Option("--config-path")
                    {
                        Argument = new Argument<string>
                        {
                            Name = "config-path",
                            Arity = ArgumentArity.ExactlyOne
                        }
                    }
                };

                command.Handler = CommandHandler.Create(typeof(ServeCommand).GetMethod(nameof(Run)));

                return command;
            }
            
            public static void Run(int port, string appBase, string configPath)
            {
                using (var host = BuildWeb(configPath).BuildWebHost(appBase, port))
                {
                    host.Listen();
                    Log.Logger.Information($"Listening on port {port}...");
                    Log.Logger.Information($"Press [enter] to quit.");
                    Console.ReadLine();
                }
                
                Log.Information("Done!");
            }
        }
    }
}
using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Common;
using Serilog;
using Statik.Hosting.Impl;

namespace YouTubeArchiverServer
{ 
    partial class Program
    {
        class GenerateCommand
        {
            public static Command Create()
            {
                var command = new Command("generate")
                {
                    new Option("--output")
                    {
                        Argument = new Argument<string>
                        {
                            Name = "output",
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

                command.Handler = CommandHandler.Create(typeof(GenerateCommand).GetMethod(nameof(Run)));

                return command;
            }
            
            public static async Task Run(string output, string appBase, string configPath)
            {
                if (string.IsNullOrEmpty(output))
                {
                    output = Path.Combine(Directory.GetCurrentDirectory(), "output");
                }

                output = Path.GetFullPath(output);
                
                Log.Information("Generate web to {output}...", output);
                
                using (var host = BuildWeb(configPath).BuildVirtualHost(appBase))
                {
                    await new HostExporter().Export(host, output);
                }
                
                Log.Information("Done!");
            }
        }
    }
}
using Build.Buildary;
using static Build.Buildary.Log;
using static Build.Buildary.GitVersion;
using static Build.Buildary.Path;
using static Build.Buildary.Directory;
using static Build.Buildary.File;
using static Build.Buildary.Shell;
using static Bullseye.Targets;

namespace Build
{
    class Program
    {
        static void Main(string[] args)
        {
            var options = Runner.ParseOptions<Runner.RunnerOptions>(args);
            
            ProjectDefinition.Register(options, new ProjectDefinition
            {
                SolutionPath = "./YouTubeArchiver.sln"
            });
            
            Runner.Execute(options);
        }
    }
}
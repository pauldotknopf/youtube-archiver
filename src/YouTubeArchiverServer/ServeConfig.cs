using System.Collections.Generic;
using System.IO;
using System.Linq;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace YouTubeArchiverServer
{
    public class ServeConfig
    {
        public string Title { get; set; }
        
        public string FooterHtml { get; set; }
        
        public List<string> Indexes { get; set; }
        
        public static ServeConfig Load(string serveConfig)
        {
            serveConfig = Path.GetFullPath(serveConfig);
            
            var directory = Path.GetDirectoryName(serveConfig);
            
            var deserializer = new DeserializerBuilder()
                .WithNamingConvention(CamelCaseNamingConvention.Instance)
                .Build();

            var result = deserializer.Deserialize<ServeConfig>(File.ReadAllText(serveConfig));

            if (result.Indexes == null)
            {
                result.Indexes = new List<string>();
            }

            result.Indexes = result.Indexes.Select(x => Path.Combine(directory, x)).ToList();

            return result;
        }
    }
}
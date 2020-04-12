using System.IO;
using System.IO.Compression;

namespace Common
{
    public enum VideoPathType
    {
        None,
        External,
        Local
    }
    
    public abstract class VideoPath
    {
        public abstract VideoPathType Type { get; }
        
        public static VideoPath None => new VideoPathNone();
        
        public static VideoPathLocal Local(string path) => new VideoPathLocal(path);

        public static VideoPathExternal External(string url) => new VideoPathExternal(url);
    }

    public class VideoPathNone : VideoPath
    {
        public override VideoPathType Type => VideoPathType.None;

        public override string ToString()
        {
            return "none";
        }
    }

    public class VideoPathExternal : VideoPath
    {
        public VideoPathExternal(string url)
        {
            Url = url;
        }
        
        public override VideoPathType Type => VideoPathType.External;
        
        public string Url { get; set; }

        public override string ToString()
        {
            return Url;
        }
    }

    public class VideoPathLocal : VideoPath
    {
        public VideoPathLocal(string path)
        {
            Path = path;
        }
        
        public override VideoPathType Type => VideoPathType.Local;
        
        public string Path { get; set; }

        public override string ToString()
        {
            return Path;
        }
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using Common;
using Common.Models;

namespace YouTubeArchiverServer.Services
{
    public class ChannelService
    {
        private readonly Dictionary<string, IndexWorkspace> _workspaces = new Dictionary<string, IndexWorkspace>();

        public ChannelService(List<IndexWorkspace> workspaces)
        {
            foreach (var workspace in workspaces)
            {
                if (_workspaces.ContainsKey(workspace.Index.Channel.Id))
                {
                    throw new Exception($"Duplicate channel {workspace.Index.Channel.Id}");
                }
                _workspaces[workspace.Index.Channel.Id] = workspace;
            }
        }

        public List<Channel> GetChannels()
        {
            return _workspaces.Values.Select(x => x.Index.Channel).ToList();
        }

        public Channel GetChannelById(string channelId)
        {
            return _workspaces[channelId].Index.Channel;
        }

        public List<Video> GetChannelVideos(string channelId)
        {
            return _workspaces[channelId].Index.Videos;
        }

        public Video GetVideoById(string videoId)
        {
            foreach (var workspace in _workspaces.Values)
            {
                foreach (var video in workspace.Index.Videos)
                {
                    if (video.Id == videoId)
                    {
                        return video;
                    }
                }
            }

            return null;
        }
    }
}
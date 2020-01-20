using System;
using System.Collections.Generic;
using System.Linq;
using Common;
using YouTubeArchiverServer.Models;

namespace YouTubeArchiverServer
{
    public class ModelBuilder
    {
        public static List<ChannelModel> BuildChannelModels(List<IndexWorkspace> workspaces)
        {
            var channels = new List<ChannelModel>();
        
            foreach (var workspace in workspaces)
            {
                // Build the channel
                
                if (channels.Any(x => x.Channel.Id == workspace.Index.Channel.Id))
                {
                    throw new Exception("Duplicate channels detected...");
                }

                var channel = new ChannelModel
                {
                    Channel = workspace.Index.Channel
                };

                // Load all the videos
                
                foreach (var video in workspace.Index.Videos)
                {
                    channel.Videos.Add(new VideoModel
                    {
                        Video = video,
                        Channel = channel
                    });
                }
                
                // Load all the topics
                
                 workspace.DiscoverTopics();

                foreach (var topicEntry in workspace.Topics)
                {
                    var topic = new TopicModel
                    {
                        Id = topicEntry.Key,
                        Topic = topicEntry.Value.Topic,
                        Channel = channel
                    };
                    
                    foreach (var result in topicEntry.Value.Results)
                    {
                        var topicVideo = new TopicVideoModel
                        {
                            Topic = topic,
                            Video = channel.Videos.Single(x => x.Video.Id == result.Id)
                        };
                        // Make sure the video reference knows about mentioned topics.
                        if (topicVideo.Video.MentionedTopics.All(x => x.Id != topic.Id))
                        {
                            topicVideo.Video.MentionedTopics.Add(topic);
                        }
                        foreach (var segment in result.Segments)
                        {
                            topicVideo.Segments.Add(new SegmentModel
                            {
                                Video = topicVideo,
                                Location = segment.Location,
                                Text = Markdig.Markdown.ToHtml(segment.Text)
                                    .Replace("<em>", "<b>").Replace("</em>", "</b>")
                                    .Replace("<p>", "").Replace("</p>", "")
                            });
                        }
                        topic.Videos.Add(topicVideo);
                    }
                    
                    channel.Topics.Add(topic);
                }
                
                channels.Add(channel);
            }

            return channels;
        }
        
        
    }
}
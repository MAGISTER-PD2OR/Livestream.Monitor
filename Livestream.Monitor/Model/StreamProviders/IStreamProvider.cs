﻿using System.Collections.Generic;
using System.Threading.Tasks;
using Livestream.Monitor.Model.Monitoring;

namespace Livestream.Monitor.Model.StreamProviders
{
    public interface IStreamProvider
    {
        string ProviderName { get; }

        /// <summary> The base url for the provider (always ending with a forward slash '/')  </summary>
        string BaseUrl { get; }

        bool HasChatSupport { get; }

        bool HasVodViewerSupport { get; }

        string GetStreamUrl(string streamId);

        /// <summary> Returns null if <see cref="HasChatSupport"/> is false </summary>
        string GetChatUrl(string streamId);

        /// <summary> Updates online streams and returns streams that were <b>not update</b> (offline streams) </summary>
        /// <param name="livestreams">A collection of potentially online streams</param>
        /// <returns>Offline livestreams</returns>
        Task<List<LivestreamModel>> UpdateOnlineStreams(List<LivestreamModel> livestreams);

        /// <summary> 
        /// Some stream providers such as twitch do not provide stream details for offline streams the same way as online streams. 
        /// </summary>
        Task UpdateOfflineStreams(List<LivestreamModel> livestreams);
    }
}
﻿using System;
using Livestream.Monitor.Model.Monitoring;

namespace Livestream.Monitor.Model.ApiClients
{
    public class LivestreamQueryResult
    {
        public LivestreamQueryResult(ChannelIdentifier channelIdentifier)
        {
            ChannelIdentifier = channelIdentifier ?? throw new ArgumentNullException(nameof(channelIdentifier));
        }

        public ChannelIdentifier ChannelIdentifier { get; }

        public LivestreamModel LivestreamModel { get; set; }

        public bool IsSuccess => FailedQueryException == null;

        public FailedQueryException FailedQueryException { get; set; }

        public override string ToString()
        {
            return $"{ChannelIdentifier} - IsSuccess: {IsSuccess}";
        }
    }
}
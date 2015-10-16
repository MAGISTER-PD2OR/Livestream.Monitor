﻿using System;
using Caliburn.Micro;

namespace Livestream.Monitor.Model
{
    public class ChannelData : PropertyChangedBase
    {
        private DateTimeOffset startTime;
        private long viewers;
        private string game;
        private string channelDescription;
        private string channelName;
        private bool live;

        public bool Live
        {
            get { return live; }
            set
            {
                if (value == live) return;
                live = value;
                NotifyOfPropertyChange(() => Live);
                NotifyOfPropertyChange(() => Uptime);
            }
        }

        public string ChannelName
        {
            get { return channelName; }
            set
            {
                if (value == channelName) return;
                channelName = value;
                NotifyOfPropertyChange(() => ChannelName);
            }
        }

        public string ChannelDescription
        {
            get { return channelDescription; }
            set
            {
                if (value == channelDescription) return;
                channelDescription = value;
                NotifyOfPropertyChange(() => ChannelDescription);
            }
        }

        public string Game
        {
            get { return game; }
            set
            {
                if (value == game) return;
                game = value;
                NotifyOfPropertyChange(() => Game);
            }
        }

        public long Viewers
        {
            get { return viewers; }
            set
            {
                if (value == viewers) return;
                viewers = value;
                NotifyOfPropertyChange(() => Viewers);
            }
        }

        public DateTimeOffset StartTime
        {
            get { return startTime; }
            set
            {
                if (value.Equals(startTime)) return;
                startTime = value;
                NotifyOfPropertyChange(() => StartTime);
                NotifyOfPropertyChange(() => Uptime);
            }
        }

        public TimeSpan Uptime => Live ? DateTimeOffset.Now - StartTime : TimeSpan.Zero;

        public override string ToString() => 
            $"{channelName}, Viewers={viewers}, Uptime={Uptime.ToString("hh'h 'mm'm 'ss's'")}";
    }
}

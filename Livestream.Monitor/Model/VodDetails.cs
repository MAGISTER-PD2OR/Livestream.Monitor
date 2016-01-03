﻿using System;
using Caliburn.Micro;

namespace Livestream.Monitor.Model
{
    public class VodDetails : PropertyChangedBase
    {
        private string url;
        private TimeSpan length;
        private int views;
        private DateTime recordedAt;
        private string game;
        private string description;
        private string title;

        public string Url
        {
            get { return url; }
            set
            {
                if (value == url) return;
                url = value;
                NotifyOfPropertyChange(() => Url);
            }
        }

        public TimeSpan Length
        {
            get { return length; }
            set
            {
                if (value == length) return;
                length = value;
                NotifyOfPropertyChange(() => Length);
            }
        }

        public int Views
        {
            get { return views; }
            set
            {
                if (value == views) return;
                views = value;
                NotifyOfPropertyChange(() => Views);
            }
        }

        public DateTime RecordedAt
        {
            get { return recordedAt; }
            set
            {
                if (value.Equals(recordedAt)) return;
                recordedAt = value;
                NotifyOfPropertyChange(() => RecordedAt);
            }
        }

        public string Title
        {
            get { return title; }
            set
            {
                if (value == title) return;
                title = value;
                NotifyOfPropertyChange(() => Title);
            }
        }

        public string Description
        {
            get { return description; }
            set
            {
                if (value == description) return;
                description = value;
                NotifyOfPropertyChange(() => Description);
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
    }
}
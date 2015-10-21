using System;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Threading;
using Caliburn.Micro;
using Livestream.Monitor.Core;
using Livestream.Monitor.Model;

namespace Livestream.Monitor.ViewModels
{
    public class ChannelListViewModel : Screen
    {
        private readonly IMonitorStreamsModel monitorStreamsModel;
        private readonly DispatcherTimer refreshTimer;
        private readonly ISettingsHandler settingsHandler;
        private readonly IWindowManager windowManager;

        private bool loading;
        private ChannelData selectedChannelData;

        public ChannelListViewModel()
        {
            if (!Execute.InDesignMode)
                throw new InvalidOperationException("Constructor only accessible from design time");

            monitorStreamsModel = new MonitorStreamsModel();
        }

        public ChannelListViewModel(
            IMonitorStreamsModel monitorStreamsModel,
            ISettingsHandler settingsHandler,
            IWindowManager windowManager)
        {
            if (settingsHandler == null) throw new ArgumentNullException(nameof(settingsHandler));
            if (windowManager == null) throw new ArgumentNullException(nameof(windowManager));

            this.monitorStreamsModel = monitorStreamsModel;
            this.settingsHandler = settingsHandler;
            this.windowManager = windowManager;
            refreshTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(30) };
            refreshTimer.Tick += async (sender, args) => await RefreshChannels();
        }

        public bool Loading
        {
            get { return loading; }
            set
            {
                if (value == loading) return;
                loading = value;
                NotifyOfPropertyChange(() => Loading);
            }
        }

        public ChannelData SelectedChannelData
        {
            get { return selectedChannelData; }
            set
            {
                if (Equals(value, selectedChannelData)) return;
                selectedChannelData = value;
                NotifyOfPropertyChange(() => SelectedChannelData);
            }
        }


        public CollectionViewSource ViewSource { get; set; } = new CollectionViewSource();

        public async Task RefreshChannels()
        {
            refreshTimer.Stop();
            await monitorStreamsModel.RefreshChannels();
            refreshTimer.Start();
        }

        /// <summary> Loads the selected stream through livestreamer and displays a messagebox with the loading process details </summary>
        public void StartStream()
        {
            var selectedChannel = SelectedChannelData;
            if (selectedChannel == null || !selectedChannel.Live || Loading) return;

            // TODO - do a smarter find for the livestreamer exe and prompt on startup if it can not be found
            const string livestreamPath = @"C:\Program Files (x86)\Livestreamer\livestreamer.exe";

            // Fall back to source stream quality for non-partnered channels
            var streamQuality = (!selectedChannel.IsPartner &&
                                 settingsHandler.Settings.DefaultStreamQuality != StreamQuality.Source)
                                    ? StreamQuality.Source
                                    : settingsHandler.Settings.DefaultStreamQuality;

            string livestreamerArgs = $"http://www.twitch.tv/{selectedChannel.ChannelName}/ {streamQuality}";

            var messageBoxViewModel = ShowStreamLoadMessageBox(selectedChannel, settingsHandler.Settings.DefaultStreamQuality);

            // the process needs to be launched from its own thread so it doesn't lockup the UI
            Task.Run(() =>
            {
                var proc = new Process
                {
                    StartInfo =
                    {
                        FileName = livestreamPath,
                        Arguments = livestreamerArgs,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        CreateNoWindow = true,
                        UseShellExecute = false
                    },
                    EnableRaisingEvents = true
                };

                // see below for output handler
                proc.ErrorDataReceived +=
                    (sender, args) =>
                    {
                        if (args.Data != null) messageBoxViewModel.MessageText += Environment.NewLine + args.Data;
                    };
                proc.OutputDataReceived +=
                    (sender, args) =>
                    {
                        if (args.Data != null) messageBoxViewModel.MessageText += Environment.NewLine + args.Data;
                    };
                proc.Start();

                proc.BeginErrorReadLine();
                proc.BeginOutputReadLine();

                proc.WaitForExit();
                messageBoxViewModel.TryClose();
            });
        }

        public void RemoveChannel()
        {
            if (SelectedChannelData == null) return;

            monitorStreamsModel.RemoveChannel(SelectedChannelData);
        }

        private MessageBoxViewModel ShowStreamLoadMessageBox(ChannelData selectedChannel, StreamQuality streamQuality)
        {
            var messageBoxViewModel = new MessageBoxViewModel
            {
                DisplayName = $"Stream '{selectedChannel.ChannelName}'",
                MessageText = "Launching livestreamer..."
            };

            // Notify the user if the quality has been swapped back to source due to the channel not being partenered.
            if (!selectedChannel.IsPartner && streamQuality != StreamQuality.Source)
            {
                messageBoxViewModel.MessageText += Environment.NewLine + "[NOTE] Channel is not a twitch partner so falling back to Source quality";
            }

            var settings = new WindowSettingsBuilder().SizeToContent()
                                                      .WithWindowStyle(WindowStyle.ToolWindow)
                                                      .WithResizeMode(ResizeMode.NoResize)
                                                      .Create();

            windowManager.ShowWindow(messageBoxViewModel, null, settings);
            return messageBoxViewModel;
        }

        protected override async void OnActivate()
        {
            Loading = true;
            try
            {
                monitorStreamsModel.OnlineChannelsRefreshComplete += OnOnlineChannelsRefreshComplete;
                ViewSource.Source = monitorStreamsModel.FollowedChannels;
                ViewSource.SortDescriptions.Add(new SortDescription("Viewers", ListSortDirection.Descending));
                ViewSource.SortDescriptions.Add(new SortDescription("Live", ListSortDirection.Descending));

                await RefreshChannels();
                // hook up followed channels after our initial call so we can refresh immediately as needed
                monitorStreamsModel.FollowedChannels.CollectionChanged += FollowedChannelsOnCollectionChanged;
            }
            catch (Exception)
            {
                // TODO - show the error to the user and log it
            }

            Loading = false;
            base.OnActivate();
        }

        private void OnOnlineChannelsRefreshComplete(object sender, EventArgs eventArgs)
        {
            // We only really care about sorting online channels so this causes the sort descriptions to be applied immediately 
            ViewSource.View.Refresh();
        }

        private async void FollowedChannelsOnCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    ViewSource.View.Refresh();
                    break;
                case NotifyCollectionChangedAction.Remove:
                    break;
                case NotifyCollectionChangedAction.Replace:
                case NotifyCollectionChangedAction.Move:
                case NotifyCollectionChangedAction.Reset:
                    await RefreshChannels();
                    break;
            }
        }
    }
}
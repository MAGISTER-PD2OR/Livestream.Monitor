using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using Caliburn.Micro;
using Hardcodet.Wpf.TaskbarNotification;
using Livestream.Monitor.Core;
using Livestream.Monitor.Core.UI;
using Livestream.Monitor.Model.Monitoring;
using Livestream.Monitor.Views;
using MahApps.Metro.Controls;
using MahApps.Metro.Controls.Dialogs;
using Octokit;
using Application = System.Windows.Application;
using INavigationService = Livestream.Monitor.Core.INavigationService;

namespace Livestream.Monitor.ViewModels
{
    public class ShellViewModel : Conductor<IScreen>.Collection.OneActive, IHandle<ActivateScreen>
    {
        private readonly MainViewModel mainViewModel;
        private readonly INavigationService navigationService;
        private readonly IMonitorStreamsModel monitorStreamsModel;
        public const string TrayIconControlName = "TrayIcon";

        private readonly Version currentAppVersion;
        private WindowState windowState = WindowState.Normal;
        private TaskbarIcon taskbarIcon;
        private bool firstMinimize = true;
        private bool isSettingsOpen;

        public ShellViewModel()
        {
            if (!Execute.InDesignMode)
                throw new InvalidOperationException("Constructor only accessible from design time");

            Settings = new SettingsViewModel();
            ActiveItem = new MainViewModel();
        }

        public ShellViewModel(
            SettingsViewModel settingsViewModel,
            MainViewModel mainViewModel,
            IEventAggregator eventAggregator,
            INavigationService navigationService,
            IMonitorStreamsModel monitorStreamsModel)
        {
            Settings = settingsViewModel ?? throw new ArgumentNullException(nameof(settingsViewModel));
            this.mainViewModel = mainViewModel ?? throw new ArgumentNullException(nameof(mainViewModel));
            this.navigationService = navigationService ?? throw new ArgumentNullException(nameof(navigationService));
            this.monitorStreamsModel = monitorStreamsModel ?? throw new ArgumentNullException(nameof(monitorStreamsModel));

            ActiveItem = mainViewModel;

            eventAggregator.Subscribe(this);
            Settings.ActivateWith(this);

            currentAppVersion = GetType().Assembly.GetName().Version;
            DisplayName = $"LIVESTREAM MONITOR V{currentAppVersion.Major}.{currentAppVersion.Minor}.{currentAppVersion.Build}";
#if DEBUG
            // helps to know that we're running the debug rather than release version of the exe
            DisplayName += " (DEBUG)";
#endif
        }

        public override string DisplayName { get; set; }

        public SettingsViewModel Settings { get; set; }

        public WindowState WindowState
        {
            get { return windowState; }
            set
            {
                if (value == windowState) return;
                windowState = value;
                NotifyOfPropertyChange(() => WindowState);
                if (windowState == WindowState.Minimized)
                    WindowMinimized();
            }
        }

        public bool IsSettingsOpen
        {
            get { return isSettingsOpen; }
            set
            {
                if (value == isSettingsOpen) return;
                isSettingsOpen = value;
                NotifyOfPropertyChange(() => IsSettingsOpen);
            }
        }

        public void ShowWindow()
        {
            Application.Current.MainWindow.Show();
            WindowState = WindowState.Normal;
            Application.Current.MainWindow.Activate();
        }

        public void ShowSettings()
        {
            IsSettingsOpen = true;
        }

        public void GotoTopStreams()
        {
            navigationService.NavigateTo<TopStreamsViewModel>();
        }

        public void GotoVodViewer()
        {
            navigationService.NavigateTo<VodListViewModel>();
        }

        public void Handle(ActivateScreen message)
        {
            if (ActiveItem.GetType() == message.Screen.GetType())
                return;

            if (ActiveItem != mainViewModel)
                DeactivateItem(ActiveItem, true);

            Items.Add(message.Screen);
            ActivateItem(message.Screen);
        }

        protected override async void OnViewLoaded(object view)
        {
            if (Execute.InDesignMode) return;

            taskbarIcon = Application.Current.MainWindow.FindChild<TaskbarIcon>(TrayIconControlName);
            if (!Debugger.IsAttached) await CheckForNewVersion();
            await InitializeMonitorStreamsModel();
            base.OnViewLoaded(view);
        }

        protected override void OnDeactivate(bool close)
        {
            taskbarIcon?.Dispose(); // this will be cleaned up on app close anyway but this is a bit cleaner
            base.OnDeactivate(close);
        }

        private void WindowMinimized()
        {
            Application.Current.MainWindow.Hide();

            // Prevent minimizing notification windows
            // I just don't want notifications to minimize in the first place but can't find any other way to do this...
            foreach (var window in Application.Current.Windows)
            {
                var notificationWindow = window as NotificationView;
                notificationWindow?.Show();
            }

            if (firstMinimize) // only show the notification one time
            {
                taskbarIcon.ShowBalloonTip("Livestream Monitor", "Livestream Monitor minimized to tray", BalloonIcon.Info);
                firstMinimize = false;
            }
        }

        private async Task InitializeMonitorStreamsModel()
        {
            var dialogController = await this.ShowProgressAsync("Initializing", "Initializing App...");
            try
            {
                await monitorStreamsModel.Initialize();
            }
            catch (Exception ex)
            {
                if (dialogController.IsOpen) await dialogController.CloseAsync();
                await this.ShowMessageAsync("Error", $"An error occured initializing the app.{Environment.NewLine}{ex.Message}");
            }

            if (dialogController.IsOpen) await dialogController.CloseAsync();
        }

        private async Task CheckForNewVersion()
        {
            var githubClient =
                new GitHubClient(new ProductHeaderValue("Livestream.Monitor",
                    $"{currentAppVersion.Major}.{currentAppVersion.Minor}.{currentAppVersion.Build}"));

            const string githubRepository = "Livestream.Monitor";
            const string githubUsername = "laurencee";

            var dialogController = await this.ShowProgressAsync("Update Check", "Checking for newer version...");
            try
            {
                var releases = await githubClient.Repository.Release.GetAll(githubUsername, githubRepository);
                var latestRelease = releases.FirstOrDefault();
                if (latestRelease != null)
                {
                    if (IsNewerVersion(latestRelease))
                    {
                        await dialogController.CloseAsync();
                        var dialogResult = await this.ShowMessageAsync("New version available",
                            "There is a newer version available. Go to download page?",
                            MessageDialogStyle.AffirmativeAndNegative);

                        if (dialogResult == MessageDialogResult.Affirmative)
                        {
                            Process.Start(latestRelease.HtmlUrl);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                if (dialogController.IsOpen) await dialogController.CloseAsync();
                await this.ShowMessageAsync("Error", $"An error occured while checking for a newer version.{Environment.NewLine}{ex.Message}");
            }

            if (dialogController.IsOpen) await dialogController.CloseAsync();
        }

        private bool IsNewerVersion(Release latestRelease)
        {
            if (string.IsNullOrWhiteSpace(latestRelease?.TagName)) return false;

            try
            {
                var releaseVersion = new Version(latestRelease.TagName);
                return releaseVersion > currentAppVersion;
            }
            catch
            {
                // failed to convert the tagname to a version for some reason
                return false;
            }
        }
    }
}
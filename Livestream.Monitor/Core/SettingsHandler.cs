﻿using System;
using System.ComponentModel;
using System.IO;
using System.Windows;
using MahApps.Metro;
using Newtonsoft.Json;

namespace Livestream.Monitor.Core
{
    public class SettingsHandler : ISettingsHandler
    {
        public const string SettingsFileName = "settings.json";
        private bool settingsLoaded;
        private Settings settings;

        public Settings Settings
        {
            get
            {
                if (!settingsLoaded) LoadSettings();
                return settings;
            }
        }

        private void LoadSettings()
        {
            if (settingsLoaded) return;
            try
            {
                bool saveSettings;
                if (File.Exists(SettingsFileName))
                {
                    settings = JsonConvert.DeserializeObject<Settings>(File.ReadAllText(SettingsFileName));
                }

                if (settings == null)
                {
                    settings = new Settings() { SettingsVersion = Settings.CurrentSettingsVersion };
                    saveSettings = true;
                }
                else
                {
                    saveSettings = ExcludeNotifyConverter.SaveRequired;
                }

                if (settings.SettingsVersion < Settings.CurrentSettingsVersion)
                {
                    if (settings.SettingsVersion == 0)
                    {
                        settings.CheckForNewVersions = true;
                    }
                    
                    settings.SettingsVersion = Settings.CurrentSettingsVersion;
                    saveSettings = true;
                }
                
                // try to set a nice default value for the chat command line
                if (settings.ChatCommandLine == null)
                {
                    settings.ChatCommandLine = Settings.DEFAULT_CHROME_COMMAND_LINE;
                    saveSettings = true;
                }

                if (string.IsNullOrWhiteSpace(settings.LivestreamerFullPath))
                {
                    if (File.Exists(Settings.DEFAULT_STREAMLINK_FULL_PATH))
                        settings.LivestreamerFullPath = Settings.DEFAULT_STREAMLINK_FULL_PATH;
                    else if (File.Exists(Settings.DEFAULT_LIVESTREAMER_FULL_PATH))
                        settings.LivestreamerFullPath = Settings.DEFAULT_LIVESTREAMER_FULL_PATH;
                    else
                        settings.LivestreamerFullPath = Settings.DEFAULT_STREAMLINK_FULL_PATH;

                    saveSettings = true;
                }

                if (saveSettings) SaveSettings();

                settings.PropertyChanged += SettingsOnPropertyChanged;
                settings.ExcludeFromNotifying.CollectionChanged += (sender, args) => SaveSettings();
                settingsLoaded = true;
            }
            catch (Exception)
            {
                settings = new Settings();
                // log error
            }
        }

        private void SettingsOnPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(settings.MetroThemeBaseColour))
            {
                var currentTheme = ThemeManager.DetectAppStyle(Application.Current.MainWindow);
                var baseTheme = ThemeManager.GetAppTheme(settings.MetroThemeBaseColour.ToString());
                ChangeTheme(baseTheme, currentTheme.Item2);
                SaveSettings();
            }
            else if (e.PropertyName == nameof(settings.MetroThemeAccentColour))
            {
                var currentTheme = ThemeManager.DetectAppStyle(Application.Current.MainWindow);
                var accent = ThemeManager.GetAccent(settings.MetroThemeAccentColour.ToString());
                ChangeTheme(currentTheme.Item1, accent);
                SaveSettings();
            }
        }

        private void ChangeTheme(AppTheme baseColour, Accent accentColour)
        {
            // change the theme for the main window so the update is immediate
            ThemeManager.ChangeAppStyle(Application.Current.MainWindow, accentColour, baseColour);

            // change the default theme for all future windows opened
            ThemeManager.ChangeAppStyle(Application.Current, accentColour, baseColour);
        }

        public void SaveSettings()
        {
            try
            {
                File.WriteAllText(SettingsFileName, JsonConvert.SerializeObject(settings, Formatting.Indented));
            }
            catch (Exception)
            {
                // can't do much...
            }
        }
    }
}

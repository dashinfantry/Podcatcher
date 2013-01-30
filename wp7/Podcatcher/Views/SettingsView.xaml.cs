﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using Microsoft.Phone.Controls;
using Podcatcher.ViewModels;
using System.Diagnostics;
using System.IO.IsolatedStorage;

namespace Podcatcher.Views
{
    public partial class SettingsView : PhoneApplicationPage
    {
        private static bool initialized = false;
        private SettingsModel m_settings = null;
        private String m_podcastUsage = null;
        private List<String> m_fileList = null;

        public SettingsView()
        {
            InitializeComponent();
            initialized = true;
        }

        protected override void OnNavigatedTo(System.Windows.Navigation.NavigationEventArgs e)
        {

            m_settings = PodcastSqlModel.getInstance().settings();
            this.DataContext = m_settings;
            this.DeleteEpisodeThreshold.Value = m_settings.ListenedThreashold;
            this.DeleteThresholdPercent.Text = String.Format("{0} %", this.DeleteEpisodeThreshold.Value.ToString());
        }

        protected override void OnNavigatedFrom(System.Windows.Navigation.NavigationEventArgs e)
        {
            PodcastSqlModel.getInstance().SubmitChanges();
        }
        
        private void DeleteEpisodeThreshold_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (!initialized
                || this.DeleteEpisodeThreshold == null)
            {
                return;
            }

            this.DeleteThresholdPercent.Text = String.Format("{0} %", ((int)e.NewValue).ToString());
            m_settings.ListenedThreashold = (int)e.NewValue;
        }

        private void NavigationPivot_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Find out when we are in the Usage pivot.
            if (this.NavigationPivot.SelectedIndex == 3)
            {
                if (m_podcastUsage == null)
                {
                    m_podcastUsage = getUsageString();
                }

                this.UsageText.Text = m_podcastUsage;
            }
        }

        private String getUsageString()
        {
            long usedBytes = 0;
            using (IsolatedStorageFile storage = IsolatedStorageFile.GetUserStoreForApplication())
            {
                m_fileList = storage.GetFileNames(App.PODCAST_DL_DIR + "/*").ToList<String>();
                IsolatedStorageFileStream fileStream = null;
                foreach (String filename in m_fileList)
                {
                    try
                    {
                        fileStream = storage.OpenFile(App.PODCAST_DL_DIR + "/" + filename, System.IO.FileMode.Open);
                        Debug.WriteLine("File: {0}, Size: {1}", filename, fileStream.Length);
                        usedBytes += fileStream.Length;
                        fileStream.Close();
                    }
                    catch (IsolatedStorageException)
                    {
                        App.showNotificationToast("Notice: Could not read all files.");
                    }
                }
            }

            String units = "GB";
            // Check if we are over a gigabyte
            if ((usedBytes >> 30) != 0)
            {
                usedBytes >>= 30;
            }
            else
            {
                // Then convert to megabytes
                units = "MB";
                usedBytes >>= 20;
            }

            return String.Format("{0} {1}", usedBytes, units);
        }

        private void DeleteAllButton_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            if (MessageBox.Show("Are you sure you want to delete all downloaded podcasts?",
                    "Delete all?",
                    MessageBoxButton.OKCancel) == MessageBoxResult.Cancel)
            {
                return;
            }

            List<PodcastEpisodeModel> episodes = PodcastSqlModel.getInstance().allEpisodes();
            foreach (PodcastEpisodeModel episode in episodes)
            {
                episode.deleteDownloadedEpisode();
            }

            this.UsageText.Text = getUsageString();
        }
    }
}
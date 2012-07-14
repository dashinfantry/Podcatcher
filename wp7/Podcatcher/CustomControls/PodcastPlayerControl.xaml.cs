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
using Podcatcher.ViewModels;
using Microsoft.Phone.Controls;
using System.Diagnostics;
using System.Windows.Media.Imaging;
using Microsoft.Phone.BackgroundAudio;
using System.IO.IsolatedStorage;
using System.Windows.Threading;

namespace Podcatcher
{
    public partial class PodcastPlayerControl : UserControl
    {
        public event EventHandler PodcastPlayerStarted;
        
        public PodcastPlayerControl()
        {
            InitializeComponent();

            showNoPlayerLayout();

            m_instance = this;

            m_playButtonBitmap = new BitmapImage(new Uri("/Images/play.png", UriKind.Relative));
            m_pauseButtonBitmap = new BitmapImage(new Uri("/Images/pause.png", UriKind.Relative));

            BackgroundAudioPlayer.Instance.PlayStateChanged += new EventHandler(PlayStateChanged);
            m_screenUpdateTimer.Interval = new TimeSpan(0, 0, 0, 0, 500); // Fire the timer half a every second.
            m_screenUpdateTimer.Tick += new EventHandler(m_screenUpdateTimer_Tick);

            if (BackgroundAudioPlayer.Instance.Track != null)
            {
                showPlayerLayout();
            }

            // If we have an episodeId stored in local cache, this means we have that episode playing.
            // Hence, here we need to reload the episode data from the SQL. 
            m_appSettings = IsolatedStorageSettings.ApplicationSettings;
            if (m_appSettings.Contains("episodeId"))
            {
                int episodeId = (int)m_appSettings["episodeId"];
                m_currentEpisode = PodcastSqlModel.getInstance().episodeForEpisodeId(episodeId);

                if (m_currentEpisode == null)
                {
                    // Episode not in SQL anymore (maybe it was deleted). So clear up a bit...
                    m_appSettings.Remove("episodeId");
                    return;
                }

                setupPlayerUIContent(m_currentEpisode);
            }

        }

        public static PodcastPlayerControl getIntance()
        {
            return m_instance;
        }

        /************************************* Private implementation *******************************/

        private static PodcastPlayerControl m_instance;
        private BitmapImage m_playButtonBitmap;
        private BitmapImage m_pauseButtonBitmap;
        private static PodcastEpisodeModel m_currentEpisode = null;
        private bool settingSliderFromPlay;
        private IsolatedStorageSettings m_appSettings;
        private DispatcherTimer m_screenUpdateTimer = new DispatcherTimer();

        internal void playEpisode(PodcastEpisodeModel episodeModel)
        {
            if (m_currentEpisode != null) { 
                m_currentEpisode.EpisodeState = PodcastEpisodeModel.EpisodeStateVal.Playable;
            }
            
            m_currentEpisode = episodeModel;
            m_appSettings.Remove("episodeId");
            m_appSettings.Add("episodeId", m_currentEpisode.PodcastId);

            setupPlayerUIContent(m_currentEpisode);
            showPlayerLayout();


            BackgroundAudioPlayer.Instance.Track = getAudioTrackForEpisode(m_currentEpisode);
            BackgroundAudioPlayer.Instance.Play();
            this.PlayButtonImage.Source = m_pauseButtonBitmap;

            PodcastPlayerStarted(this, new EventArgs());
        }

        private void setupPlayerUIContent(PodcastEpisodeModel currentEpisode)
        {
            this.PodcastLogo.Source = currentEpisode.PodcastSubscription.PodcastLogo;
            this.PodcastEpisodeName.Text = currentEpisode.EpisodeName;
        }

        private void showNoPlayerLayout()
        {
            this.NoPlayingLayout.Visibility = Visibility.Visible;
            this.PlayingLayout.Visibility = Visibility.Collapsed;
        }

        private void showPlayerLayout()
        {
            this.NoPlayingLayout.Visibility = Visibility.Collapsed;
            this.PlayingLayout.Visibility = Visibility.Visible;
        }

        void PlayStateChanged(object sender, EventArgs e)
        {
            switch (BackgroundAudioPlayer.Instance.PlayerState)
            {
                case PlayState.Playing:
                    // Player is playing
                    Debug.WriteLine("Podcast player is playing...");
                    this.TotalDurationText.Text = BackgroundAudioPlayer.Instance.Track.Duration.ToString("hh\\:mm\\:ss");
                    m_currentEpisode.EpisodeState = PodcastEpisodeModel.EpisodeStateVal.Playing;

                    m_screenUpdateTimer.Start();
                    break;

                case PlayState.Paused:
                    // Player is on pause
                    Debug.WriteLine("Podcast player is paused...");
                    m_currentEpisode.EpisodeState = PodcastEpisodeModel.EpisodeStateVal.Paused;

                    // Clear CompositionTarget.Rendering 
                    m_screenUpdateTimer.Stop();
                    break;

                case PlayState.Stopped:
                case PlayState.Shutdown:
                    m_screenUpdateTimer.Stop();
                    m_appSettings.Remove("episodeId");
                    break;

            }
        }


        private void rewButtonClicked(object sender, System.Windows.Input.GestureEventArgs e)
        {
            BackgroundAudioPlayer.Instance.Rewind();
        }

        private void playButtonClicked(object sender, System.Windows.Input.GestureEventArgs e)
        {
            if (BackgroundAudioPlayer.Instance.PlayerState == PlayState.Playing)
            {
                BackgroundAudioPlayer.Instance.Pause();
                this.PlayButtonImage.Source = m_playButtonBitmap;
            }
            else
            {
                BackgroundAudioPlayer.Instance.Play();
                this.PlayButtonImage.Source = m_pauseButtonBitmap;
            }
        }

        private void ffButtonClicked(object sender, System.Windows.Input.GestureEventArgs e)
        {
            BackgroundAudioPlayer.Instance.FastForward();
        }

        private AudioTrack getAudioTrackForEpisode(PodcastEpisodeModel m_currentEpisode)
        {
            return new AudioTrack(new Uri(m_currentEpisode.EpisodeFile, UriKind.Relative),
                        m_currentEpisode.EpisodeName,
                        m_currentEpisode.PodcastSubscription.PodcastName,
                        "",
                        new Uri(m_currentEpisode.PodcastSubscription.PodcastLogoLocalLocation, UriKind.Relative));
        }

        void m_screenUpdateTimer_Tick(object sender, EventArgs e)
        {
            TimeSpan position = TimeSpan.Zero;
            TimeSpan duration = TimeSpan.Zero;

            duration = BackgroundAudioPlayer.Instance.Track.Duration;
            position = BackgroundAudioPlayer.Instance.Position;

            this.CurrentPositionText.Text = position.ToString("hh\\:mm\\:ss");

            settingSliderFromPlay = true;
            if (duration.Ticks > 0)
            {
                this.PositionSlider.Value = (double)position.Ticks / duration.Ticks;
            }
            else
            {
                this.PositionSlider.Value = 0;
            }
            settingSliderFromPlay = false;
        }

        private void PositionSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (!settingSliderFromPlay)
            {
                if (e.NewValue <= 0.1)
                {
                    return;
                }

                AudioTrack audioTrack = null;
                TimeSpan duration = TimeSpan.Zero;

                try
                {
                    // Sometimes these property accesses will raise exceptions
                    audioTrack = BackgroundAudioPlayer.Instance.Track;

                    if (audioTrack != null)
                        duration = audioTrack.Duration;
                }
                catch
                {
                }

                if (audioTrack == null)
                    return;

                long ticks = (long)(e.NewValue * duration.Ticks);
                BackgroundAudioPlayer.Instance.Position = new TimeSpan(ticks);
            }
        }
    }
}
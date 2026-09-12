using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Windows;
using System.Windows.Controls;
using LibVLCSharp.Shared;

namespace IpTvApp
{
    public partial class MainWindow : Window
    {
        private static readonly HttpClient _httpClient = CreateHttpClient();
        private LibVLC _libVLC;
        private MediaPlayer _mediaPlayer;

        public MainWindow()
        {
            InitializeComponent();
            Core.Initialize(); // loads the native VLC libraries, must run once before anything else
            _libVLC = new LibVLC();
            _mediaPlayer = new MediaPlayer(_libVLC);
            VideoView.MediaPlayer = _mediaPlayer;
        }
        
        private static HttpClient CreateHttpClient()
        {
            var client = new HttpClient();
            //client.DefaultRequestHeaders.UserAgent.ParseAdd("VLC/3.0.18 LibVLC/3.0.18");
            client.Timeout = TimeSpan.FromSeconds(300); // give slow playlist generation more room
            return client;
        }

        private async void LoadButton_Click(object sender, RoutedEventArgs e)
        {
            var profile = new PlaylistProfile
            {
                Name = PlaylistNameBox.Text.Trim(),
                ServerUrl = PlaylistUrlBox.Text.Trim(),
                Username = UsernameBox.Text.Trim(),
                Password = PasswordInput.Text
            };

            string requestUrl = profile.BuildChannelsApiUrl();
            Console.WriteLine(requestUrl);
            
            try
            {
                LoadButton.IsEnabled = false;
                StatusText.Text = "Loading playlist...";

                string content = await _httpClient.GetStringAsync(requestUrl);
                List<Channel> channels = XtreamApiClient.Parse(content, profile);

                ChannelList.ItemsSource = channels;
                StatusText.Text = $"Loaded {channels.Count} channels.";
            }
            catch (HttpRequestException ex)
            {
                StatusText.Text = "Failed to load.";
                MessageBox.Show($"HTTP error: {ex.Message}\nStatus: {ex.StatusCode}");
            }
            catch (TaskCanceledException)
            {
                StatusText.Text = "Failed to load.";
                MessageBox.Show("The request timed out. The server may be slow or unresponsive.");
            }
            finally
            {
                LoadButton.IsEnabled = true;
            }
            
        }
        
        private void ChannelList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ChannelList.SelectedItem is Channel channel)
            {
                var media = new Media(_libVLC, channel.Url, FromType.FromLocation);
                _mediaPlayer.Play(media);
                PlayPauseButton.Content = "Pause";
            }
        }
        
        private void PlayPauseButton_Click(object sender, RoutedEventArgs e)
        {
            if (_mediaPlayer.IsPlaying)
            {
                _mediaPlayer.Pause();
                PlayPauseButton.Content = "Play";
            }
            else
            {
                _mediaPlayer.Play();
                PlayPauseButton.Content = "Pause";
            }
        }

        private void StopButton_Click(object sender, RoutedEventArgs e)
        {
            _mediaPlayer.Stop();
            PlayPauseButton.Content = "Play";
        }

        private void VolumeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (_mediaPlayer != null)
            {
                _mediaPlayer.Volume = (int)e.NewValue;
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            _mediaPlayer?.Stop();
            _mediaPlayer?.Dispose();
            _libVLC?.Dispose();
            base.OnClosed(e);
        }
    }
}
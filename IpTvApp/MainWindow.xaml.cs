using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using LibVLCSharp.Shared;
using Microsoft.EntityFrameworkCore;

namespace IpTvApp
{
    public partial class MainWindow : Window
    {
        private static readonly HttpClient _httpClient = CreateHttpClient();
        private readonly AppDbContext _db = new();
        private ObservableCollection<PlaylistProfile> _profiles;
        private PlaylistProfile _currentProfile;

        private List<Channel> _allChannels = new();
        private List<string> _allCategories = new();

        private LibVLC _libVLC;
        private MediaPlayer _mediaPlayer;

        public MainWindow()
        {
            InitializeComponent();
            Core.Initialize();
            _libVLC = new LibVLC();
            _mediaPlayer = new MediaPlayer(_libVLC);
            VideoView.MediaPlayer = _mediaPlayer;

            LoadProfilesFromDatabase();
        }

        private static HttpClient CreateHttpClient()
        {
            var client = new HttpClient();
            client.Timeout = TimeSpan.FromSeconds(300);
            return client;
        }

        private void LoadProfilesFromDatabase()
        {
            var profiles = _db.Profiles.Include(p => p.Channels).ToList();
            _profiles = new ObservableCollection<PlaylistProfile>(profiles);
            ProfileComboBox.ItemsSource = _profiles;

            if (_profiles.Count > 0)
            {
                ProfileComboBox.SelectedIndex = 0;
            }
        }

        private void ProfileComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ProfileComboBox.SelectedItem is not PlaylistProfile profile) return;

            _currentProfile = profile;
            PlaylistNameBox.Text = profile.Name;
            PlaylistUrlBox.Text = profile.ServerUrl;
            UsernameBox.Text = profile.Username;
            PasswordInput.Text = profile.Password;

            SetChannelData(profile.Channels);

            StatusText.Text = profile.LastUpdated is { } updated
                ? $"Showing {profile.Channels.Count} channels (last updated {updated:g})"
                : "No cached channels yet — click Load Playlist.";
        }

        // Central place that (re)builds the category list whenever the channel set changes
        private void SetChannelData(List<Channel> channels)
        {
            _allChannels = channels;

            _allCategories = _allChannels
                .Select(c => c.Category)
                .Distinct()
                .OrderBy(c => c)
                .ToList();

            CategoryList.ItemsSource = _allCategories;
            ChannelList.ItemsSource = null; // clear channel pane until a category is picked
        }

        private void CategorySearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            string filter = CategorySearchBox.Text.Trim();

            CategoryList.ItemsSource = string.IsNullOrEmpty(filter)
                ? _allCategories
                : _allCategories
                    .Where(c => c.Contains(filter, StringComparison.OrdinalIgnoreCase))
                    .ToList();
        }

        private void CategoryList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (CategoryList.SelectedItem is not string selectedCategory) return;

            ChannelList.ItemsSource = _allChannels
                .Where(c => c.Category == selectedCategory)
                .ToList();
        }

        private void NewProfileButton_Click(object sender, RoutedEventArgs e)
        {
            var newProfile = new PlaylistProfile { Name = "New Profile" };
            _profiles.Add(newProfile);
            ProfileComboBox.SelectedItem = newProfile;
        }

        private void SaveProfileButton_Click(object sender, RoutedEventArgs e)
        {
            if (_currentProfile == null) return;

            _currentProfile.Name = PlaylistNameBox.Text.Trim();
            _currentProfile.ServerUrl = PlaylistUrlBox.Text.Trim().TrimEnd('/');
            _currentProfile.Username = UsernameBox.Text.Trim();
            _currentProfile.Password = PasswordInput.Text;

            if (_currentProfile.Id == 0)
            {
                _db.Profiles.Add(_currentProfile);
            }

            _db.SaveChanges();
            ProfileComboBox.Items.Refresh();
            StatusText.Text = "Profile saved.";
        }

        private void DeleteProfileButton_Click(object sender, RoutedEventArgs e)
        {
            if (_currentProfile == null) return;

            _db.Profiles.Remove(_currentProfile);
            _db.SaveChanges();

            _profiles.Remove(_currentProfile);
            _currentProfile = null;
            SetChannelData(new List<Channel>());
            StatusText.Text = "Profile deleted.";
        }

        private async void LoadButton_Click(object sender, RoutedEventArgs e)
        {
            if (_currentProfile == null)
            {
                MessageBox.Show("Create or select a profile first.");
                return;
            }

            _currentProfile.Name = PlaylistNameBox.Text.Trim();
            _currentProfile.ServerUrl = PlaylistUrlBox.Text.Trim().TrimEnd('/');
            _currentProfile.Username = UsernameBox.Text.Trim();
            _currentProfile.Password = PasswordInput.Text;

            string channelsUrl = _currentProfile.BuildChannelsApiUrl();
            string categoriesUrl = _currentProfile.BuildCategoriesApiUrl();

            try
            {
                LoadButton.IsEnabled = false;
                StatusText.Text = "Loading playlist...";

                string channelsJson = await _httpClient.GetStringAsync(channelsUrl);
                string categoriesJson = await _httpClient.GetStringAsync(categoriesUrl);

                var channels = XtreamApiClient.Parse(channelsJson, categoriesJson, _currentProfile);

                _db.Channels.RemoveRange(_currentProfile.Channels);
                _currentProfile.Channels = channels;
                _currentProfile.LastUpdated = DateTime.Now;

                await _db.SaveChangesAsync();

                SetChannelData(_currentProfile.Channels);
                StatusText.Text = $"Loaded {channels.Count} channels across {_allCategories.Count} categories.";
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
            catch (JsonException ex)
            {
                StatusText.Text = "Failed to load.";
                MessageBox.Show($"Unexpected response format: {ex.Message}");
            }
            catch (Exception ex)
            {
                StatusText.Text = "Failed to load.";
                MessageBox.Show($"Unexpected error: {ex.Message}");
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
                media.AddOption(":http-user-agent=VLC/3.0.18 LibVLC/3.0.18");
                StatusText.Text = $"Connecting to {channel.Name}...";
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
            _db?.Dispose();
            base.OnClosed(e);
        }
    }
}
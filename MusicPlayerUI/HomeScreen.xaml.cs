﻿using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using MusicPlayerEntities;
using MusicPlayerRepositories;
using MusicPlayerServices;

namespace MusicPlayerUI
{
    public partial class HomeScreen : Window
    {
        private UserService _userService;
        private SongService _songService;
        private ArtistService _artistService;
        private int _currentUserId = 1;
        private DispatcherTimer progressTimer;
        private Artist _selectedArtist;

        public HomeScreen()
        {
            InitializeComponent();
            _userService = new UserService();
            _songService = new SongService();
            _artistService = new ArtistService();

            // Show the favorites view by default
            ShowView(FavoritesView);
            FavoritesButton.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#3E3E3E"));

            LoadFavorites();
            InitializeProgressTimer();

            // Set event handler for window closing to clean up resources
            this.Closing += HomeScreen_Closing;
        }

        #region View Navigation

        private void ShowView(UIElement viewToShow)
        {
            // Reset all navigation button backgrounds
            HomeButton.Background = new SolidColorBrush(Colors.Transparent);
            MyMusicButton.Background = new SolidColorBrush(Colors.Transparent);
            FavoritesButton.Background = new SolidColorBrush(Colors.Transparent);

            // Hide all views
            HomeView.Visibility = Visibility.Collapsed;
            MyMusicView.Visibility = Visibility.Collapsed;
            FavoritesView.Visibility = Visibility.Collapsed;
            PlaylistView.Visibility = Visibility.Collapsed;
            ArtistDetailView.Visibility = Visibility.Collapsed;

            // Show the requested view
            viewToShow.Visibility = Visibility.Visible;
        }

        private void HomeButton_Click(object sender, RoutedEventArgs e)
        {
            ShowView(HomeView);
            HomeButton.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#3E3E3E"));
        }

        private void MyMusicButton_Click(object sender, RoutedEventArgs e)
        {
            ShowView(MyMusicView);
            MyMusicButton.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#3E3E3E"));

            // Load artists if we haven't already
            if (ArtistsPanel.Children.Count == 0)
            {
                LoadArtists();
            }
        }

        private void FavoritesButton_Click(object sender, RoutedEventArgs e)
        {
            ShowView(FavoritesView);
            FavoritesButton.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#3E3E3E"));
        }

        private void ShowArtistDetails(Artist artist)
        {
            _selectedArtist = artist;
            ShowView(ArtistDetailView);

            // Set artist details
            ArtistNameText.Text = artist.Name;
            ArtistBioText.Text = string.IsNullOrEmpty(artist.Bio) ?
                "No biography available for this artist." : artist.Bio;

            // Set artist initial for the avatar
            if (!string.IsNullOrEmpty(artist.Name))
            {
                ArtistInitial.Text = artist.Name[0].ToString().ToUpper();
            }
            else
            {
                ArtistInitial.Text = "A";
            }

            // Load artist's songs
            LoadArtistSongs(artist.ArtistId);
        }

        private void PlaylistsListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var selectedPlaylist = PlaylistsListBox.SelectedItem as ListBoxItem;
            if (selectedPlaylist != null)
            {
                // Get the playlist ID from the Tag property
                if (int.TryParse(selectedPlaylist.Tag.ToString(), out int playlistId))
                {
                    LoadPlaylist(playlistId);
                    ShowView(PlaylistView);
                }
            }
        }

        #endregion

        #region Progress Timer

        private void InitializeProgressTimer()
        {
            progressTimer = new DispatcherTimer();
            progressTimer.Interval = TimeSpan.FromMilliseconds(500); // Update every 500ms
            progressTimer.Tick += ProgressTimer_Tick;
            progressTimer.Start();
        }

        private void ProgressTimer_Tick(object sender, EventArgs e)
        {
            UpdateProgressBar();
        }

        private void UpdateProgressBar()
        {
            if (_songService.GetPlaybackState() == NAudio.Wave.PlaybackState.Playing)
            {
                float position = _songService.GetCurrentPosition();
                float duration = _songService.GetCurrentDuration();

                if (duration > 0)
                {
                    // Calculate percentage and update progress bar
                    double progressPercentage = (position / duration) * 100;
                    SongProgressBar.Value = progressPercentage;
                }
            }
        }

        private void HomeScreen_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            // Stop timer
            if (progressTimer != null)
            {
                progressTimer.Stop();
            }

            // Clean up player resources when window is closed
            _songService.Dispose();
        }

        #endregion

        #region Favorites

        private void LoadFavorites()
        {
            try
            {
                Favorite_List.Items.Clear();

                List<Song> favorites = _songService.GetAll();
                if (favorites == null) { return; }

                int index = 1;
                foreach (Song song in favorites)
                {
                    // Create a song view model for binding
                    var songVM = new SongViewModel
                    {
                        Index = index.ToString(),
                        Title = song.Title,
                        Artist = song.Artist?.Name ?? "Unknown Artist",
                        Album = song.Album?.Title ?? "Unknown Album",
                        Duration = FormatDuration(song.Duration),
                        SongData = song
                    };

                    Favorite_List.Items.Add(songVM);
                    index++;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading favorites: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Favorite_List_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var selectedItem = Favorite_List.SelectedItem as SongViewModel;

            if (selectedItem != null)
            {
                song_name.Text = selectedItem.Title;
                song_artist.Text = selectedItem.Artist;
            }
        }

        #endregion

        #region Artists

        private void LoadArtists()
        {
            try
            {
                ArtistsPanel.Children.Clear();
                List<Artist> artists = _songService.GetAllArtists();

                foreach (var artist in artists)
                {
                    // Create an artist card
                    Border artistCard = CreateArtistCard(artist);
                    ArtistsPanel.Children.Add(artistCard);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading artists: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private Border CreateArtistCard(Artist artist)
        {
            // Main container
            Border card = new Border
            {
                Width = 180,
                Height = 220,
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#252525")),
                CornerRadius = new CornerRadius(8),
                Margin = new Thickness(10)
            };

            // Add a mouse click event to show artist details
            card.MouseLeftButtonUp += (s, e) => ShowArtistDetails(artist);

            // Card content
            StackPanel content = new StackPanel
            {
                Margin = new Thickness(10)
            };

            // Artist Avatar/Circle
            Border avatarBorder = new Border
            {
                Width = 150,
                Height = 150,
                CornerRadius = new CornerRadius(75), // Makes it a circle
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#333333")),
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 0, 0, 10)
            };

            // Initial letter
            TextBlock initial = new TextBlock
            {
                Text = !string.IsNullOrEmpty(artist.Name) ? artist.Name[0].ToString().ToUpper() : "A",
                FontSize = 70,
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1DB954")),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };

            avatarBorder.Child = initial;
            content.Children.Add(avatarBorder);

            // Artist Name
            TextBlock nameText = new TextBlock
            {
                Text = artist.Name,
                Foreground = new SolidColorBrush(Colors.White),
                FontWeight = FontWeights.SemiBold,
                FontSize = 16,
                TextWrapping = TextWrapping.Wrap,
                TextAlignment = TextAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center
            };
            content.Children.Add(nameText);

            card.Child = content;
            return card;
        }

        private void LoadArtistSongs(int artistId)
        {
            try
            {
                ArtistSongs_List.Items.Clear();
                List<Song> songs = _songService.GetSongsByArtist(artistId);

                // Update artist info text
                int albumCount = CountUniqueAlbums(songs);
                ArtistInfoText.Text = $"{songs.Count} songs • {albumCount} albums";

                int index = 1;
                foreach (Song song in songs)
                {
                    // Create a song view model for binding
                    var songVM = new SongViewModel
                    {
                        Index = index.ToString(),
                        Title = song.Title,
                        Artist = song.Artist?.Name ?? "Unknown Artist",
                        Album = song.Album?.Title ?? "Unknown Album",
                        Duration = FormatDuration(song.Duration),
                        PlayCount = song.PlayCount?.ToString() ?? "0",
                        SongData = song
                    };

                    ArtistSongs_List.Items.Add(songVM);
                    index++;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading artist songs: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private int CountUniqueAlbums(List<Song> songs)
        {
            HashSet<int?> uniqueAlbumIds = new HashSet<int?>();
            foreach (var song in songs)
            {
                if (song.AlbumId.HasValue)
                {
                    uniqueAlbumIds.Add(song.AlbumId);
                }
            }
            return uniqueAlbumIds.Count;
        }

        private void ArtistSearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            // We'll implement the search when the Search button is clicked
        }

        private void SearchArtists_Click(object sender, RoutedEventArgs e)
        {
            string searchTerm = ArtistSearchBox.Text;
            if (string.IsNullOrWhiteSpace(searchTerm))
            {
                LoadArtists(); // Load all artists
                return;
            }

            try
            {
                ArtistsPanel.Children.Clear();
                List<Artist> allArtists = _songService.GetAllArtists();

                // Filter artists by name
                var filteredArtists = allArtists.FindAll(a =>
                    a.Name.IndexOf(searchTerm, StringComparison.OrdinalIgnoreCase) >= 0);

                foreach (var artist in filteredArtists)
                {
                    // Create an artist card
                    Border artistCard = CreateArtistCard(artist);
                    ArtistsPanel.Children.Add(artistCard);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error searching artists: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ArtistSongs_List_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var selectedItem = ArtistSongs_List.SelectedItem as SongViewModel;

            if (selectedItem != null)
            {
                song_name.Text = selectedItem.Title;
                song_artist.Text = selectedItem.Artist;
            }
        }

        private void PlayAllArtistSongs_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedArtist != null)
            {
                try
                {
                    // Use the service to play all songs by the selected artist
                    _songService.PlayAllSongsByArtist(_selectedArtist.ArtistId);

                    // Update UI
                    PlayBtn.Content = "❚❚";
                    UpdateNowPlayingInfo();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error playing artist songs: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        #endregion

        #region Playlists

        private void LoadPlaylist(int playlistId)
        {
            try
            {
                // Get the repository through dependency injection if possible
                var playlistRepository = PlaylistRepository.Instance;
                var playlist = playlistRepository.GetOne(playlistId);

                if (playlist == null)
                {
                    MessageBox.Show("Playlist not found", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                // Set playlist info
                SelectedPlaylistNameText.Text = playlist.Name;

                // Load songs
                Playlist_Songs_List.Items.Clear();
                List<Song> songs = playlistRepository.GetSongsFromPlaylist(playlistId);

                // Update playlist info text
                int totalSeconds = 0;
                foreach (var song in songs)
                {
                    totalSeconds += song.Duration;
                }
                TimeSpan totalDuration = TimeSpan.FromSeconds(totalSeconds);
                SelectedPlaylistInfoText.Text = $"{songs.Count} songs • {FormatTotalDuration(totalDuration)}";

                int index = 1;
                foreach (Song song in songs)
                {
                    var songVM = new SongViewModel
                    {
                        Index = index.ToString(),
                        Title = song.Title,
                        Artist = song.Artist?.Name ?? "Unknown Artist",
                        Album = song.Album?.Title ?? "Unknown Album",
                        Duration = FormatDuration(song.Duration),
                        SongData = song
                    };

                    Playlist_Songs_List.Items.Add(songVM);
                    index++;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading playlist: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Playlist_Songs_List_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var selectedItem = Playlist_Songs_List.SelectedItem as SongViewModel;

            if (selectedItem != null)
            {
                song_name.Text = selectedItem.Title;
                song_artist.Text = selectedItem.Artist;
            }
        }

        #endregion

        #region Playback

        private void PlaySong(object sender, RoutedEventArgs e)
        {
            if (_songService.IsPaused())
            {
                // If paused, resume playback
                _songService.ResumePlayback();
                PlayBtn.Content = "❚❚";
            }
            else if (_songService.GetPlaybackState() == NAudio.Wave.PlaybackState.Playing)
            {
                // If playing, pause playback
                _songService.PausePlayback();
                PlayBtn.Content = "▶";
            }
            else
            {
                // If stopped or no current song, play the selected song or queue
                UIElement currentView = GetVisibleView();

                if (currentView == FavoritesView && Favorite_List.SelectedItem != null)
                {
                    var selectedItem = Favorite_List.SelectedItem as SongViewModel;
                    _songService.PlaySong(selectedItem.SongData);
                }
                else if (currentView == PlaylistView && Playlist_Songs_List.SelectedItem != null)
                {
                    var selectedItem = Playlist_Songs_List.SelectedItem as SongViewModel;
                    _songService.PlaySong(selectedItem.SongData);
                }
                else if (currentView == ArtistDetailView && ArtistSongs_List.SelectedItem != null)
                {
                    var selectedItem = ArtistSongs_List.SelectedItem as SongViewModel;
                    _songService.PlaySong(selectedItem.SongData);
                }
                else
                {
                    // No song is selected, try to play from queue
                    _songService.PlayFromQueue();
                }

                PlayBtn.Content = "❚❚";
                UpdateNowPlayingInfo();
            }
        }

        private UIElement GetVisibleView()
        {
            if (FavoritesView.Visibility == Visibility.Visible) return FavoritesView;
            if (PlaylistView.Visibility == Visibility.Visible) return PlaylistView;
            if (ArtistDetailView.Visibility == Visibility.Visible) return ArtistDetailView;
            if (MyMusicView.Visibility == Visibility.Visible) return MyMusicView;
            if (HomeView.Visibility == Visibility.Visible) return HomeView;
            return FavoritesView; // Default to FavoritesView if nothing is visible
        }

        private void NextSong_Click(object sender, RoutedEventArgs e)
        {
            _songService.NextSong();
            UpdateNowPlayingInfo();
            PlayBtn.Content = "❚❚";
        }

        private void PreviousSong_Click(object sender, RoutedEventArgs e)
        {
            _songService.PreviousSong();
            UpdateNowPlayingInfo();
            PlayBtn.Content = "❚❚";
        }

        private void ToggleLoop_Click(object sender, RoutedEventArgs e)
        {
            _songService.ToggleLoop();
            bool isLooping = _songService.IsLooping();
            LoopBtn.Foreground = isLooping
                ? new SolidColorBrush(Color.FromRgb(29, 185, 84))
                : new SolidColorBrush(Color.FromRgb(179, 179, 179));
        }

        private void VolumeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (_songService != null)
            {
                float volume = (float)(e.NewValue / 100.0); // Convert from percentage to 0-1 range
                _songService.SetVolume(volume);
            }
        }

        private void UpdateNowPlayingInfo()
        {
            Song currentSong = _songService.GetCurrentSong();
            if (currentSong != null)
            {
                song_name.Text = currentSong.Title;
                song_artist.Text = currentSong.Artist?.Name ?? "Unknown Artist";

                // Reset progress bar when song changes
                SongProgressBar.Value = 0;
            }
        }

        #endregion

        #region Utilities

        private string FormatDuration(int seconds)
        {
            TimeSpan time = TimeSpan.FromSeconds(seconds);
            return $"{time.Minutes}:{time.Seconds:D2}";
        }

        private string FormatTotalDuration(TimeSpan timeSpan)
        {
            if (timeSpan.TotalHours >= 1)
            {
                return $"{Math.Floor(timeSpan.TotalHours)} hours {timeSpan.Minutes} minutes";
            }
            else
            {
                return $"{timeSpan.Minutes} minutes";
            }
        }

        #endregion
    }

    // ViewModel for song display in ListView
    public class SongViewModel
    {
        public string Index { get; set; }
        public string Title { get; set; }
        public string Artist { get; set; }
        public string Album { get; set; }
        public string Duration { get; set; }
        public string PlayCount { get; set; }
        public Song SongData { get; set; }
    }
}
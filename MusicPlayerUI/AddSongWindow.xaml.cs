﻿using MusicPlayerRepositories;
using MusicPlayerServices;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.IO;
using NAudio.Wave;
using Microsoft.Win32;
using MusicPlayerEntities;

namespace MusicPlayerUI
{
    /// <summary>
    /// Interaction logic for AddSongWindow.xaml
    /// </summary>
    public partial class AddSongWindow : Window
    {
        private readonly SongService _songService;
        private readonly ArtistService _artistService;
        private readonly AlbumService _albumService;
        private readonly GenreService _genreService;
        private readonly UserService _userService; // Add reference to UserService
        private string _selectedFilePath;
        private int _fileDuration;
        private readonly string _targetDirectory;
        private readonly int _currentUserRole;
        private readonly string _currentUsername;
        private Artist _currentArtist; // To store the artist for role 2 users

        public AddSongWindow(SongService songService, ArtistService artistService, 
            AlbumService albumService, GenreService genreService, 
            int userRole, string username, UserService userService)
        {
            InitializeComponent(); // Ensure this is called first
            
            _songService = songService;
            _artistService = artistService;
            _albumService = albumService;
            _genreService = genreService;
            _userService = userService;
            _currentUserRole = userRole;
            _currentUsername = username;

            // Use a more reliable path resolution
            try 
            {
                string baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
                string assetsPath = Path.GetFullPath(Path.Combine(baseDirectory, "..", "..", "..", "Assets", "Songs"));
                
                // Fallback to user music folder if the project path isn't accessible
                if (!Directory.Exists(Path.GetDirectoryName(assetsPath)))
                {
                    _targetDirectory = Path.Combine(
                        Environment.GetFolderPath(Environment.SpecialFolder.MyMusic),
                        "MusicPlayerApp");
                }
                else
                {
                    _targetDirectory = assetsPath;
                }
                
                Directory.CreateDirectory(_targetDirectory);
                Console.WriteLine($"Song storage directory: {_targetDirectory}");
            }
            catch (Exception ex)
            {
                // Fallback to a safe directory if path resolution fails
                _targetDirectory = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.MyMusic),
                    "MusicPlayerApp");
                Directory.CreateDirectory(_targetDirectory);
                MessageBox.Show($"Using fallback directory: {_targetDirectory}", "Path Resolution Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            }

            ReleaseDatePicker.SelectedDate = DateTime.Today;
            
            // Make sure UI is loaded before configuring fields
            Loaded += (s, e) => {
                ConfigureArtistField();
                LoadAlbums();
                LoadGenres();
            };
        }

        private void ConfigureArtistField()
        {
            try 
            {
                if (ArtistComboBox == null)
                {
                    MessageBox.Show("ArtistComboBox is not initialized", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
                
                if (_currentUserRole == 2) // Artist role
                {
                    // Find or create artist associated with this username
                    _currentArtist = _artistService.GetArtistByUsername(_currentUsername);
                    if (_currentArtist == null)
                    {
                        // Create new artist based on username
                        _currentArtist = new Artist
                        {
                            Name = _currentUsername,
                            Bio = string.Empty
                        };
                        _artistService.AddNewArtist(_currentArtist);
                    }

                    // Disable artist selection and set to current artist
                    ArtistComboBox.IsEnabled = false;
                    ArtistComboBox.IsEditable = false;
                    
                    // Set up the ComboBox with just this artist
                    var singleArtistList = new List<Artist> { _currentArtist };
                    ArtistComboBox.ItemsSource = singleArtistList;
                    ArtistComboBox.DisplayMemberPath = "Name";
                    ArtistComboBox.SelectedValuePath = "ArtistId";
                    ArtistComboBox.SelectedItem = _currentArtist;
                    
                    // Hide the "+" button for adding artists if it exists
                    var addArtistButton = this.FindName("NewArtistButton") as Button;
                    if (addArtistButton != null)
                    {
                        addArtistButton.Visibility = Visibility.Collapsed;
                    }
                }
                else if (_currentUserRole == 3) // Admin role
                {
                    // Admin can select from all artists or create new ones
                    LoadArtists();
                }
                else
                {
                    // Default behavior for other roles
                    LoadArtists();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error configuring artist field: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadArtists()
        {
            try
            {
                var artists = _artistService.GetAllArtists();
                ArtistComboBox.ItemsSource = artists;
                ArtistComboBox.DisplayMemberPath = "Name";
                ArtistComboBox.SelectedValuePath = "ArtistId";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading artists: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadAlbums()
        {
            try
            {
                // For artists, only show their albums
                if (_currentUserRole == 2 && _currentArtist != null)
                {
                    var albums = _albumService.GetByArtistId(_currentArtist.ArtistId);
                    AlbumComboBox.ItemsSource = albums;
                }
                else
                {
                    var albums = _albumService.GetAll();
                    AlbumComboBox.ItemsSource = albums;
                }
                
                AlbumComboBox.DisplayMemberPath = "Title";
                AlbumComboBox.SelectedValuePath = "AlbumId";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading albums: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadGenres()
        {
            try
            {
                var genres = _genreService.GetAll();
                GenreComboBox.ItemsSource = genres;
                GenreComboBox.DisplayMemberPath = "Name";
                GenreComboBox.SelectedValuePath = "GenreId";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading genres: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BrowseButton_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Filter = "MP3 Files (*.mp3)|*.mp3|All files (*.*)|*.*",
                Title = "Select MP3 File"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                _selectedFilePath = openFileDialog.FileName;
                SelectedFileText.Text = Path.GetFileName(_selectedFilePath);

                // Get duration from the mp3 file
                try
                {
                    using (var audioFile = new AudioFileReader(_selectedFilePath))
                    {
                        _fileDuration = (int)audioFile.TotalTime.TotalSeconds;
                        DurationTextBox.Text = _fileDuration.ToString();
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error reading audio file: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    _fileDuration = 0;
                    DurationTextBox.Text = "Unknown";
                }
            }
        }

        private void NewArtistButton_Click(object sender, RoutedEventArgs e)
        {
            // This button should be hidden for role 2 users
            var newArtistWindow = new AddArtistWindow(_artistService);
            if (newArtistWindow.ShowDialog() == true)
            {
                LoadArtists();

                var newArtistName = newArtistWindow.ArtistName;
                foreach (var artist in (List<Artist>)ArtistComboBox.ItemsSource)
                {
                    if (artist.Name == newArtistName)
                    {
                        ArtistComboBox.SelectedItem = artist;
                        break;
                    }
                }
            }
        }

        private void NewAlbumButton_Click(object sender, RoutedEventArgs e)
        {
            var newAlbumWindow = new AddAlbumWindow(_albumService, _artistService);
            if (newAlbumWindow.ShowDialog() == true)
            {
                LoadAlbums();

                // Select the newly added album
                var newAlbumTitle = newAlbumWindow.AlbumTitle;
                foreach (var album in (List<Album>)AlbumComboBox.ItemsSource)
                {
                    if (album.Title == newAlbumTitle)
                    {
                        AlbumComboBox.SelectedItem = album;
                        break;
                    }
                }
            }
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Validate input
                if (string.IsNullOrWhiteSpace(TitleTextBox.Text))
                {
                    ErrorMessageText.Text = "Please enter a song title.";
                    return;
                }

                if (ArtistComboBox.SelectedItem == null && string.IsNullOrWhiteSpace(ArtistComboBox.Text))
                {
                    ErrorMessageText.Text = "Please select or enter an artist.";
                    return;
                }

                if (string.IsNullOrWhiteSpace(_selectedFilePath))
                {
                    ErrorMessageText.Text = "Please select an MP3 file.";
                    return;
                }

                // Check if source file exists
                if (!File.Exists(_selectedFilePath))
                {
                    ErrorMessageText.Text = "The selected file no longer exists.";
                    return;
                }

                int artistId;
                var selectedArtist = ArtistComboBox.SelectedItem as Artist;

                if (selectedArtist != null)
                {
                    artistId = selectedArtist.ArtistId;
                }
                else
                {
                    var newArtist = new Artist
                    {
                        Name = ArtistComboBox.Text,
                        Bio = string.Empty
                    };

                    _artistService.AddNewArtist(newArtist);
                    artistId = newArtist.ArtistId;
                }

                int? albumId = null;
                var selectedAlbum = AlbumComboBox.SelectedItem as Album;

                if (selectedAlbum != null)
                {
                    albumId = selectedAlbum.AlbumId;
                }
                else if (!string.IsNullOrWhiteSpace(AlbumComboBox.Text))
                {
                    var newAlbum = new Album
                    {
                        Title = AlbumComboBox.Text,
                        ArtistId = artistId,
                        ReleaseYear = DateTime.Now.Year
                    };
                    _albumService.AddNewAlbum(newAlbum);
                    albumId = newAlbum.AlbumId;
                }

                int? genreId = null;
                var selectedGenre = GenreComboBox.SelectedItem as Genre;

                if (selectedGenre != null)
                {
                    genreId = selectedGenre.GenreId;
                }

                // Create the song object passing the original file path
                // The repository will handle copying the file
                var newSong = new Song
                {
                    Title = TitleTextBox.Text,
                    ArtistId = artistId,
                    AlbumId = albumId,
                    GenreId = genreId,
                    Duration = _fileDuration,
                    FilePath = _selectedFilePath, // Pass the complete file path
                    ReleaseDate = ReleaseDatePicker.SelectedDate.HasValue
                        ? DateOnly.FromDateTime(ReleaseDatePicker.SelectedDate.Value)
                        : null,
                    PlayCount = 0
                };

                _songService.Add(newSong);
                
                DialogResult = true;
            }
            catch (Exception ex)
            {
                ErrorMessageText.Text = $"Error saving song: {ex.Message}";
                MessageBox.Show($"Error saving song: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }
    }
}

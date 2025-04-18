﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MusicPlayerEntities;
using MusicPlayerRepositories;
using NAudio.Wave;

namespace MusicPlayerServices
{
    public class SongService : ISongService
    {
        private SongRepository _songRepository;

        public SongService()
        {
            _songRepository = SongRepository.Instance;
        }

        public Song? GetOne(int id)
        => _songRepository.GetOne(id);

        public List<Song> GetAll()
        => _songRepository.GetAll();

        public void Add(Song a)
        => _songRepository.Add(a);

        public void Update(Song a)
        => _songRepository.Update(a);

        public void Delete(int id)
        => _songRepository.Delete(id);

        // Progress Tracking
        public float GetCurrentPosition()
        => _songRepository.GetCurrentPosition();

        public float GetCurrentDuration()
        => _songRepository.GetCurrentDuration();

        // Queue Management
        public void LoadPlaylistToQueue(int playlistId, bool clearCurrentQueue = true)
        => _songRepository.LoadPlaylistToQueue(playlistId, clearCurrentQueue);

        public void AddToQueue(Song song)
        => _songRepository.AddToQueue(song);

        public void AddToQueue(int songId)
        => _songRepository.AddToQueue(songId);

        public Song RemoveFromQueue(int index)
        => _songRepository.RemoveFromQueue(index);

        public void ClearQueue()
        => _songRepository.ClearQueue();

        public List<Song> GetCurrentQueue()
        => _songRepository.GetCurrentQueue();

        public Song GetCurrentSong()
        => _songRepository.GetCurrentSong();

        // Playback Control
        public void PlaySong(Song song)
        => _songRepository.PlaySong(song);

        public void PlaySongById(int songId)
        => _songRepository.PlaySongById(songId);

        public void PlayFromQueue()
        => _songRepository.PlayFromQueue();

        public void PausePlayback()
        => _songRepository.PausePlayback();

        public void ResumePlayback()
        => _songRepository.ResumePlayback();

        public void TogglePlayPause()
        => _songRepository.TogglePlayPause();

        public void StopPlayback()
        => _songRepository.StopPlayback();

        public void NextSong()
        => _songRepository.NextSong();

        public void PreviousSong()
        => _songRepository.PreviousSong();

        public void SetVolume(float volume)
        => _songRepository.SetVolume(volume);

        public float GetVolume()
        => _songRepository.GetVolume();

        public void ToggleLoop()
        => _songRepository.ToggleLoop();

        public bool IsLooping()
        => _songRepository.IsLooping();

        public bool IsPaused()
        => _songRepository.IsPaused();

        public PlaybackState GetPlaybackState()
        => _songRepository.GetPlaybackState();

        public void Dispose()
        => _songRepository.Dispose();

        // Add these methods to your SongService.cs

        public List<Song> GetSongsByArtist(int artistId)
            => _songRepository.GetSongsByArtist(artistId);

        public List<Song> GetSongsByArtistName(string artistName)
            => _songRepository.GetSongsByArtistName(artistName);

        public void UpdateSongArtist(int songId, int newArtistId)
            => _songRepository.UpdateSongArtist(songId, newArtistId);

        public List<Artist> GetAllArtists()
            => _songRepository.GetAllArtists();

        public List<Song> SearchSongsByArtist(string searchTerm)
            => _songRepository.SearchSongsByArtist(searchTerm);

        public List<Song> GetTopSongsByArtist(int artistId, int count = 5)
            => _songRepository.GetTopSongsByArtist(artistId, count);

        public Dictionary<int, int> GetSongCountByArtist()
            => _songRepository.GetSongCountByArtist();

        public void PlayAllSongsByArtist(int artistId)
        {
            var songs = _songRepository.GetSongsByArtist(artistId);
            if (songs.Count == 0)
            {
                return;
            }

            // Clear current queue and add all songs by this artist
            _songRepository.ClearQueue();

            foreach (var song in songs)
            {
                _songRepository.AddToQueue(song);
            }

            // Start playback of the first song
            _songRepository.PlayFromQueue();
        }
    }
}
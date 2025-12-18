using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using NAudio.Wave;
using SoundpadLightFINAL.Controllers;
using SoundpadLightFINAL.Data;
using SoundpadLightFINAL.Models;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace SoundpadLightFINAL.Views;

public partial class MainWindow : Window
{
    private readonly AuthController _authController;
    private readonly SoundController _soundController;
    private readonly PlaylistController _playlistController;
    private readonly Database _database;
    private readonly User _currentUser;

    private IList<SoundItem> _sounds = new List<SoundItem>();
    private IList<Playlist> _playlists = new List<Playlist>();
    private int? _currentPlaylistId;

    private IWavePlayer? _outputDevice;
    private AudioFileReader? _audioFile;

    private float _volume = 1f;
    private bool _isUserSeeking;
    private DispatcherTimer _timer = new();

    private SoundItem? _currentSound;
    private Button? _currentPlayButton;

    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".mp3", ".wav", ".ogg", ".flac"
    };

    public MainWindow(
        AuthController authController,
        SoundController soundController,
        PlaylistController playlistController,
        Database database,
        User currentUser)
    {
        _authController = authController;
        _soundController = soundController;
        _playlistController = playlistController;
        _database = database;
        _currentUser = currentUser;

        InitializeComponent();
        InitializeUi();
    }

    // ================= INIT =================

    private void InitializeUi()
    {
        UpdateUsernameDisplay(_currentUser.Username);
        UsernameTextBlock.Text = _currentUser.Username;

        AddButton.Click += OnAddSoundClick;
        DeleteButton.Click += OnDeleteSoundClick;
        BrowseButton.Click += OnBrowseClick;
        VolumeSlider.PropertyChanged += OnVolumeChanged;

        AddHandler(DragDrop.DragOverEvent, OnDragOver);
        AddHandler(DragDrop.DropEvent, OnDrop);

        _timer.Interval = TimeSpan.FromMilliseconds(200);
        _timer.Tick += UpdateTimeline;

        VolumeSlider.Value = 100;

        // Загрузка плейлистов и звуков
        LoadPlaylists();
        LoadSounds();
    }

    public void UpdateUsernameDisplay(string username)
    {
        _currentUser.Username = username;
        UsernameTextBlock.Text = username;
    }

    // ================= PLAYLISTS =================

    private void LoadPlaylists()
    {
        _playlists = _playlistController.GetUserPlaylists(_currentUser.Id);

        var items = new List<PlaylistListItem>
        {
            new() { Id = null, Name = "All sounds" }
        };
        items.AddRange(_playlists.Select(p => new PlaylistListItem { Id = p.Id, Name = p.Name }));

        PlaylistListBox.ItemsSource = items;
        PlaylistListBox.SelectedIndex = 0;
        _currentPlaylistId = null;

        PlaylistListBox.SelectionChanged += OnPlaylistSelectionChanged;
        AddPlaylistButton.Click += OnAddPlaylistClick;
        RenamePlaylistButton.Click += OnRenamePlaylistClick;
        DeletePlaylistButton.Click += OnDeletePlaylistClick;
    }

    private void OnPlaylistSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (PlaylistListBox.SelectedItem is PlaylistListItem item)
            _currentPlaylistId = item.Id;

        LoadSounds();
    }

    private void OnAddPlaylistClick(object? sender, RoutedEventArgs e)
    {
        var newId = _playlistController.AddPlaylist(_currentUser.Id, "New playlist");
        LoadPlaylists();

        // Автовыбор нового плейлиста
        foreach (var obj in (IEnumerable)PlaylistListBox.ItemsSource!)
        {
            if (obj is PlaylistListItem item && item.Id == newId)
            {
                PlaylistListBox.SelectedItem = item;
                break;
            }
        }
    }

    private async void OnRenamePlaylistClick(object? sender, RoutedEventArgs e)
    {
        if (PlaylistListBox.SelectedItem is not PlaylistListItem item || item.Id == null)
            return;

        var newName = await ShowTextInputDialogAsync("Rename playlist", "Playlist name:", item.Name);
        if (!string.IsNullOrWhiteSpace(newName))
        {
            _playlistController.RenamePlaylist(item.Id.Value, newName);
            LoadPlaylists();
        }
    }

    private void OnDeletePlaylistClick(object? sender, RoutedEventArgs e)
    {
        if (PlaylistListBox.SelectedItem is not PlaylistListItem item || item.Id == null)
            return;

        _playlistController.DeletePlaylist(item.Id.Value);
        LoadPlaylists();
        LoadSounds();
    }

    private class PlaylistListItem
    {
        public int? Id { get; set; }
        public string Name { get; set; } = "";
        public override string ToString() => Name;
    }

    private async System.Threading.Tasks.Task<string?> ShowTextInputDialogAsync(string title, string label, string initialText)
    {
        var window = new Window
        {
            Title = title,
            Width = 320,
            Height = 160,
            CanResize = false,
            WindowStartupLocation = WindowStartupLocation.CenterOwner
        };

        var box = new TextBox { Text = initialText };
        var ok = new Button { Content = "OK" };
        ok.Click += (_, _) => window.Close(box.Text);

        window.Content = new StackPanel
        {
            Margin = new Thickness(12),
            Children = { new TextBlock { Text = label }, box, ok }
        };

        return await window.ShowDialog<string?>(this);
    }

    // ================= AUDIO =================

    private void PlaySound(SoundItem item)
    {
        StopPlayback();

        _audioFile = new AudioFileReader(item.FilePath)
        {
            Volume = _volume
        };

        _outputDevice = new WaveOutEvent
        {
            DesiredLatency = 200,
            NumberOfBuffers = 4
        };

        _outputDevice.Init(_audioFile);
        _outputDevice.Play();

        TimelineSlider.Maximum = _audioFile.TotalTime.TotalSeconds;
        _timer.Start();

        _currentSound = item;
    }

    private void PlaySelectedSound()
    {
        if (SoundsDataGrid.SelectedItem is SoundItem item)
            PlaySound(item);
    }

    private void StopPlayback()
    {
        _timer.Stop();

        _outputDevice?.Stop();
        _outputDevice?.Dispose();
        _outputDevice = null;

        _audioFile?.Dispose();
        _audioFile = null;

        _currentPlayButton?.SetValue(Button.ContentProperty, "▶");
        _currentPlayButton = null;
        _currentSound = null;
    }

    // ================= TIMELINE =================

    private void UpdateTimeline(object? sender, EventArgs e)
    {
        if (_audioFile == null || _isUserSeeking)
            return;

        TimelineSlider.Value = _audioFile.CurrentTime.TotalSeconds;
        TimeText.Text =
            $"{_audioFile.CurrentTime:mm\\:ss} / {_audioFile.TotalTime:mm\\:ss}";
    }

    private void OnTimelinePointerPressed(object? sender, PointerPressedEventArgs e)
        => _isUserSeeking = true;

    private void OnTimelinePointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        _isUserSeeking = false;

        if (_audioFile != null)
            _audioFile.CurrentTime =
                TimeSpan.FromSeconds(TimelineSlider.Value);
    }

    private void OnTimelineChanged(object? sender, RangeBaseValueChangedEventArgs e) { }

    // ================= GRID PLAY =================

    private void OnGridPlayClick(object? sender, RoutedEventArgs e)
    {
        if (sender is not Button btn || btn.Tag is not SoundItem item)
            return;

        if (_currentSound?.Id == item.Id)
        {
            StopPlayback();
            return;
        }

        _currentPlayButton?.SetValue(Button.ContentProperty, "▶");

        PlaySound(item);

        btn.Content = "⏹";
        _currentSound = item;
        _currentPlayButton = btn;
    }

    // ================= UI =================

    private void OnVolumeChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (_audioFile != null)
            _audioFile.Volume = (float)(VolumeSlider.Value / 100);
    }

    private async void OnBrowseClick(object? sender, RoutedEventArgs e)
    {
        var files = await StorageProvider.OpenFilePickerAsync(
            new FilePickerOpenOptions
            {
                AllowMultiple = false,
                FileTypeFilter = new[]
                {
                    new FilePickerFileType("Audio")
                    {
                        Patterns = new[] { "*.mp3", "*.wav", "*.ogg", "*.flac" }
                    }
                }
            });

        if (files.Count > 0)
            PathTextBox.Text = files[0].Path.LocalPath;
    }

    private void OnAddSoundClick(object? sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(NameTextBox.Text) ||
            string.IsNullOrWhiteSpace(PathTextBox.Text))
            return;

        if (!File.Exists(PathTextBox.Text))
            return;

        _soundController.AddSound(
            _currentUser.Id,
            NameTextBox.Text,
            PathTextBox.Text,
            _currentPlaylistId);

        NameTextBox.Text = "";
        PathTextBox.Text = "";
        LoadSounds();
    }

    private void OnDeleteSoundClick(object? sender, RoutedEventArgs e)
    {
        if (SoundsDataGrid.SelectedItem is not SoundItem item)
            return;

        _soundController.DeleteSound(item.Id);
        LoadSounds();
    }

    // ================= DRAG & DROP =================

    private void OnDragOver(object? sender, DragEventArgs e)
    {
        e.DragEffects = e.Data.Contains(DataFormats.Files)
            ? DragDropEffects.Copy
            : DragDropEffects.None;
    }

    private void OnDrop(object? sender, DragEventArgs e)
    {
        if (!e.Data.Contains(DataFormats.Files))
            return;

        var files = e.Data.GetFileNames();
        if (files == null)
            return;

        foreach (var file in files)
        {
            if (!AllowedExtensions.Contains(Path.GetExtension(file)))
                continue;

            _soundController.AddSound(
                _currentUser.Id,
                Path.GetFileNameWithoutExtension(file),
                file,
                _currentPlaylistId);
        }

        LoadSounds();
    }

    // ================= DATA =================

    private void LoadSounds()
    {
        _sounds = _soundController.GetUserSounds(
            _currentUser.Id,
            _currentPlaylistId);

        SoundsDataGrid.ItemsSource = _sounds;
    }

    // ================= LOGOUT =================

    public void Logout()
    {
        var w = new LoginWindow(
            _authController,
            _soundController,
            _playlistController,
            _database);

        w.Show();
        Close();
    }
}

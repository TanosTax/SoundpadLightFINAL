using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Layout;
using SoundpadLightFINAL.Controllers;
using SoundpadLightFINAL.Models;

namespace SoundpadLightFINAL.Views;

public partial class AccountWindow : Window
{
    private readonly MainWindow _parent;
    private readonly User _user;
    private readonly AuthController _authController;
    private readonly SoundController _soundController;
    private readonly PlaylistController _playlistController;

    public AccountWindow(MainWindow parent, User user, AuthController authController, SoundController soundController, PlaylistController playlistController)
    {
        _parent = parent;
        _user = user;
        _authController = authController;
        _soundController = soundController;
        _playlistController = playlistController;

        InitializeComponent();
        InitializeUi();
    }

    private void InitializeUi()
    {
        CloseButton.Click += OnCloseClick;
        LogoutButton.Click += OnLogoutClick;
        RenameUserButton.Click += OnRenameUserClick;

        AccountNameTextBlock.Text = _user.Username;

        var playlists = _playlistController.GetUserPlaylists(_user.Id);
        var sounds = _soundController.GetUserSounds(_user.Id, null);

        PlaylistsCountTextBlock.Text = playlists.Count.ToString();
        SoundsCountTextBlock.Text = sounds.Count.ToString();

        if (playlists.Count == 0)
        {
            AveragePerPlaylistTextBlock.Text = "–";
        }
        else
        {
            var avg = (double)sounds.Count / playlists.Count;
            AveragePerPlaylistTextBlock.Text = avg.ToString("0.0");
        }
    }

    private void OnCloseClick(object? sender, RoutedEventArgs e)
    {
        Close();
    }

    private void OnLogoutClick(object? sender, RoutedEventArgs e)
    {
        Close();
        _parent.Logout();
    }

    private async void OnRenameUserClick(object? sender, RoutedEventArgs e)
    {
        ErrorTextBlock.Text = string.Empty;

        var dialog = new Window
        {
            Title = "Rename account",
            Width = 320,
            Height = 160,
            Background = this.Background,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            CanResize = false
        };

        var textBox = new TextBox
        {
            Text = _user.Username
        };

        var okButton = new Button { Content = "OK", Width = 70, Margin = new Thickness(0, 0, 8, 0) };
        var cancelButton = new Button { Content = "Cancel", Width = 70 };

        okButton.Click += (_, _) => dialog.Close(textBox.Text);
        cancelButton.Click += (_, _) => dialog.Close(null);

        var buttonsPanel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Right
        };
        buttonsPanel.Children.Add(okButton);
        buttonsPanel.Children.Add(cancelButton);

        var root = new StackPanel
        {
            Margin = new Thickness(12)
        };
        root.Children.Add(new TextBlock { Text = "New username:", Margin = new Thickness(0, 0, 0, 4) });
        root.Children.Add(textBox);
        root.Children.Add(new StackPanel { Height = 8 });
        root.Children.Add(buttonsPanel);

        dialog.Content = root;

        var result = await dialog.ShowDialog<string?>(this);
        if (string.IsNullOrWhiteSpace(result))
        {
            return;
        }

        if (!_authController.ChangeUsername(_user.Id, result, out var error))
        {
            ErrorTextBlock.Text = error ?? "Failed to rename account.";
            return;
        }

        _user.Username = result;
        AccountNameTextBlock.Text = result;
        _parent.UpdateUsernameDisplay(result);
    }
}



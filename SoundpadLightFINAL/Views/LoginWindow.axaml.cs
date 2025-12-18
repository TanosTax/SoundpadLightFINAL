using System;
using Avalonia.Controls;
using Avalonia.Interactivity;
using SoundpadLightFINAL.Controllers;
using SoundpadLightFINAL.Data;
using SoundpadLightFINAL.Models;

namespace SoundpadLightFINAL.Views;

public partial class LoginWindow : Window
{
    private readonly AuthController _authController;
    private readonly SoundController _soundController;
    private readonly PlaylistController _playlistController;
    private readonly Database _database;

    public LoginWindow(AuthController authController, SoundController soundController, PlaylistController playlistController, Database database)
    {
        _authController = authController;
        _soundController = soundController;
        _playlistController = playlistController;
        _database = database;

        InitializeComponent();
        InitializeUi();
    }

    private void InitializeUi()
    {
        LoginButton.Click += OnLoginClick;
        RegisterButton.Click += OnRegisterClick;
    }

    private void OnLoginClick(object? sender, RoutedEventArgs e)
    {
        ErrorTextBlock.Text = string.Empty;

        var username = UsernameTextBox.Text ?? string.Empty;
        var password = PasswordTextBox.Text ?? string.Empty;

        var user = _authController.Login(username, password);
        if (user == null)
        {
            ErrorTextBlock.Text = "Invalid username or password.";
            return;
        }

        OpenMainWindow(user);
    }

    private void OnRegisterClick(object? sender, RoutedEventArgs e)
    {
        var registerWindow = new RegisterWindow(_authController);
        registerWindow.ShowDialog(this);
    }

    private void OpenMainWindow(User user)
    {
        var mainWindow = new MainWindow(_authController, _soundController, _playlistController, _database, user);
        mainWindow.Show();
        Close();
    }
}



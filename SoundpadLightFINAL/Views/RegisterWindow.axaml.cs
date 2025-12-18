using System;
using Avalonia.Controls;
using Avalonia.Interactivity;
using SoundpadLightFINAL.Controllers;

namespace SoundpadLightFINAL.Views;

public partial class RegisterWindow : Window
{
    private readonly AuthController _authController;

    public RegisterWindow(AuthController authController)
    {
        _authController = authController;
        InitializeComponent();
        InitializeUi();
    }

    private void InitializeUi()
    {
        RegisterButton.Click += OnRegisterClick;
        CancelButton.Click += OnCancelClick;
    }

    private void OnRegisterClick(object? sender, RoutedEventArgs e)
    {
        ErrorTextBlock.Text = string.Empty;

        var username = UsernameTextBox.Text ?? string.Empty;
        var password = PasswordTextBox.Text ?? string.Empty;

        var result = _authController.Register(username, password, out var error);
        if (!result)
        {
            ErrorTextBlock.Text = error ?? "Failed to register user.";
            return;
        }

        Close();
    }

    private void OnCancelClick(object? sender, RoutedEventArgs e)
    {
        Close();
    }
}



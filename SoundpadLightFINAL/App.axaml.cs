using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using SoundpadLightFINAL.Controllers;
using SoundpadLightFINAL.Data;
using SoundpadLightFINAL.Views;

namespace SoundpadLightFINAL
{
    public partial class App : Application
    {
        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public override void OnFrameworkInitializationCompleted()
        {
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                var database = new Database();
                database.EnsureCreated();

                var authController = new AuthController(database);
                var soundController = new SoundController(database);
                var playlistController = new PlaylistController(database);

                desktop.MainWindow = new LoginWindow(authController, soundController, playlistController, database);
            }

            base.OnFrameworkInitializationCompleted();
        }
    }
}
using System;
using System.Windows;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Connect4Client.Data;
using Connect4Client.Models;
using System.IO;

namespace Connect4Client
{
    public partial class App : Application
    {
        private ServiceProvider? serviceProvider;
        
        public IServiceProvider? Services => serviceProvider;

        protected override void OnStartup(StartupEventArgs e)
        {
            try
            {
                base.OnStartup(e);
                Application.Current.ShutdownMode = ShutdownMode.OnExplicitShutdown;
                InitializeServices();
                
                var loginWindow = new LoginWindow();
                var loginResult = loginWindow.ShowDialog();
                
                if (loginResult == true && loginWindow.LoggedInPlayer != null)
                {
                    var mainWindow = new MainWindow(loginWindow.LoggedInPlayer);
                    mainWindow.Show();
                    Application.Current.MainWindow = mainWindow;
                    Application.Current.ShutdownMode = ShutdownMode.OnMainWindowClose;
                    
                }
                else
                {
                    Shutdown();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Application startup error: {ex.Message}", 
                    "Startup Error", MessageBoxButton.OK, MessageBoxImage.Error);
                Shutdown();
            }
        }

        private void InitializeServices()
        {
            try
            {
                string appDirectory = AppDomain.CurrentDomain.BaseDirectory;
                string projectRoot = Path.GetFullPath(Path.Combine(appDirectory, "..", "..", ".."));
                string dbPath = Path.Combine(projectRoot, "Connect4ClientDb.mdf");
                
                var services = new ServiceCollection();
                
                services.AddDbContext<GameContext>(options =>
                    options.UseSqlServer($"Server=(localdb)\\mssqllocaldb;AttachDbFilename={dbPath};Database=Connect4ClientDb;Trusted_Connection=True;MultipleActiveResultSets=true"));
                
                serviceProvider = services.BuildServiceProvider();
                
                using var scope = serviceProvider.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<GameContext>();
                context.Database.EnsureCreated();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Service initialization error: {ex.Message}", 
                    "Service Error", MessageBoxButton.OK, MessageBoxImage.Error);
                throw;
            }
        }

        protected override void OnExit(ExitEventArgs e)
        {
            serviceProvider?.Dispose();
            base.OnExit(e);
        }
    }
} 
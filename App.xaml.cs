using System.Windows;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Connect4Client.Data;
using System.IO;

namespace Connect4Client
{
    public partial class App : Application
    {
        private ServiceProvider? serviceProvider;
        
        public IServiceProvider? Services => serviceProvider;

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            
            try
            {
                // Get the application directory (where the executable is running from)
                string appDirectory = AppDomain.CurrentDomain.BaseDirectory;
                
                // Go up to find the project root (where the .mdf file should be)
                string projectRoot = Path.GetFullPath(Path.Combine(appDirectory, "..", "..", ".."));
                string dbPath = Path.Combine(projectRoot, "Connect4ClientDb.mdf");
                
                // Configure services like in the lectures
                var services = new ServiceCollection();
                
                // Add DbContext with SQL Server pointing to local .mdf file
                services.AddDbContext<GameContext>(options =>
                    options.UseSqlServer($"Server=(localdb)\\mssqllocaldb;AttachDbFilename={dbPath};Database=Connect4ClientDb;Trusted_Connection=True;MultipleActiveResultSets=true"));
                
                serviceProvider = services.BuildServiceProvider();
                
                // Initialize the database
                using var scope = serviceProvider.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<GameContext>();
                context.Database.EnsureCreated();
                
                // Show login window first
                var loginWindow = new LoginWindow();
                var loginResult = loginWindow.ShowDialog();
                
                if (loginResult == true && loginWindow.LoggedInPlayer != null)
                {
                    // Login successful, show main window
                    var mainWindow = new MainWindow(loginWindow.LoggedInPlayer);
                    mainWindow.Show();
                }
                else
                {
                    // Login failed or cancelled, exit application
                    Shutdown();
                }
            }
            catch (Exception ex)
            {
                // Show error message and exit
                MessageBox.Show($"Application startup failed: {ex.Message}\n\nDetails: {ex.ToString()}", 
                    "Startup Error", MessageBoxButton.OK, MessageBoxImage.Error);
                Shutdown();
            }
        }

        protected override void OnExit(ExitEventArgs e)
        {
            serviceProvider?.Dispose();
            base.OnExit(e);
        }
    }
} 
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;
using System.Windows.Media.Animation;
using Connect4Client.Models;
using Connect4Client.Services;
using Connect4Client.Data;

namespace Connect4Client
{
    public partial class MainWindow : Window
    {
        private const int ROWS = 6;
        private const int COLUMNS = 7;
        
        private Rectangle[,] gameBoard = null!;
        private Ellipse[,] gamePieces = null!;
        private int[,] boardState = null!;
        private bool isPlayerTurn = true;
        private bool gameActive = false;
        private bool isProcessingMove = false;
        private Player? currentPlayer;
        private GameDto? currentGame;
        
        private DispatcherTimer animationTimer = null!;
        private DispatcherTimer cpuMoveTimer = null!;
        private DispatcherTimer colorChangeTimer = null!;
        private Ellipse? droppingPiece;
        private int animationColumn = -1;
        private int animationRow = -1;
        private double animationSpeed = 8.0;
        
        private readonly GameService gameService;
        private readonly ApiService apiService;
        
        public MainWindow()
        {
            try
            {
                InitializeComponent();
                gameService = new GameService();
                apiService = new ApiService();
                
                // Set responsive window size based on screen
                SetResponsiveWindowSize();
                
                InitializeTimers();
                InitializeGameBoard();
                InitializeColumnButtons();
                UpdatePlayerInfo();
                UpdateGameStatus();
                
                // Test database connection and save functionality
                _ = Task.Run(async () =>
                {
                    try
                    {
                        // Test service provider
                        System.Diagnostics.Debug.WriteLine($"Service provider is null: {((App)App.Current).Services == null}");
                        
                        var dbTest = await gameService.TestDatabaseConnection();
                        System.Diagnostics.Debug.WriteLine($"Database connection test result: {dbTest}");
                        
                        // Test saving a game
                        if (dbTest)
                        {
                            System.Diagnostics.Debug.WriteLine("Testing manual save...");
                            var testBoard = new int[6, 7];
                            testBoard[0, 0] = 1; // Add a test piece
                            testBoard[1, 1] = 2; // Add a CPU piece
                            
                            try
                            {
                                await gameService.SaveGameState(999, testBoard, true, 999);
                                System.Diagnostics.Debug.WriteLine("Test game save successful");
                                
                                // Test loading games
                                var savedGames = await gameService.GetSavedGamesForPlayer(999);
                                System.Diagnostics.Debug.WriteLine($"Found {savedGames.Count} test games");
                                
                                if (savedGames.Count > 0)
                                {
                                    System.Diagnostics.Debug.WriteLine($"First saved game - PlayerId: {savedGames[0].PlayerId}, GameId: {savedGames[0].GameId}");
                                }
                            }
                            catch (Exception ex)
                            {
                                System.Diagnostics.Debug.WriteLine($"Manual save test failed: {ex.Message}");
                                System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
                            }
                        }
                        else
                        {
                            System.Diagnostics.Debug.WriteLine("Database connection test failed - cannot test save functionality");
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Database test failed: {ex.Message}");
                        System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
                    }
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"MainWindow initialization error: {ex.Message}", 
                    "Initialization Error", MessageBoxButton.OK, MessageBoxImage.Error);
                throw;
            }
        }
        
        private void SetResponsiveWindowSize()
        {
            var screenWidth = SystemParameters.PrimaryScreenWidth;
            var screenHeight = SystemParameters.PrimaryScreenHeight;
            
            Width = Math.Max(800, Math.Min(screenWidth * 0.8, 1200));
            Height = Math.Max(600, Math.Min(screenHeight * 0.8, 900));
        }

        public MainWindow(Player loggedInPlayer) : this()
        {
            SetLoggedInPlayer(loggedInPlayer);
        }

        public void ShowLoginDialog()
        {
            var loginWindow = new LoginWindow();
            if (loginWindow.ShowDialog() == true && loginWindow.LoggedInPlayer != null)
            {
                SetLoggedInPlayer(loginWindow.LoggedInPlayer);
            }
            else
            {
                MessageBox.Show("You need to login to play games. Click 'Connect to Server' to login later.", 
                    "Login Required", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void SetLoggedInPlayer(Player player)
        {
            currentPlayer = player;
            UpdatePlayerInfo();
            UpdateGameStatus();
            ConnectButton.Content = "✓ Connected";
            ConnectButton.IsEnabled = false;
            
            _ = Task.Run(async () =>
            {
                await Dispatcher.InvokeAsync(async () =>
                {
                    await StartNewGame();
                });
            });
        }
        
        private void UpdatePlayerInfo()
        {
            if (currentPlayer != null)
            {
                PlayerNameText.Text = currentPlayer.FirstName;
                UpdateStatistics();
            }
            else
            {
                PlayerNameText.Text = "Not Connected";
            }
        }
        
        private void InitializeTimers()
        {
            animationTimer = new DispatcherTimer();
            animationTimer.Interval = TimeSpan.FromMilliseconds(50);
            animationTimer.Tick += AnimationTimer_Tick;
            
            cpuMoveTimer = new DispatcherTimer();
            cpuMoveTimer.Interval = TimeSpan.FromMilliseconds(1500);
            cpuMoveTimer.Tick += CpuMoveTimer_Tick;
            
            colorChangeTimer = new DispatcherTimer();
            colorChangeTimer.Interval = TimeSpan.FromMilliseconds(200);
            colorChangeTimer.Tick += ColorChangeTimer_Tick;
        }
        
        private void InitializeGameBoard()
        {
            // Initialize the Rectangle matrix for the game board
            gameBoard = new Rectangle[ROWS, COLUMNS];
            gamePieces = new Ellipse[ROWS, COLUMNS];
            boardState = new int[ROWS, COLUMNS];
            
            // Create Rectangle controls for each cell
            for (int row = 0; row < ROWS; row++)
            {
                for (int col = 0; col < COLUMNS; col++)
                {
                    // Create the background rectangle (cell)
                    Rectangle cell = new Rectangle
                    {
                        Style = (Style)FindResource("GameCellStyle"),
                        Name = $"Cell_{row}_{col}"
                    };
                    
                    // Position the rectangle in the grid
                    Grid.SetRow(cell, row);
                    Grid.SetColumn(cell, col);
                    GameBoardGrid.Children.Add(cell);
                    gameBoard[row, col] = cell;
                    
                    // Create the game piece (ellipse) for this cell
                    Ellipse piece = new Ellipse
                    {
                        Style = (Style)FindResource("PlayerPieceStyle"),
                        Fill = Brushes.Transparent,
                        Visibility = Visibility.Hidden
                    };
                    
                    Grid.SetRow(piece, row);
                    Grid.SetColumn(piece, col);
                    GameBoardGrid.Children.Add(piece);
                    gamePieces[row, col] = piece;
                    
                    // Initialize board state
                    boardState[row, col] = 0; // 0 = empty, 1 = player, 2 = CPU
                }
            }
        }
        
        private void InitializeColumnButtons()
        {
            // Create column buttons for dropping pieces
            for (int col = 0; col < COLUMNS; col++)
            {
                Button columnButton = new Button
                {
                    Content = $"▼",
                    FontSize = 16,  // Slightly smaller for better responsiveness
                    FontWeight = FontWeights.Bold,
                    Style = (Style)FindResource("GameButtonStyle"),
                    Tag = col
                };
                
                columnButton.Click += ColumnButton_Click;
                Grid.SetColumn(columnButton, col);
                ColumnButtonsGrid.Children.Add(columnButton);
            }
        }
        
        private async void ColumnButton_Click(object sender, RoutedEventArgs e)
        {
            if (!gameActive || !isPlayerTurn || animationTimer.IsEnabled || isProcessingMove) 
            {
                if (!gameActive)
                {
                    MessageBox.Show("Please start a new game first by clicking the 'New Game' button.", 
                        "Game Not Started", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else if (isProcessingMove)
                {
                    // Silently ignore clicks while processing - no need to show message
                    return;
                }
                return;
            }
            
            Button clickedButton = (Button)sender;
            int column = (int)clickedButton.Tag;
            
            await MakePlayerMove(column);
        }
        
        private async Task MakePlayerMove(int column)
        {
            if (currentGame == null || currentPlayer == null) 
            {
                if (currentPlayer == null)
                {
                    MessageBox.Show("Please login first to make a move.", "Login Required", MessageBoxButton.OK, MessageBoxImage.Warning);
                    ShowLoginDialog();
                }
                return;
            }
            
            // Prevent multiple simultaneous moves
            if (isProcessingMove)
            {
                return;
            }
            isProcessingMove = true;
            
            try
            {
                // Make move on server
                var response = await apiService.MakeMove(currentGame.Id, column);
                
                if (!response.Success)
                {
                    MessageBox.Show(response.Message, "Move Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                
                if (response.Game == null)
                {
                    return;
                }
                
                // Update current game state
                currentGame = response.Game;
                
                // Update board state from server (convert jagged array to 2D array)
                boardState = GameDto.To2DArray(response.Game.Board);
                
                // Add a short delay before updating the visual board to show the move
                await Task.Delay(300);
                
                // Update visual board to match server state
                await UpdateVisualBoard();
                
                // Save game state locally
                await SaveGameState();
                
                // Check game status
                if (currentGame.Status == "Won")
                {
                    await SaveGameState(); // Save before handling win
                    await HandleGameWin(true);
                }
                else if (currentGame.Status == "Lost")
                {
                    // Show CPU thinking animation
                    await Task.Delay(800); // CPU "thinking" delay
                    
                    await SaveGameState(); // Save before handling loss
                    await HandleGameWin(false);
                }
                else if (currentGame.Status == "Draw")
                {
                    await SaveGameState(); // Save before handling draw
                    await HandleGameDraw();
                }
                else
                {
                    // Game continues - show CPU thinking if there's a CPU move
                    if (response.CpuMove.HasValue)
                    {
                        await Task.Delay(800); // CPU "thinking" delay
                    }
                    
                    UpdateGameStatus();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error making move: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                // Always clear the processing flag
                isProcessingMove = false;
            }
        }
        
        private async Task UpdateVisualBoard()
        {
            // Store previous board state to detect new pieces
            var previousState = new int[ROWS, COLUMNS];
            for (int row = 0; row < ROWS; row++)
            {
                for (int col = 0; col < COLUMNS; col++)
                {
                    var piece = gamePieces[row, col];
                    previousState[row, col] = piece.Visibility == Visibility.Visible ? 
                        (piece.Fill == Brushes.Red ? 1 : 2) : 0;
                }
            }
            
            // Find new pieces and separate player vs CPU moves
            var newPlayerPieces = new List<(int row, int col)>();
            var newCpuPieces = new List<(int row, int col)>();
            
            for (int row = 0; row < ROWS; row++)
            {
                for (int col = 0; col < COLUMNS; col++)
                {
                    var piece = gamePieces[row, col];
                    var newValue = boardState[row, col];
                    var oldValue = previousState[row, col];
                    
                    if (newValue != oldValue && newValue != 0)
                    {
                        // This is a new piece - categorize by player type
                        if (newValue == 1) // Player piece
                        {
                            newPlayerPieces.Add((row, col));
                        }
                        else if (newValue == 2) // CPU piece
                        {
                            newCpuPieces.Add((row, col));
                        }
                    }
                    
                    // Update the piece immediately for empty or existing pieces
                    switch (newValue)
                    {
                        case 0: // Empty
                            piece.Fill = Brushes.Transparent;
                            piece.Visibility = Visibility.Hidden;
                            break;
                        case 1: // Player
                            if (oldValue == 0) // Only if it's actually new
                            {
                                piece.Fill = Brushes.Red;
                                piece.Visibility = Visibility.Hidden; // Will be shown by animation
                            }
                            else
                            {
                                piece.Fill = Brushes.Red;
                                piece.Visibility = Visibility.Visible;
                            }
                            break;
                        case 2: // CPU
                            if (oldValue == 0) // Only if it's actually new
                            {
                                piece.Fill = Brushes.Blue;
                                piece.Visibility = Visibility.Hidden; // Will be shown by animation
                            }
                            else
                            {
                                piece.Fill = Brushes.Blue;
                                piece.Visibility = Visibility.Visible;
                            }
                            break;
                    }
                }
            }
            
            // ALWAYS animate player pieces first, then CPU pieces
            foreach (var (row, col) in newPlayerPieces)
            {
                await AnimateSimpleFallingPiece(row, col, true); // Player move
                await Task.Delay(100);
            }
            
            // Then animate CPU pieces (if any)
            foreach (var (row, col) in newCpuPieces)
            {
                await AnimateSimpleFallingPiece(row, col, false); // CPU move
                await Task.Delay(100);
            }
        }
        
        private async Task AnimateSimpleFallingPiece(int targetRow, int targetCol, bool isPlayerMove)
        {
            var piece = gamePieces[targetRow, targetCol];
            var startRow = 0; // Start from top
            
            // Create a temporary animated piece
            var animatedPiece = new Ellipse
            {
                Width = piece.ActualWidth > 0 ? piece.ActualWidth : 40,
                Height = piece.ActualHeight > 0 ? piece.ActualHeight : 40,
                Fill = isPlayerMove ? Brushes.Red : Brushes.Blue,
                Stroke = Brushes.DarkBlue,
                StrokeThickness = 2
            };
            
            // Add to game board grid
            Grid.SetRow(animatedPiece, startRow);
            Grid.SetColumn(animatedPiece, targetCol);
            GameBoardGrid.Children.Add(animatedPiece);
            
            // Animate falling down
            for (int currentRow = startRow; currentRow <= targetRow; currentRow++)
            {
                Grid.SetRow(animatedPiece, currentRow);
                await Task.Delay(80); // Falling speed
            }
            
            // Remove animated piece and show final piece
            GameBoardGrid.Children.Remove(animatedPiece);
            piece.Visibility = Visibility.Visible;
            
            // Add a subtle glow effect
            await AnimateSimpleGlow(piece);
        }
        
        private async Task AnimateSimpleGlow(Ellipse piece)
        {
            var originalOpacity = piece.Opacity;
            
            // Quick glow effect
            piece.Opacity = 0.6;
            await Task.Delay(100);
            piece.Opacity = 1.0;
            await Task.Delay(100);
            piece.Opacity = originalOpacity;
        }
        
        // FindLowestEmptyRow removed - server handles piece placement logic
        
        private async Task AnimateDropPiece(int targetRow, int targetColumn, bool isPlayerMove)
        {
            // Show animation status
            


            // Create the dropping piece
            droppingPiece = new Ellipse
            {
                Width = 40,
                Height = 40,
                Fill = isPlayerMove ? Brushes.Red : Brushes.Yellow,
                Stroke = Brushes.DarkBlue,
                StrokeThickness = 2
            };
            
            // Position the piece at the top of the column
            Canvas.SetLeft(droppingPiece, targetColumn * 60 + 10);
            Canvas.SetTop(droppingPiece, -50);
            
            // Set animation parameters
            animationColumn = targetColumn;
            animationRow = targetRow;
            
            // Start the animation
            animationTimer.Start();
            
            // Wait for animation to complete
            await Task.Delay(1000);
            
            // Place the final piece
            gamePieces[targetRow, targetColumn].Fill = isPlayerMove ? Brushes.Red : Brushes.Yellow;
            gamePieces[targetRow, targetColumn].Visibility = Visibility.Visible;
            
            // Add glowing effect
            await AnimateGlowEffect(targetRow, targetColumn);
            
            // Hide animation status
            

        }
        
        private void AnimationTimer_Tick(object? sender, EventArgs e)
        {
            if (droppingPiece == null) return;
            
            // Move the piece down
            double currentTop = Canvas.GetTop(droppingPiece);
            double targetTop = animationRow * 60 + 10;
            
            if (currentTop < targetTop)
            {
                Canvas.SetTop(droppingPiece, currentTop + animationSpeed);
                
                // Add bounce effect when close to target
                if (currentTop + animationSpeed >= targetTop - 20)
                {
                    animationSpeed = Math.Max(2.0, animationSpeed * 0.8);
                }
            }
            else
            {
                // Animation complete
                animationTimer.Stop();
                droppingPiece = null;
                animationSpeed = 8.0; // Reset speed
            }
        }
        
        private async Task AnimateGlowEffect(int row, int column)
        {
            var piece = gamePieces[row, column];
            
            // Create glow animation
            var scaleTransform = new ScaleTransform(1.0, 1.0);
            piece.RenderTransform = scaleTransform;
            piece.RenderTransformOrigin = new Point(0.5, 0.5);
            
            // Scale up and down animation
            var scaleAnimation = new DoubleAnimation
            {
                From = 1.0,
                To = 1.3,
                Duration = TimeSpan.FromMilliseconds(300),
                AutoReverse = true
            };
            
            scaleTransform.BeginAnimation(ScaleTransform.ScaleXProperty, scaleAnimation);
            scaleTransform.BeginAnimation(ScaleTransform.ScaleYProperty, scaleAnimation);
            
            await Task.Delay(600);
        }
        
        private void CpuMoveTimer_Tick(object? sender, EventArgs e)
        {
            // CPU moves are now handled by the server
            // This timer is no longer needed but kept for compatibility
            cpuMoveTimer.Stop();
            colorChangeTimer.Stop();

        }
        
        private void AnimateThinking()
        {
            // CPU thinking animation - removed status text since UI was updated
            colorChangeTimer.Start();
        }
        
        private void ColorChangeTimer_Tick(object? sender, EventArgs e)
        {
            // Animation timer - kept for potential future use
            // Status text was removed from UI
        }
        
        // Client-side win checking removed - server handles all game logic
        
        private async Task HandleGameWin(bool playerWon)
        {
            gameActive = false;
            colorChangeTimer.Stop();
            
            if (playerWon)
            {
                GameStatusText.Text = "You Win!";
                GameStatusText.Foreground = Brushes.Green;
                await AnimateWinCelebration();
            }
            else
            {
                GameStatusText.Text = "CPU Wins!";
                GameStatusText.Foreground = Brushes.Red;
            }
            
            CurrentPlayerText.Text = "Game Over";
            
            // Update statistics
            if (currentPlayer != null)
            {
                if (playerWon)
                    currentPlayer.GamesWon++;
                else
                    currentPlayer.GamesLost++;
                
                currentPlayer.GamesPlayed++;
                UpdateStatistics();
                
                // Save to server
                await apiService.UpdatePlayerStatistics(currentPlayer);
            }
            
            // Update game status in local database
            if (currentGame != null && currentPlayer != null)
            {
                string finalStatus = playerWon ? "Won" : "Lost";
                await gameService.UpdateGameStatus(currentPlayer.Id, currentGame.Id, finalStatus);
                
                // Final save of the complete game state
                await SaveGameState();
            }
        }
        
        private async Task HandleGameDraw()
        {
            gameActive = false;
            colorChangeTimer.Stop();
            
            GameStatusText.Text = "It's a Draw!";
            GameStatusText.Foreground = Brushes.Blue;
            CurrentPlayerText.Text = "Game Over";
            
            // Update statistics
            if (currentPlayer != null)
            {
                currentPlayer.GamesPlayed++;
                UpdateStatistics();
                await apiService.UpdatePlayerStatistics(currentPlayer);
            }
            
            // Update game status in local database
            if (currentGame != null && currentPlayer != null)
            {
                await gameService.UpdateGameStatus(currentPlayer.Id, currentGame.Id, "Draw");
                
                // Final save of the complete game state
                await SaveGameState();
            }
        }
        
        private async Task AnimateWinCelebration()
        {
            // Find winning pieces (4 in a row)
            var winningPieces = FindWinningPieces();
            
            if (winningPieces.Count == 0) return;
            
            // Create celebration animation with color changes
            for (int i = 0; i < 5; i++)
            {
                // Flash only the winning pieces
                foreach (var (row, col) in winningPieces)
                {
                    gamePieces[row, col].Fill = i % 2 == 0 ? Brushes.LimeGreen : 
                        (boardState[row, col] == 1 ? Brushes.Red : Brushes.Blue);
                }
                
                await Task.Delay(300);
            }
        }
        
        private List<(int row, int col)> FindWinningPieces()
        {
            var winningPieces = new List<(int row, int col)>();
            
            // Check horizontal
            for (int row = 0; row < ROWS; row++)
            {
                for (int col = 0; col <= COLUMNS - 4; col++)
                {
                    if (boardState[row, col] != 0 &&
                        boardState[row, col] == boardState[row, col + 1] &&
                        boardState[row, col] == boardState[row, col + 2] &&
                        boardState[row, col] == boardState[row, col + 3])
                    {
                        for (int i = 0; i < 4; i++)
                        {
                            winningPieces.Add((row, col + i));
                        }
                        return winningPieces;
                    }
                }
            }
            
            // Check vertical
            for (int row = 0; row <= ROWS - 4; row++)
            {
                for (int col = 0; col < COLUMNS; col++)
                {
                    if (boardState[row, col] != 0 &&
                        boardState[row, col] == boardState[row + 1, col] &&
                        boardState[row, col] == boardState[row + 2, col] &&
                        boardState[row, col] == boardState[row + 3, col])
                    {
                        for (int i = 0; i < 4; i++)
                        {
                            winningPieces.Add((row + i, col));
                        }
                        return winningPieces;
                    }
                }
            }
            
            // Check diagonal (top-left to bottom-right)
            for (int row = 0; row <= ROWS - 4; row++)
            {
                for (int col = 0; col <= COLUMNS - 4; col++)
                {
                    if (boardState[row, col] != 0 &&
                        boardState[row, col] == boardState[row + 1, col + 1] &&
                        boardState[row, col] == boardState[row + 2, col + 2] &&
                        boardState[row, col] == boardState[row + 3, col + 3])
                    {
                        for (int i = 0; i < 4; i++)
                        {
                            winningPieces.Add((row + i, col + i));
                        }
                        return winningPieces;
                    }
                }
            }
            
            // Check diagonal (top-right to bottom-left)
            for (int row = 0; row <= ROWS - 4; row++)
            {
                for (int col = 3; col < COLUMNS; col++)
                {
                    if (boardState[row, col] != 0 &&
                        boardState[row, col] == boardState[row + 1, col - 1] &&
                        boardState[row, col] == boardState[row + 2, col - 2] &&
                        boardState[row, col] == boardState[row + 3, col - 3])
                    {
                        for (int i = 0; i < 4; i++)
                        {
                            winningPieces.Add((row + i, col - i));
                        }
                        return winningPieces;
                    }
                }
            }
            
            return winningPieces;
        }
        
        private void UpdateGameStatus()
        {
            if (!gameActive)
            {
                GameStatusText.Text = "Not Started";
                CurrentPlayerText.Text = "Press New Game";
                return;
            }
            
            GameStatusText.Text = "In Progress";
            CurrentPlayerText.Text = isPlayerTurn ? "Your Turn" : "CPU's Turn";
            GameStatusText.Foreground = isPlayerTurn ? Brushes.Green : Brushes.Blue;
        }
        
        private void UpdateStatistics()
        {
            if (currentPlayer != null)
            {
                GamesWonText.Text = $"Games Won: {currentPlayer.GamesWon}";
                GamesLostText.Text = $"Games Lost: {currentPlayer.GamesLost}";
                GamesPlayedText.Text = $"Games Played: {currentPlayer.GamesPlayed}";
            }
            else
            {
                GamesWonText.Text = "Games Won: --";
                GamesLostText.Text = "Games Lost: --";
                GamesPlayedText.Text = "Games Played: --";
            }
        }
        
        private async void NewGameButton_Click(object sender, RoutedEventArgs e)
        {
            await StartNewGame();
        }
        
        private async Task StartNewGame()
        {
            try
            {
                if (currentPlayer == null)
                {
                    MessageBox.Show("Please login first to start a game.", "Login Required", MessageBoxButton.OK, MessageBoxImage.Warning);
                    ShowLoginDialog();
                    return;
                }

                // Start new game on server
                var response = await apiService.StartGame(currentPlayer.PlayerId);
                
                if (!response.Success)
                {
                    MessageBox.Show(response.Message, "Game Start Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                
                if (response.Game == null)
                {
                    return;
                }
                
                // Set current game
                currentGame = response.Game;
                
                // Reset game state
                gameActive = true;
                isPlayerTurn = true;
                isProcessingMove = false; // Reset processing flag for new game
                
                // Update board state from server (convert jagged array to 2D array)
                boardState = GameDto.To2DArray(response.Game.Board);
                
                // Update visual board to match server state
                await UpdateVisualBoard();
                
                // Reset timers
                animationTimer.Stop();
                cpuMoveTimer.Stop();
                colorChangeTimer.Stop();
                
                UpdateGameStatus();
                UpdatePlayerInfo();

                // Save initial game state
                await SaveGameState();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error starting new game: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        
        private void ConnectButton_Click(object sender, RoutedEventArgs e)
        {
            ShowLoginDialog();
        }
        
        private async Task SaveGameState()
        {
            if (currentGame == null || currentPlayer == null) 
            {
                return;
            }
            
            try
            {
                await gameService.SaveGameState(currentPlayer.Id, boardState, isPlayerTurn, currentGame.Id);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving game state: {ex.Message}", "Save Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        

        
        private async void LoadGameButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (currentPlayer == null)
                {
                    MessageBox.Show("Please connect to server first!", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                
                var restoreWindow = new GameRestoreWindow(currentPlayer.Id);
                restoreWindow.Owner = this; // Set the owner to prevent crashes
                restoreWindow.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                
                if (restoreWindow.ShowDialog() == true && restoreWindow.SelectedGame != null)
                {
                    await RestoreGame(restoreWindow.SelectedGame);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading games: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        
        private async Task RestoreGame(SavedGame savedGame)
        {
            try
            {
                if (savedGame == null)
                {
                    MessageBox.Show("Invalid game data selected.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                // Convert string back to 2D array
                boardState = ConvertStringToBoardState(savedGame.BoardStateJson);
                isPlayerTurn = savedGame.IsPlayerTurn;
                gameActive = true;
                
                // Create a mock current game for UI purposes
                currentGame = new GameDto
                {
                    Id = savedGame.GameId,
                    Status = savedGame.GameStatus,
                    Board = GameDto.From2DArray(boardState), // Convert 2D array to jagged array
                    CurrentPlayer = savedGame.IsPlayerTurn ? "Player" : "CPU"
                };
                
                // Update visual board to match restored state
                await UpdateVisualBoard();
                
                UpdateGameStatus();
                MessageBox.Show("Game restored successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error restoring game: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                
                // Reset game state on error
                gameActive = false;
                isPlayerTurn = true;
                UpdateGameStatus();
            }
        }

        private static int[,] ConvertStringToBoardState(string boardStateString)
        {
            try
            {
                if (string.IsNullOrEmpty(boardStateString)) 
                    return new int[6, 7];
                
                var rows = boardStateString.Split(';');
                if (rows.Length == 0) 
                    return new int[6, 7];
                
                int rowCount = rows.Length;
                int colCount = rows[0].Split(',').Length;
                var result = new int[rowCount, colCount];
                
                for (int i = 0; i < rowCount; i++)
                {
                    var cols = rows[i].Split(',');
                    for (int j = 0; j < colCount && j < cols.Length; j++)
                    {
                        if (int.TryParse(cols[j], out int value))
                        {
                            result[i, j] = value;
                        }
                    }
                }
                
                return result;
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Error converting string to board state: {ex.Message}", "Conversion Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                return new int[6, 7];
            }
        }
        
        // AnimateGameRestoration removed - using UpdateVisualBoard instead
        
        private int CountPieces(int[,] board)
        {
            int count = 0;
            for (int row = 0; row < ROWS; row++)
            {
                for (int col = 0; col < COLUMNS; col++)
                {
                    if (board[row, col] != 0) count++;
                }
            }
            return count;
        }
    }
}

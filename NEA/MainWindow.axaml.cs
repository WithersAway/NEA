using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Threading;

namespace NEA
{
    public partial class MainWindow : Window
    {
        const double moveConstant = 5d;
        const double enemyMove = 1d;
        public Game GameObject;
        public List<Enemy> enemies = new();
        List<Rectangle> playerProjectiles = new();
        public HashSet<Key> keysPressed = new();
        bool gameOver = false;
        private Point mousePosition;
        private readonly DispatcherTimer gameTimer;

        public MainWindow()
        {
            InitializeComponent();
            
            List<string> testingList =
            [
                // temporary testing values for player stats & name & class
                "TempName",
                "10",
                "10",
                "10",
                "10",
                "10",
                "10",
                "10",
                "15",
                "0"
            ]; // 10 in all stats, 15hp, warrior, name = tempname
            
            List<int> enemystats =
            [
                10,
                10,
                10,
                10,
                10,
                10,
                10,
                15,
            ];
            // Setting up player sprite
            Rectangle PlayerRect = new()
            { 
                Name = "PlayerRect", 
                Fill = Brushes.HotPink, 
                Height = 50, 
                Stroke = Brushes.Black, 
                Width = 30 
            };
            
            GameObject = new Game(testingList, PlayerRect, 1);
           
            for (int i = 0; i < 5; i++)
            {
                enemies.Add(new Enemy(new Rectangle { Fill = Brushes.Black, Height = 35, Width = 25 }, enemystats));
            }
            
            int ii = 1;
            foreach (Enemy Enemy in enemies)
            {
                MyCanvas.Children.Add(Enemy.enemy);
                Canvas.SetTop(Enemy.enemy, 160 * ii);
                Canvas.SetLeft(Enemy.enemy, 160 * ii);
                ii++;
            }
            
            MyCanvas.Children.Add(GameObject.player.PlayerRectangle);
            Canvas.SetTop(GameObject.player.PlayerRectangle, 100);
            Canvas.SetLeft(GameObject.player.PlayerRectangle, 100);
           
            // Set up event handlers
            KeyDown += MainWindow_KeyDown;
            KeyUp += MainWindow_KeyUp;
            PointerPressed += MainWindow_PointerPressed;
            PointerMoved += MainWindow_PointerMoved;

            // Set up game loop timer
            gameTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(16.66) // ~60 FPS
            };
            gameTimer.Tick += GameTimer_Tick;
            gameTimer.Start();
        }
        
        private DateTime lastDamageTime = DateTime.MinValue;
        private const double iFrameLength = 0.25d; // Seconds of invincibility after taking damage

        private void MainWindow_PointerPressed(object? sender, PointerPressedEventArgs e)
        {
            ShootProjectile(GameObject.player.PlayerRectangle);
        }

        private void MainWindow_KeyUp(object? sender, KeyEventArgs e)
        {
            keysPressed.Remove(e.Key);
        }

        private void GameTimer_Tick(object? sender, EventArgs e)
        {
            Update(GameObject.player, enemies);
        }

        private void MainWindow_PointerMoved(object? sender, PointerEventArgs e)
        {
            mousePosition = e.GetPosition(MyCanvas);
        }
        
        private async void DealDamageToPlayer()
        {
            GameObject.player.PlayerStats.Hp -= 1; // Decrease HP by 1 for damage
            
            // Update HP display in window title
            Title = $"HP: {GameObject.player.PlayerStats.Hp}";
            
            // Check for game over
            if (GameObject.player.PlayerStats.Hp <= 0)
            {
                gameOver = true;
                
                // Stop the game timer
                gameTimer.Stop();
                
                // Display Game Over message and stop game
                var messageBox = new Window()
                {
                    Title = "Game Over",
                    Width = 300,
                    Height = 150,
                    WindowStartupLocation = WindowStartupLocation.CenterOwner,
                    Content = new StackPanel
                    {
                        Children =
                        {
                            new TextBlock { Text = "Game Over!", HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center, Margin = new Thickness(20) },
                            new TextBlock { Text = "Close window to continue...", HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center, Margin = new Thickness(20)}
                        }
                    }
                };
                
                await messageBox.ShowDialog(this);
                Close();
            }
        }
        
        private int currentStage = 1;
        private bool stageTransitioning = false;
        private readonly List<int> enemyStats = new List<int>
        {
            10, 10, 10, 10, 10, 10, 10, 15
        };
        
        private void Update(Player player, List<Enemy> enemies)
        {
            if (gameOver || stageTransitioning) {
             return;
            }

            // Create a list to store enemies that need to be removed
            List<Enemy> enemiesToRemove = new List<Enemy>();
            List<Rectangle> projectilesToRemove = new List<Rectangle>();

            foreach (Enemy enemy in enemies)
            {
                EnemyMovement(player.PlayerRectangle, enemy);
            }
            
            if (Canvas.GetTop(player.PlayerRectangle) > 600)
            {
                Canvas.SetTop(player.PlayerRectangle, 0);
            }

            double x = Canvas.GetLeft(player.PlayerRectangle);
            double y = Canvas.GetTop(player.PlayerRectangle);

            foreach (Rectangle projectile in playerProjectiles)
            {
                foreach (Enemy enemy in enemies)
                {
                    if (CheckCollisionOfTwoRects(projectile, enemy.enemy))
                    {
                        enemiesToRemove.Add(enemy);
                        projectilesToRemove.Add(projectile);
                        MyCanvas.Children.Remove(enemy.enemy);
                        MyCanvas.Children.Remove(projectile);
                    }
                }
            }

            // Remove marked enemies and projectiles
            foreach (Enemy enemy in enemiesToRemove)
            {
                enemies.Remove(enemy);
            }
            foreach (Rectangle projectile in projectilesToRemove)
            {
                playerProjectiles.Remove(projectile);
            }

            // Check if all enemies are dead
            if (enemies.Count == 0 && !stageTransitioning)
            {
                StartNextStage();
            }
            
            if (keysPressed.Contains(Key.W)) { y -= moveConstant; }
            if (keysPressed.Contains(Key.S)) { y += moveConstant; }
            if (keysPressed.Contains(Key.A)) { x -= moveConstant; }
            if (keysPressed.Contains(Key.D)) { x += moveConstant; }

            Canvas.SetTop(player.PlayerRectangle, y);
            Canvas.SetLeft(player.PlayerRectangle, x);

            foreach (Rectangle projectile in playerProjectiles)
            {
                MoveProjectiles(projectile);
            }
        }
        
        private async void StartNextStage()
        {
            stageTransitioning = true;
            currentStage++;

            // Show stage message
            TextBlock stageMessage = new()
            {
                Text = $"Stage {currentStage}",
                FontSize = 48,
                Foreground = Brushes.White,
                Background = Brushes.Black,
                Padding = new Thickness(20),
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center
            };

            // Add message to canvas
            MyCanvas.Children.Add(stageMessage);
            Canvas.SetLeft(stageMessage, (MyCanvas.Bounds.Width - 200) / 2);
            Canvas.SetTop(stageMessage, (MyCanvas.Bounds.Height - 100) / 2);

            // Wait for 2 seconds
            await Task.Delay(2000);
            
            MyCanvas.Children.Remove(stageMessage);

            // Spawn new enemies
            SpawnEnemies();

            stageTransitioning = false;
        }

        private void SpawnEnemies()
        {
            // Calculate number of enemies for new stage (starting amount + stage number - 1)
            int enemyCount = 5 + (currentStage - 1);

            // Clear any remaining projectiles
            foreach (var projectile in playerProjectiles.ToList())
            {
                MyCanvas.Children.Remove(projectile);
            }
            playerProjectiles.Clear();

            // Spawn new enemies
            for (int i = 0; i < enemyCount; i++)
            {
                Enemy newEnemy = new(new Rectangle 
                { 
                    Fill = Brushes.Black, 
                    Height = 35, 
                    Width = 25 
                }, enemyStats);
                
                enemies.Add(newEnemy);
                MyCanvas.Children.Add(newEnemy.enemy);

                // Position enemies in a circle
                double angle = Math.PI * 2 * i / enemyCount;
                double radius = 200; // Distance from center
                double centerX = MyCanvas.Bounds.Width / 2;
                double centerY = MyCanvas.Bounds.Height / 2;

                Canvas.SetLeft(newEnemy.enemy, centerX + Math.Cos(angle) * radius);
                Canvas.SetTop(newEnemy.enemy, centerY + Math.Sin(angle) * radius);
            }
        }
            
        private void MainWindow_KeyDown(object? sender, KeyEventArgs e)
        {
            // simple wasd movement for player
            keysPressed.Add(e.Key);
        }
        
        private Rect RectConverter(Rectangle rectangle) // currently unused after transition to Avalonia
        // takes a rectangle and outputs the position and size as a Rect to be used in CheckCollisionOfTwoRects in
        // IntersectsWith method to allow to check collisions of player w/ enemy
        {
            double x = Canvas.GetLeft(rectangle);
            double y = Canvas.GetTop(rectangle);
            double width = rectangle.Width;
            double height = rectangle.Height;
            
            return new Rect(x, y, width, height);
        }
        
        private void EnemyMovement(Rectangle player, Enemy enemy)
        {
            // check if a collision is present
            if (CheckCollisionOfTwoRects(player, enemy.enemy))
            {
                // Check if enough time has passed since last damage (invincibility frames)
                if ((DateTime.Now - lastDamageTime).TotalSeconds >= iFrameLength)
                {
                    DealDamageToPlayer();
                    lastDamageTime = DateTime.Now;
                }
            }

            double currentEnemyX = Canvas.GetLeft(enemy.enemy);
            double currentEnemyY = Canvas.GetTop(enemy.enemy);
            double playerX = Canvas.GetLeft(player);
            double playerY = Canvas.GetTop(player);

            // Calculate distances
            double xDist = playerX - currentEnemyX;
            double yDist = playerY - currentEnemyY;
            double directDistance = Math.Sqrt(xDist * xDist + yDist * yDist);

            // Calculate movement
            double xToMove = enemyMove * (xDist / directDistance);
            double yToMove = enemyMove * (yDist / directDistance);

            double nextX = currentEnemyX + xToMove;
            double nextY = currentEnemyY + yToMove;
            
            Canvas.SetLeft(enemy.enemy, nextX);
            if (CheckCollisionOfTwoRects(player, enemy.enemy))
            {
                Canvas.SetLeft(enemy.enemy, currentEnemyX);
            }
            
            Canvas.SetTop(enemy.enemy, nextY);
            if (CheckCollisionOfTwoRects(player, enemy.enemy))
            {
                Canvas.SetTop(enemy.enemy, currentEnemyY);
            }
            // make enemies look at player
            double AngleToRotate = Math.Atan2(yDist, xDist) * (180 / Math.PI);
            enemy.enemy.RenderTransform = new RotateTransform(AngleToRotate);

            MyCanvas.Children.Remove(enemy.enemy);
            MyCanvas.Children.Add(enemy.enemy);
        }

        private static bool CheckCollisionOfTwoRects(Rectangle rect1, Rectangle rect2)
        {
            // Get positions
            double x1 = Canvas.GetLeft(rect1);
            double y1 = Canvas.GetTop(rect1);
            double x2 = Canvas.GetLeft(rect2);
            double y2 = Canvas.GetTop(rect2);

            // Add small buffer (1 pixel) to make collisions cleaner
            const double buffer = 1.0;

            // Check for intersection with buffer
            return !(x1 + rect1.Width + buffer < x2 || x2 + rect2.Width + buffer < x1 || y1 + rect1.Height + buffer < y2 || y2 + rect2.Height + buffer < y1);
        }
        
        private void ShootProjectile(Rectangle Sender)
        {
            Rectangle projectile = new Rectangle { Fill = Brushes.Black, Height = 10, Width = 10 };
            MyCanvas.Children.Add(projectile);
    
            double startX = Canvas.GetLeft(Sender) + Sender.Width / 2;
            double startY = Canvas.GetTop(Sender) + Sender.Height / 2;
    
            // Calculate direction vector for projectile
            double dirX = mousePosition.X - startX;
            double dirY = mousePosition.Y - startY;
    
            // Normalize the direction vector with Pythagoras
            double length = Math.Sqrt(dirX * dirX + dirY * dirY);
            dirX /= length;
            dirY /= length;
    
            // Store direction with the projectile
            projectile.Tag = new Vector(dirX, dirY);
    
            Canvas.SetTop(projectile, startY);
            Canvas.SetLeft(projectile, startX);
            playerProjectiles.Add(projectile);
        }
        
        private static void MoveProjectiles(Rectangle projectile)
        {
            #pragma warning disable CS8605
            Vector direction = (Vector)projectile.Tag;
            #pragma warning restore CS8605

            double speed = moveConstant * 0.5;
    
            Canvas.SetLeft(projectile, Canvas.GetLeft(projectile) + direction.X * speed);
            Canvas.SetTop(projectile, Canvas.GetTop(projectile) + direction.Y * speed);
        }
    }
}
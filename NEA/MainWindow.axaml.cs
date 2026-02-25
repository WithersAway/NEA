using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia; //avalonia is a FOSS cross-platform WPF port to allow for development on Linux
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Input;
using Avalonia.Markup.Xaml.Converters;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Threading;
using Avalonia.VisualTree;

namespace NEA
{
    public partial class MainWindow : Window
    {
        #region InitVariables
        public bool hardmode = false;
        public bool EnemiesUnstuckThisRound = false;
        public bool doubleshot = false;
        private bool saveClicked = false;
        private bool callShop = false;
        private List<Rectangle> ammopickups = [];
        private readonly List<string> upgrades = ["Damage +1", "Damage +2", "Damage +3", "Heal", "Enemy Slow", "Projectile Size Up 25%", "Scavenger (10%)", 
            "Fire Rate Up 10%", "Fire Rate Up 20%", "Fire Rate Up 30%", "Speed 10%"];
        private Dictionary<string, Buff> UpgradeEffects = [];
        private TextBox InputPath;
        public DateTime lastPlayerCollisionTime = DateTime.MinValue;
        double moveModifier = 1d;
        double projectilespeed = 1d;
        const double moveConstant = 5d;
        const double stuckMove = 10d;
        double enemyMove = 1d;
        public Game GameObject;
        public List<Enemy> enemies = [];
        List<Rectangle> playerProjectiles = [];
        List<Obstacle> obstacles = [];
        public HashSet<Key> keysPressed = [];
        bool gameOver = false;
        private Point mousePosition;
        private Label playerAmmo;
        private Label PlayerHealth;
        private readonly DispatcherTimer gameTimer;
        private bool pauseMenuOpen = false;
        private int currentStage = 1;
        private bool stageTransitioning = false;
        private DateTime lastDamageTime = DateTime.MinValue;
        private DateTime lastShotTime = DateTime.MinValue;
        private const double iFrameLength = 0.5d; // Seconds of invincibility after taking damage
        private double PlayerFireRate = 1;
        Bitmap GoblinTexture = new("goblin.png");
        private double PlayerFireRateBoost = 1;
        private double projectilespeedbase = 5d;
        private Bitmap playerSprite = new Bitmap("playerSprite.png");
        public Image MapImage = new Image();
        //public Image bg = new Image();
        
        WriteableBitmap map;

        List<int> enemystats =
            [
                10,
                10,
                10,
                10,
                10,
                10,
                10,
                2,
            ];
        List<int> bossstats =
            [
                10,
                10,
                10,
                10,
                10,
                10,
                10,
                20,
            ];
            #endregion
        public MainWindow()
        {
            InitializeComponent();
            

            foreach (string upgrade in upgrades) //fill upgradeeffects dict with available buffs
            {
                UpgradeEffects.Add(upgrade, new Buff(upgrade));
            }
            List<string> playerStatTestingList =
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
                "3",
                "0"
            ]; // 10 in all stats, 3hp, warrior, name = tempname
            
            
            // Setting up player sprite
            Rectangle PlayerRect = new()
            {
                Name = "PlayerRect",
                Fill = new ImageBrush(playerSprite),
                Stretch = Stretch.Fill,
                Height = 40,
                Stroke = Brushes.Black,
                Width = 40
            };
            
            GameObject = new Game(playerStatTestingList, PlayerRect, 1, 800, 600);
            
            //sets up MapImage (background)
            MapImage.Stretch = Stretch.Fill;
            MapImage.Width = 1920;
            MapImage.Height = 1080;
            MapImage.ZIndex = -1; //forces the background to always render first
            

            Dispatcher.UIThread.Invoke(() => //used to force the map function to be run and assigned to MapImage in the UI Thread, as otherwise there are thread ownership issues 
            //since MapImage is a child of the canvas, it belongs to the UI thread, while map doesnt
            {
                map = setupMap();
                MapImage.Source =map;
            });
            

            MyCanvas.Children.Add(MapImage);
            Canvas.SetLeft(MapImage, 0);
            Canvas.SetTop(MapImage, 0);

            
            GameObject.player.PlayerStats.Hp = 3;
            
            playerAmmo = new()
            {
                Name = "AmmoCounter",
                Height = 30,
                Width = 50,
                FontSize = 25,
                Content = $"Ammo: {GameObject.player.GetAmmo()}",
                Background = Brushes.Aqua
            };

            PlayerHealth = new(){
                Name = "PlayerHealth",
                Height = 30,
                Width = 75,
                FontSize = 25,
                Content = $"HP: {GameObject.player.GetHp()}", 
                Background = Brushes.Aqua
            };
            MyCanvas.Children.Add(playerAmmo);
            
            SpawnEnemies();

            MyCanvas.Children.Add(PlayerHealth);
            Canvas.SetTop(PlayerHealth, 0);
            Canvas.SetLeft(PlayerHealth, 50);

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
                Interval = TimeSpan.FromMilliseconds(16.67)// ~60 FPS, actually just over since 1/60 is 0.01666... sec or 16.666... ms
            };
            gameTimer.Tick += GameTimer_Tick;
            gameTimer.Start();
        }
        private void MainWindow_PointerPressed(object? sender, PointerPressedEventArgs e)
        {
            ShootProjectile(GameObject.player);
        }
        private void MainWindow_KeyUp(object? sender, KeyEventArgs e)
        {
            if(!stageTransitioning){keysPressed.Remove(e.Key);}
            
        }
        private void GameTimer_Tick(object? sender, EventArgs e)
        {
            Update(GameObject.player, enemies);
        }
        private void MainWindow_PointerMoved(object? sender, PointerEventArgs e)
        {
            mousePosition = e.GetPosition(MyCanvas);
        }   
        private WriteableBitmap setupMap(){
            int w, h;
            //adding a way to display the noise generated as part of the background
            w = GameObject.Level.map.GetLength(0); //map is a 2d array so these statements return the dimensions (i.e. width and height)
            h = GameObject.Level.map.GetLength(1);
            WriteableBitmap wb = new WriteableBitmap(
                new PixelSize(w,h),
                new Vector(96,96),
                PixelFormat.Rgba8888,
                AlphaFormat.Opaque
            );
            //writing
            using (var fb = wb.Lock()) 
            {
                unsafe //using pointers to access and change individual pixels in the writable bitmap as rectangles had performance issues
                //this allows me to use one array instead of using 2073600 rectangles
                {
                    byte* p = (byte*) fb.Address.ToPointer();
                    for (int i = 0; i < h-1; i++)
                    {
                        for (int j = 0; j < w; j++)
                        {
                            int currIndex = i * fb.RowBytes + 4*j;
                            bool isWall = GameObject.Level.map[j,i] == TileType.Wall;
                            byte colour;
                            if (isWall)
                            {
                                colour = (byte)0; //if wall, tile is black
                            }
                            else
                            {
                                colour =  (byte)255; //if floor, tile is white
                            }
                            p[currIndex] = colour; //RED value
                            p[currIndex + 1] = colour; //GREEN value
                            p[currIndex + 2] = colour; //BLUE value
                            p[currIndex + 3] = (byte)(255-colour); //ALPHA value
                        }
                    }
                }
            }
            return wb;
        }
        private async void DealDamageToPlayer()
        {
            if (!GameObject.player.InstadeathOn())
            {
                GameObject.player.PlayerStats.Hp -= 1; // Decrease HP by 1 for damage    
            }
            else
            {
                gameOver = true;
            }
            
            
            // Update HP display in label
            PlayerHealth.Content = $"HP: {GameObject.player.GetHp()}";
            MyCanvas.Children.Remove(PlayerHealth);
            MyCanvas.Children.Add(PlayerHealth);
            
            // Check for game over
            if (GameObject.player.PlayerStats.Hp <= 0 || gameOver)
            {
                gameOver = true;
                
                // Stop the game timer to stop other subroutines
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
                            new TextBlock
                            {
                                Text = "Game Over!",
                                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                                Margin = new Thickness(20)
                            },
                            new TextBlock
                            {
                                Text = "Close window to continue...",
                                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                                Margin = new Thickness(20)
                            }
                        }
                    }
                };
                
                await messageBox.ShowDialog(this);
                Close();
            }
        }  
        private void Update(Player player, List<Enemy> enemies)
        {
            PlayerHealth.Content = $"HP: {GameObject.player.GetHp()}";
            MyCanvas.Children.Remove(PlayerHealth);
            MyCanvas.Children.Add(PlayerHealth);
            
            if (gameOver || stageTransitioning) {
             return;
            }
            playerAmmo.Content = player.GetAmmo();
            MyCanvas.Children.Remove(playerAmmo);
            MyCanvas.Children.Add(playerAmmo);
            // Create a list to store enemies that need to be removed
            List<Enemy> enemiesToRemove = new List<Enemy>();
            List<Rectangle> projectilesToRemove = new List<Rectangle>();

            foreach (Enemy enemy in enemies)
            {
                EnemyMovement(player.PlayerRectangle, enemy);
            }
            
            
            int x = (int)Canvas.GetLeft(player.PlayerRectangle);
            int y = (int)Canvas.GetTop(player.PlayerRectangle);

            foreach (Rectangle projectile in playerProjectiles)
            {
                foreach (Enemy enemy in enemies)
                {
                    if (IsTouching(projectile, enemy.enemy))
                    {
                        double currentPlayerDamage = player.getPlayerDamage() * player.getPlayerDamageBase();
                        if (enemy is Boss boss)
                        {
                            // Boss needs 5 * currentStage hits to die
                            bool defeated = false;
                            defeated = boss.ApplyHit(currentPlayerDamage);

                            projectilesToRemove.Add(projectile);

                            if (defeated)
                            {
                                enemiesToRemove.Add(enemy);
                                callShop = true;
                            }

                            MyCanvas.Children.Remove(projectile);
                        }
                        else
                        {
                            
                            enemiesToRemove.Add(enemy);                
                            projectilesToRemove.Add(projectile);                            
                        }
                    }
                }
            }
            List<Rectangle> ammoPickupsToRemove = [];
            foreach (Enemy enemy in enemiesToRemove)
            {
                Rectangle ammopickup = new() 
                {
                    Name = "ammopickup",
                    Fill = Brushes.Green,
                    Height = 10,
                    Stroke = Brushes.Black,
                    Width = 10
                };
                ammopickups.Add(ammopickup);
                MyCanvas.Children.Add(ammopickup);
                Canvas.SetLeft(ammopickup, Canvas.GetLeft(enemy.enemy));
                Canvas.SetTop(ammopickup, Canvas.GetTop(enemy.enemy));
                enemies.Remove(enemy);
                MyCanvas.Children.Remove(enemy.enemy);
            }
            enemiesToRemove.Clear();
            foreach (Rectangle ammo in ammopickups)
            {
                if (IsTouching(ammo, player.PlayerRectangle))
                {
                    player.SetAmmo(Convert.ToInt32(player.GetAmmo()+(player.getScavengeMod()*player.getPlayerAmmoMax())));
                    MyCanvas.Children.Remove(ammo);
                    ammoPickupsToRemove.Add(ammo);

                }
            }
            foreach (Rectangle ammo in ammoPickupsToRemove)
            {
                MyCanvas.Children.Remove(ammo);
                ammopickups.Remove(ammo);
            }
            ammoPickupsToRemove.Clear();

            // Remove marked projectiles
            
            foreach (Rectangle projectile in projectilesToRemove)
            {
                playerProjectiles.Remove(projectile);
                MyCanvas.Children.Remove(projectile);
            }
            projectilesToRemove.Clear();

            // Check if all enemies are dead
            if (enemies.Count == 0 && !stageTransitioning)
            {
                StartNextStage();
            }
            
            if (keysPressed.Contains(Key.Escape) && !pauseMenuOpen)
            {
                PauseMenu();
            }

            if (keysPressed.Contains(Key.F) && !EnemiesUnstuckThisRound)
            {
                EnemiesUnstuckThisRound = true;
                Random r = new Random();
                foreach (Enemy enemy in enemies)
                {
                    Canvas.SetLeft(enemy.enemy, 1920/2 + r.Next(1,50));
                    Canvas.SetTop(enemy.enemy, 1080/2 + r.Next(1,50));
                }
            }
            //player movement
            int collY, collx;
            collY = -1;
            collx = collY;
            if (keysPressed.Contains(Key.W)) { TryMove(ref x, ref y, 0, -1 * (int)Math.Floor(moveConstant), map); }
            if (keysPressed.Contains(Key.S)) { collY = y + (int)player.PlayerRectangle.Height; 
                TryMove(ref x, ref collY, 0, +1 * (int)Math.Floor(moveConstant), map); }
            if (keysPressed.Contains(Key.A)) { TryMove(ref x, ref y, -1 * (int)Math.Floor(moveConstant), 0, map); }
            if (keysPressed.Contains(Key.D)) { collx = x + (int)player.PlayerRectangle.Width; 
                TryMove(ref collx, ref y, +1 * (int)Math.Floor(moveConstant), 0, map);  }

            if (collY != -1)
            {
                y = collY - (int)player.PlayerRectangle.Height;
            }
            else if (collx != -1)
            {
                x = collx - (int)player.PlayerRectangle.Width;
            }
            //forces player to stay on screen
            x = (int)Math.Clamp(x, 0, 1920-player.PlayerRectangle.Width);
            y = (int)Math.Clamp(y, 0, 1080-player.PlayerRectangle.Height);

            Canvas.SetTop(player.PlayerRectangle, y);
            Canvas.SetLeft(player.PlayerRectangle, x);

            foreach (Rectangle projectile in playerProjectiles)
            {
                MoveProjectiles(projectile);
            }
        }
        private Point ScreenToMap(int screenX, int screenY, int renderWidth, int renderHeight, Bitmap map)
        {
            
            double scaleX = map.PixelSize.Width  / (double)renderWidth;
            double scaleY = map.PixelSize.Height / (double)renderHeight;

            int mx = (int)(screenX * scaleX);
            int my = (int)(screenY * scaleY);

            return new Point(mx, my);
        }
        bool IsBlockedScreen(WriteableBitmap map, int screenX, int screenY, int renderWidth, int renderHeight)
        {
            Point p = ScreenToMap(screenX, screenY, renderWidth, renderHeight, map);
            return IsBlocked(map, (int)p.X, (int)p.Y);
        }


        private void OnSaveClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e){
            SaveGame();
        }
        private void OnLoadClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e){
            LoadGame();
        }
        private void OnInfoClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e){
            ShowInfo();
        }
        private void MainWindow_KeyDown(object? sender, KeyEventArgs e)
        {
            // simple wasd movement for player
            if (!stageTransitioning)
            {
                keysPressed.Add(e.Key);    
            }
            
        }
        private async void PauseMenu(){
            pauseMenuOpen = true;
            keysPressed.Remove(Key.Escape);
            gameTimer.Stop();
            var saveButton = new Button
            {
                Content = "Save",
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                Margin = new Thickness(20)
            };
            var loadButton = new Button
            {
                Content = "Load",
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                Margin = new Thickness(20)
            };
            var playerInfoButton = new Button
            {
                Content = "Player Info",
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                Margin = new Thickness(20)

            };
            saveButton.Click += OnSaveClicked;
            loadButton.Click += OnLoadClicked;
            playerInfoButton.Click += OnInfoClicked;
            var messageBox = new Window()
            {
                    Title = "Pause Menu",
                    Width = 450,
                    Height = 350,
                    WindowStartupLocation = WindowStartupLocation.CenterOwner,
                    Content = new StackPanel
                    {
                        Children =
                        {
                            new TextBlock
                            {
                                Text = "Paused.",
                                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                                Margin = new Thickness(20)
                            },
                            new TextBlock
                            {
                                Text = "Close window to continue...",
                                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                                Margin = new Thickness(20)
                            },
                            saveButton,
                            loadButton,
                            playerInfoButton
                        }
                    }
            };    
            await messageBox.ShowDialog(this);
            gameTimer.Start();
            pauseMenuOpen = false;
            
        }
        private async void SaveGame(){
            InputPath = new TextBox
            {
                Width = 200,
                Watermark = "Type path here...",
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                Margin = new Thickness(10)
            };

            var CommitSave = new Button
            {
                Content = "Save File",
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                Margin = new Thickness(20)
            };
            CommitSave.Click += CommitSaveClicked;
            var saveMessageBox = new Window()
                {
                    Title = "Save Game Menu",
                    Width = 450,
                    Height = 300,
                    WindowStartupLocation = WindowStartupLocation.CenterOwner,
                    Content = new StackPanel
                    {
                        Children =
                        {
                            new TextBlock
                            {
                                Text = "Give path to location to save to.",
                                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                                Margin = new Thickness(20)
                            },
                            InputPath,
                            CommitSave
                        }
                    }
                };
                await saveMessageBox.ShowDialog(this);
                
        }
        
        private async void CommitSaveClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e){
            string path = "";
            try
            {
                if (!string.IsNullOrEmpty(InputPath.Text))
                {
                    path = InputPath.Text;
                }
                
            }
            catch (System.ArgumentNullException)
            {

                var ErrorBox = new Window()
                {
                    Title = "Error!",
                    Width = 150,
                    Height = 100,
                    WindowStartupLocation = WindowStartupLocation.CenterOwner,
                    Content = new StackPanel
                    {
                        Children =
                        {
                            new TextBlock
                            {
                                Text = "Please enter a valid path.",
                                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                                Margin = new Thickness(20)
                            },
                            
                        }
                    }
                };
                await ErrorBox.ShowDialog(this);
                return;
            }
            if (path == string.Empty || saveClicked)
            {
                return;
            }
            else
            {
                try
                {
                    try
                    {
                        using (StreamWriter sw = new(path))
                        {
                            
                            sw.WriteLine("##~##");
                            sw.WriteLine(GameObject.floor);
                            sw.WriteLine(GameObject.player.GetHp());
                            sw.WriteLine(GameObject.player.GetWeapon().GetItemName());
                            if (GameObject.player.HasRelic())
                            {
                                sw.WriteLine(GameObject.player.GetRelic().GetItemName());    
                            }
                            else
                            {
                                sw.WriteLine("@");
                            }
                            foreach (Buff upgrade in GameObject.player.PlayerUpgrades)
                            {
                                sw.WriteLine(upgrade.getBuffID());
                            }
                            
                        };
                        saveClicked = true;
                        return;
                    }
                    catch(System.IO.DirectoryNotFoundException)
                    {
                        return;
                    }
                }
                catch (System.ArgumentNullException)
                {
                
                    return;
                }
            }
            
            


        }
        private async void LoadGame(){
            InputPath = new TextBox
            {
                Width = 200,
                Watermark = "Type path here...",
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                Margin = new Thickness(10)
            };

            var CommitLoad = new Button
            {
                Content = "Load File",
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                Margin = new Thickness(20)
            };
            CommitLoad.Click += CommitLoadClicked;
            var saveMessageBox = new Window()
                {
                    Title = "Load Game Menu",
                    Width = 450,
                    Height = 300,
                    WindowStartupLocation = WindowStartupLocation.CenterOwner,
                    Content = new StackPanel
                    {
                        Children =
                        {
                            new TextBlock
                            {
                                Text = "Give path to location to save to.",
                                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                                Margin = new Thickness(20)
                            },
                            InputPath,
                            CommitLoad
                        }
                    }
                };
                await saveMessageBox.ShowDialog(this);
        }
        private async void CommitLoadClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e){
            string path = "";
            try
            {
                if (!string.IsNullOrEmpty(InputPath.Text))
                {
                    path = InputPath.Text;
                }
                
            }
            catch (System.ArgumentNullException)
            {

                var ErrorBox = new Window()
                {
                    Title = "Error!",
                    Width = 150,
                    Height = 100,
                    WindowStartupLocation = WindowStartupLocation.CenterOwner,
                    Content = new StackPanel
                    {
                        Children =
                        {
                            new TextBlock
                            {
                                Text = "Please enter a valid path.",
                                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                                Margin = new Thickness(20)
                            },
                            
                        }
                    }

                };
                await ErrorBox.ShowDialog(this);
                return;
            }
            if (path == string.Empty)
            {
                return;
            }
            else
            {
                try
                {
                    using (StreamReader sr = new(path))
                    {
                        if (sr.ReadLine() != "##~##")
                        {
                            return;
                        }
                        GameObject.floor = int.Parse(sr.ReadLine()) - 1;
                        GameObject.player.PlayerStats.Hp = int.Parse(sr.ReadLine());
                        GameObject.player.playerWeapon = new Weapon(sr.ReadLine(), 1, 1, false, false, 1, false, 1);
                        if (sr.Peek() != '@')
                        {
                            GameObject.player.AddRelic(new Item(sr.ReadLine(), 1, 1, true, false, true, 1 ));
                        }
                        while (sr.Peek() != -1)
                        {
                            OnUpgradePicked(sr.ReadLine());
                        }
                        StartNextStage();
                    };
                    return;
                }
                catch (ArgumentNullException)
                {

                    return;
                }
            }
        }
        private async void ShowInfo(){
            string upgradespicked = "";
            foreach (Buff item in GameObject.player.PlayerUpgrades)
            {
                upgradespicked += item.getBuffID();
                upgradespicked += ", ";
            }
            var PlayerInfo = new Window()
                {
                    Title = "Info",
                    Width = 300,
                    Height = 300,
                    WindowStartupLocation = WindowStartupLocation.CenterOwner,
                    Content = new StackPanel
                    {
                        Children =
                        {
                            new TextBlock
                            {
                                Text = $"Player Weapon: {GameObject.player.GetWeapon().GetItemName()}\nPlayer Damage: {GameObject.player.getPlayerDamageBase()}, Multiplier {GameObject.player.getPlayerDamage()}x \nPlayer Fire Rate: {(PlayerFireRate / PlayerFireRateBoost).ToString("#.##")} second(s) per shot ",
                                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                                Margin = new Thickness(20)
                            },
                            new TextBlock{
                            Text = $"Upgrades Picked: {upgradespicked}",
                            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                            Margin = new Thickness(20)
                            },

                        }
                    }

                };
                await PlayerInfo.ShowDialog(this);
                return;

            
        }
        private void OnUpgradePicked(string key){
            GameObject.player.PlayerUpgrades.Add(UpgradeEffects[key]);
            switch (UpgradeEffects[key].getBuffID())
            {
                case "Damage +1":
                    GameObject.player.setPlayerDamage(0.1);
                    break;
                case "Damage +2":
                    GameObject.player.setPlayerDamage(0.2);
                    break;
                case "Damage +3":
                    GameObject.player.setPlayerDamage(0.3);
                    break;
                case "Heal":
                    GameObject.player.PlayerStats.Hp = 3;
                    PlayerHealth.Content = $"HP: {GameObject.player.PlayerStats.Hp}";
                    MyCanvas.Children.Remove(PlayerHealth);
                    MyCanvas.Children.Add(PlayerHealth);
                    break;
                case "Enemy Slow":
                    enemyMove *= 0.9;
                    break;
                case "Projectile Size Up 25%":
                    GameObject.player.SetProjSize(GameObject.player.GetProjSize() * 1.25);
                    break;
                case "Scavenger (+10% max ammo per pickup)":
                    GameObject.player.setScavengerModifier(0.1);
                    break;
                case "Fire Rate Up 10%":
                    PlayerFireRateBoost += 0.1;
                    break;
                case "Fire Rate Up 20%":
                    PlayerFireRateBoost += 0.2;
                    break;
                case "Fire Rate Up 30%":
                    PlayerFireRateBoost += 0.3;
                    break;
                case "Speed 10%":
                    moveModifier *= 1.1d;
                    break;
                default:
                    break;
            }
            
        }
        private async Task PickUpgrade(){
            Random r = new();
            gameTimer.Stop();
            List<string> UpgradesAvailable = [];
            List<Button> UpgradePickButtons = [];
            for (int i = 0; i < 3; i++)
            {
                var index = i;
                UpgradesAvailable.Add(upgrades[r.Next(upgrades.Count)]);
                var button = new Button{
                    Content = UpgradesAvailable[i],
                    HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                    Margin = new Thickness(20)
                };
                button.Click += (sender, e) => 
                { 
                    OnUpgradePicked(UpgradesAvailable[index]); 
                    (((Button)sender).GetVisualRoot() as Window)?.Close(); 
                };
                UpgradePickButtons.Add(button);
            }
            
            var stackPanel = new StackPanel
            {
                Children =
                {
                    new TextBlock
                    {
                        Text = "Pick an upgrade.",
                        HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                        Margin = new Thickness(20)
                    },
                }
            };
            foreach (Button button in UpgradePickButtons)
            {
                stackPanel.Children.Add(button);
            }
            var saveMessageBox = new Window()
            {
                Title = "Save Game Menu",
                Width = 450,
                Height = 300,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Content = stackPanel

            };
            await saveMessageBox.ShowDialog(this);
            
        }
        private void OnItemBought(Item item){
            if (item is Weapon)
            {
                GameObject.player.playerWeapon = Weapon.convertToWeapon(item, GameObject.floor);
            }
            else
            {
               GameObject.player.AddItemToInventory(item);
            }
            switch (item.GetItemType())
            {
                case "Longbow":
                    PlayerFireRate = 3;
                    GameObject.player.setPlayerDamageBase(5);
                    GameObject.player.setPlayerAmmoMax(10);
                    break;
                case "Shortbow":
                    PlayerFireRate = 0.5;
                    GameObject.player.setPlayerDamageBase(1);
                    GameObject.player.setPlayerAmmoMax(20);
                    break;
                case "Crossbow":
                    PlayerFireRate = 1.25;
                    GameObject.player.setPlayerDamageBase(3);
                    GameObject.player.setPlayerAmmoMax(10);
                    break;
                case "HandCrossbow":
                    PlayerFireRate = 0.75;
                    GameObject.player.setPlayerDamageBase(2);
                    GameObject.player.setPlayerAmmoMax(25);
                    break;
                case "HeavyCrossbow":
                    PlayerFireRate = 2.5;
                    GameObject.player.setPlayerDamageBase(4);
                    GameObject.player.setPlayerAmmoMax(15);
                    break;
                case "Toxicarp":
                    PlayerFireRate = 0.25;
                    GameObject.player.setPlayerDamageBase(1);
                    GameObject.player.setPlayerAmmoMax(100);
                    break;
                case "LightCrossbow":
                    PlayerFireRate = 1;
                    GameObject.player.setPlayerDamageBase(2);
                    GameObject.player.setPlayerAmmoMax(30);
                    break;
                case "Handgun":
                    PlayerFireRate = 1;
                    GameObject.player.setPlayerDamageBase(2);
                    GameObject.player.setPlayerAmmoMax(10);
                    break;
                case "Rifle":
                    PlayerFireRate = 2.5;
                    GameObject.player.setPlayerDamageBase(5);
                    GameObject.player.setPlayerAmmoMax(10);
                    break;
                case "ScopedRifle":
                    PlayerFireRate = 5;
                    GameObject.player.setPlayerDamageBase(10);
                    GameObject.player.setPlayerAmmoMax(15);
                    break;
                case "Pistol":
                    PlayerFireRate = 0.75;
                    GameObject.player.setPlayerDamageBase(2);
                    GameObject.player.setPlayerAmmoMax(12);
                    break;
                case "MoltenFury":
                    PlayerFireRate = 0.5;
                    GameObject.player.setPlayerDamageBase(2);
                    GameObject.player.setPlayerAmmoMax(20);
                    break;
                case "AerialBane":
                    PlayerFireRate = 0;
                    GameObject.player.setPlayerDamageBase(1);
                    GameObject.player.setPlayerAmmoMax(40);
                    break;
                default:
                    break;
            }
            string weaponModifier = null;
            string weaponType;
            if (item is Weapon)
            {

                (weaponModifier, weaponType) = Weapon.GetWeaponAndModifier(Weapon.convertToWeapon(item, GameObject.floor));
            }
            #region ApplyModifier

            //"Strong", "Warped", "Sighted", "Deadly", "Fine", "Grand", "Hasty", "Neat", "Rapid", "Unreal", "Precise", "Masterful", "Antique"
            if (weaponModifier != null)
            {
                switch (weaponModifier)
                {
                    case "Strong":
                        GameObject.player.setPlayerDamageBase((int)Math.Round(GameObject.player.getPlayerDamageBase() * 1.2));
                        break;
                    case "Warped":
                        GameObject.player.setPlayerDamageBase((int)Math.Floor(GameObject.player.getPlayerDamageBase() * 0.9));
                        projectilespeed = 1.25d;
                        PlayerFireRate *= 0.9d;
                        break;
                    case "Sighted":
                        PlayerFireRate *= 0.75d;
                        break;
                    case "Deadly":
                        GameObject.player.setPlayerDamageBase((int)Math.Floor(GameObject.player.getPlayerDamageBase() * 1.1));
                        PlayerFireRate *= 0.9d;
                        break;
                    case "Fine":
                        //no change
                        break;
                    case "Grand":
                        GameObject.player.setPlayerDamageBase((int)Math.Floor(GameObject.player.getPlayerDamageBase() * 1.25));
                        PlayerFireRate *= 1.2;
                        break;
                    case "Hasty":
                        GameObject.player.setPlayerDamageBase((int)Math.Floor(GameObject.player.getPlayerDamageBase() * 0.85));
                        PlayerFireRate *= 0.6d;
                        break;
                    case "Neat":
                        GameObject.player.setPlayerDamageBase((int)Math.Floor(GameObject.player.getPlayerDamageBase() * 1.05));
                        PlayerFireRate*= 0.95d;
                        break;
                    case "Rapid":
                        PlayerFireRate *= 0.75d;
                        break;
                    case "Unreal":
                        GameObject.player.setPlayerDamageBase((int)Math.Floor(GameObject.player.getPlayerDamageBase() * 1.2));
                        PlayerFireRate *= 0.85d;
                        projectilespeed = 1.05d;
                        break;
                    case "Precise":
                        GameObject.player.setPlayerDamageBase((int)Math.Floor(GameObject.player.getPlayerDamageBase() * 1.5));
                        PlayerFireRate *= 1.5d;
                        projectilespeed = 1.2d;
                        break;
                    case "Masterful":
                        GameObject.player.setPlayerDamageBase((int)Math.Floor(GameObject.player.getPlayerDamageBase() * 1.2));
                        break;
                    case "Antique":
                        GameObject.player.setPlayerDamageBase((int)Math.Floor(GameObject.player.getPlayerDamageBase() * 0.9));
                        PlayerFireRate *= 1.15d;
                        projectilespeed = 1.25d;
                        break;
                    default:
                        break;
                }
                if (GameObject.player.getPlayerDamageBase() < 1)
                {
                    GameObject.player.setPlayerDamageBase(1);
                }
            }
            
            #endregion
            if (item.GetRelic()) {ApplyRelic(item);}
            
        }
        private void ApplyRelic(Item item){
{               
                if(GameObject.player.HasRelic()){
                    RemoveRelic(GameObject.player.GetRelic());
                }
                GameObject.player.AddRelic(item); 
                
                switch (item.GetItemName())
                {
                    //relics are powerful items with a significant downside
                    case "AxiomCore":
                        GameObject.player.setScavengerModifier(1);
                        moveModifier = 0.6d;
                        break;
                    case "ChronicleOfAshAndLight":
                        GameObject.player.setPlayerDamage(2);
                        PlayerFireRateBoost = 0;
                        break;
                    case "NullSigil":
                        GameObject.player.toggleInstadeath();
                        GameObject.player.setPlayerDamage(2);
                        break;
                    case "EonLens":
                        projectilespeedbase += 5d; 
                        moveModifier = 0.5d;
                        break;
                    case "SeveranceRelic":
                        PlayerFireRateBoost += 2;
                        GameObject.player.toggleInstadeath();
                        break;
                    case "VaultedStar":
                        GameObject.player.setPlayerDamage(2);
                        moveModifier = 0.5d;
                        break;
                    case "ParadoxKeystone":
                        PlayerFireRateBoost += 1;
                        moveModifier = 0.7d;
                        break;
                    case "PaleEngine":
                        PlayerFireRateBoost += 1;
                        GameObject.player.setPlayerDamage(-2);
                        if (GameObject.player.getPlayerDamage() < 0)
                        {
                            GameObject.player.setPlayerDamage(0-GameObject.player.getPlayerDamage());
                        }
                        break;
                    case "EchoReliquary":
                        doubleshot = true;
                        moveModifier = 0.5d;
                        break;
                    case "MeridianShard":
                        GameObject.player.toggleInstadeath();
                        hardmode = true;
                        break;

                    default:
                        break;
                }
                return;
                


            }   
        }
        public void RemoveRelic(Item item){
                switch (item.GetItemName())
                {
                    //relics are powerful items with a significant downside
                    case "AxiomCore":
                        GameObject.player.setScavengerModifier(-1);
                        moveModifier = 1d;
                        break;
                    case "ChronicleOfAshAndLight":
                        GameObject.player.setPlayerDamage(-2);
                        PlayerFireRateBoost = 1;
                        break;
                    case "NullSigil":
                        GameObject.player.toggleInstadeath();
                        GameObject.player.setPlayerDamage(-2);
                        break;
                    case "EonLens":
                        projectilespeedbase -= 5d; 
                        moveModifier = 1d;
                        break;
                    case "SeveranceRelic":
                        PlayerFireRateBoost -= 2;
                        GameObject.player.toggleInstadeath();
                        break;
                    case "VaultedStar":
                        GameObject.player.setPlayerDamage(-2);
                        moveModifier = 1d;
                        break;
                    case "ParadoxKeystone":
                        PlayerFireRateBoost -= 1;
                        moveModifier = 1d;
                        break;
                    case "PaleEngine":
                        PlayerFireRateBoost -= 1;
                        GameObject.player.setPlayerDamage(2);
                        break;
                    case "EchoReliquary":
                        doubleshot = false;
                        moveModifier = 1d;
                        break;
                    case "MeridianShard":
                        GameObject.player.toggleInstadeath();
                        hardmode = false;
                        break;

                    default:
                        break;
                }
        }
        private async Task GoShop(){

            Random r = new();
            gameTimer.Stop();
            Shop shop = new Shop(GameObject.floor);
            List<Button> ItemBuyButtons = [];
            foreach (Item item in shop.ItemsAvailable)
            {
                
                var button = new Button{
                    Content = item.GetItemName(),
                    HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                    Margin = new Thickness(20)
                };
                if (item.GetRelic())
                {
                    button.Content += " - Taking this item will replace any other relic you have!";
                }
                button.Click += (sender, e) => 
                { 
                    OnItemBought(item); 
                    (((Button)sender).GetVisualRoot() as Window)?.Close(); 
                };
                //lambda method to allow passing parameters to the event handler for button click
                ItemBuyButtons.Add(button);

                
            }
            var stackPanel = new StackPanel
            {
                Children =
                {
                    new TextBlock
                    {
                        Text = "Pick a weapon.",
                        HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                        Margin = new Thickness(20)
                    },
                }
            };
            foreach (Button button in ItemBuyButtons)
            {
                stackPanel.Children.Add(button);
            }
            var itemBuyBox = new Window()
            {
                Title = "Item Buy Menu",
                Width = 500,
                Height = 600,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Content = stackPanel

            };
            await itemBuyBox.ShowDialog(this);
            

        }
        private async void StartNextStage()
        {
            keysPressed.Clear();
            EnemiesUnstuckThisRound = false;
            Task task1 = PickUpgrade();
            GameObject.floor += 1; //floor is updated here as the shop function (GoShop) uses the floor to determine what items are available
            //this is why floor is increased here and then subtracted from in the if and else if statements
            if (!callShop && GameObject.floor - 1 != 1)
            {
                await task1;
                gameTimer.Start();

            }
            else if (callShop || GameObject.floor - 1 == 1)
            {
                Task task2 = GoShop();
                callShop = false;
                await Task.WhenAll(task1, task2);
                gameTimer.Start();

            }
            Dispatcher.UIThread.Invoke(() =>//used to force the map function to be run and assigned to MapImage in the UI Thread, as otherwise there are thread ownership issues 
            //since MapImage is a child of the canvas, it belongs to the UI thread, while map doesnt
            {
                GameObject.Level = new(800, 600, GameObject.Level.Seed + 10);
                map = setupMap();
                MapImage.Source = map;
            });
            MyCanvas.Children.Remove(MapImage);
            MyCanvas.Children.Add(MapImage);



            GameObject.player.SetAmmo(GameObject.player.getPlayerAmmoMax());
            stageTransitioning = true;
            currentStage = GameObject.floor;

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
            if (currentStage % 5 != 0)
            {
                // Spawn new enemies
                SpawnEnemies();
            }
            else
            {
                SpawnBoss();
                TextBlock bossMessage = new()
                {
                    Text = "Boss stage",
                    FontSize = 48,
                    Foreground = Brushes.White,
                    Background = Brushes.Black,
                    Padding = new Thickness(20),
                    HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                    VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center
                };
                MyCanvas.Children.Add(bossMessage);
                Canvas.SetLeft(bossMessage, (MyCanvas.Bounds.Width - 200) / 2);
                Canvas.SetTop(bossMessage, (MyCanvas.Bounds.Height - 100) / 2);
                // Wait for 2 seconds
                await Task.Delay(2000);

                MyCanvas.Children.Remove(bossMessage);
            }
            //SpawnObstacles();
            stageTransitioning = false;
        }
        private void SpawnObstacles(){
            foreach (Obstacle obstacle in obstacles)
            {
                MyCanvas.Children.Remove(obstacle.obstacle);
            }
            obstacles.Clear();
            for (int i = 0; i < 5; i++)
            {
                Obstacle newObstacle = new(new Rectangle
                {
                    Fill = Brushes.Cyan,
                    Height = 20,
                    Width = 20
                });
                obstacles.Add(newObstacle);
                MyCanvas.Children.Add(newObstacle.obstacle);
                Random r = new();
                Canvas.SetLeft(newObstacle.obstacle, r.Next(800));
                Canvas.SetTop(newObstacle.obstacle, r.Next(600));
            }
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
                Random r = new();
                Enemy newEnemy = new(new Rectangle 
                { 
                    Fill = new ImageBrush(GoblinTexture), 
                    Height = 50, 
                    Width = 50 
                }, enemystats);
                
                enemies.Add(newEnemy);
                MyCanvas.Children.Add(newEnemy.enemy);
                int x,y;
                do
                {
                    
                    x = r.Next(15,1920);
                    y = r.Next(15,1080);
                    

                }while (IsBlockedScreen(map, x,y,1920 + 25,1080 + 25));
                Canvas.SetTop(newEnemy.enemy, y);
                Canvas.SetLeft(newEnemy.enemy, x);
                
            }
        }
        private void SpawnBoss(){
            // Clear any remaining projectiles
            foreach (Rectangle projectile in playerProjectiles)
            {
                MyCanvas.Children.Remove(projectile);
            }
            playerProjectiles.Clear();

            Boss newBoss = new(new Rectangle{
                Fill = Brushes.Green, 
                Height = 45, 
                Width = 45 
            }, bossstats, 1);
            // Initialize boss hp based on current stage
            newBoss.InitializeForStage(currentStage);
            enemies.Add(newBoss);

            // Add boss to canvas and position it in centre
            MyCanvas.Children.Add(newBoss.enemy);
            int x, y;
            Random r = new();
            do
                {
                    
                    x = r.Next(15,1920);
                    y = r.Next(15,1080);
                    

                }while (IsBlockedScreen(map, x,y,1920 + 25,1080 + 25));
            Canvas.SetLeft(newBoss.enemy, x);
            Canvas.SetTop(newBoss.enemy, y);
        }
        
        
        private void EnemyMovement(Rectangle player, Enemy enemy)
        {
            // check if a collision is present
            if (IsTouching(player, enemy.enemy))
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

            // Prevent division by zero / NaN when overlapping
            if (directDistance <= double.Epsilon)
            {
                return; // Skip movement this tick
            }

            // Calculate movement
            double xToMove = enemyMove * (xDist / directDistance);
            double yToMove = enemyMove * (yDist / directDistance);

            double nextX = currentEnemyX + xToMove;
            double nextY = currentEnemyY + yToMove;
            foreach (Obstacle obstacle in obstacles)
            {
                if (IsTouching(enemy.enemy, obstacle.obstacle))
                {
                    nextX = currentEnemyX - 2 * xToMove;
                    nextY = currentEnemyY - 2 * yToMove;
                }
            }
            if (!IsBlockedScreen(map, (int)nextX + 3 /*3 pixel buffer*/, (int)nextY + 3, 1920, 1080) && !IsBlockedScreen(map, (int)(nextX + 3 + enemy.enemy.Width), (int)(nextY + 3 + enemy.enemy.Height), 1920, 1080))
            {
                Canvas.SetLeft(enemy.enemy, nextX);
                Canvas.SetTop(enemy.enemy, nextY);
            }
            else
            {
                Canvas.SetLeft(enemy.enemy, currentEnemyX);
                Canvas.SetTop(enemy.enemy, currentEnemyY);
            }
            
            
            if (IsTouching(player, enemy.enemy))
            {
                Canvas.SetLeft(enemy.enemy, currentEnemyX);
                Canvas.SetTop(enemy.enemy, currentEnemyY);
            }


            MyCanvas.Children.Remove(enemy.enemy);
            MyCanvas.Children.Add(enemy.enemy);

        }
        private bool IsBlocked(WriteableBitmap map, int x, int y)
        {
            // Out of bounds = blocked
            if (x < 0 || y < 0 || x >= map.PixelSize.Width || y >= map.PixelSize.Height)
                return true;
            using var fb = map.Lock();
            unsafe
            {
                byte* ptr = (byte*)fb.Address;
                int stride = fb.RowBytes;

                int index = y * stride + x * 4;

                byte b = ptr[index + 0];
                byte g = ptr[index + 1];
                byte r = ptr[index + 2];

                // #000000 - checks to see if movement is attempted into a black pixel
                return r == 0 && g == 0 && b == 0;
            }
        }
        private void TryMove(ref int x, ref int y, int dx, int dy, WriteableBitmap map)
        {
            int nx = x + dx;
            int ny = y + dy;

            if (!IsBlockedScreen(map, nx, ny, 1920, 1080))
            {
                x = nx;
                y = ny;
            }
        }     
        private bool IsTouching(Rectangle a, Rectangle b){
            bool c = false;
            double x1 = Canvas.GetLeft(a);
            double y1 = Canvas.GetTop(a);
            double x2 = Canvas.GetLeft(b);
            double y2 = Canvas.GetTop(b);
            Rect aRect = new Rect(x1, y1, a.Width, a.Height);
            Rect bRect = new Rect(x2, y2, b.Width, b.Height);
            if (aRect.Intersects(bRect))
            {
                c = true;
            }
            return c;
        }
        private void ShootProjectile(Player Sender)
        {
            //Check if player has ammo to shoot and shot cooldown has passed
            double playerFireRateWithBoost = PlayerFireRate / PlayerFireRateBoost;
            if (!((DateTime.Now - lastShotTime).TotalSeconds >= playerFireRateWithBoost))
            {
                return;
            }
            if (!(Sender.GetAmmo() > 0))
            {
                return;
            }
            
            Rectangle projectile = new() { Fill = Brushes.Crimson, Height = GameObject.player.GetProjSize(), Width = GameObject.player.GetProjSize() };
            MyCanvas.Children.Add(projectile);
    
            double startX = Canvas.GetLeft(Sender.PlayerRectangle) + Sender.PlayerRectangle.Width / 2;
            double startY = Canvas.GetTop(Sender.PlayerRectangle) + Sender.PlayerRectangle.Height / 2;
    
            // Calculate direction vector for projectile
            double dirX = mousePosition.X - startX;
            double dirY = mousePosition.Y - startY;
    
            // Normalise the direction vector with Pythagoras
            double length = Math.Sqrt(dirX * dirX + dirY * dirY);
            if (length <= double.Epsilon)
            {
                // If mouse is exactly at the player's center, skip firing this tick to avoid div 0
                return;
            }
            dirX /= length;
            dirY /= length;
    
            // Store direction with the projectile
            projectile.Tag = new Vector(dirX, dirY);
    
            Canvas.SetTop(projectile, startY + projectile.Height/2);
            Canvas.SetLeft(projectile, startX + projectile.Width/2);
            playerProjectiles.Add(projectile);
            if (doubleshot)
            {
              playerProjectiles.Add(projectile);
              Sender.SetAmmo(Sender.GetAmmo() - 2);
            }
            else
            {
                Sender.SetAmmo(Sender.GetAmmo() - 1);
            }
            
            lastShotTime = DateTime.Now;
        }        
        private void MoveProjectiles(Rectangle projectile)
        {
            #pragma warning disable CS8605 //Disables warning (it got annoying)
            Vector direction = (Vector)projectile.Tag;
            #pragma warning restore CS8605
            int projX, projY;
            projX = (int)Canvas.GetLeft(projectile);
            projY = (int)Canvas.GetTop(projectile);
            double speed = projectilespeedbase * projectilespeed;
            //stops projectiles moving into walls
            TryMove(ref projX, ref projY, (int)Math.Floor(direction.X * speed), (int)Math.Floor(direction.Y * speed), map);

            Canvas.SetLeft(projectile, projX);
            Canvas.SetTop(projectile, projY);
        }
    }
}

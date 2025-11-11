using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia; //avalonia is a FOSS cross-platform WPF port to allow for development at home
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using Avalonia.VisualTree;

namespace NEA
{
    public partial class MainWindow : Window
    {
        #region InitVariables
        private bool saveClicked = false;
        private bool callShop = false;
        private List<Rectangle> ammopickups = [];
        private readonly List<string> upgrades = ["Damage +1", "Damage +2", "Damage +3", "Heal", "Enemy Slow", "Projectile Size Up 25%", "Scavenger (10%)", "Fire Rate Up 10%", "Fire Rate Up 20%", "Fire Rate Up 30%"];
        private Dictionary<string, Buff> UpgradeEffects = [];
        private TextBox InputPath;
        public DateTime lastPlayerCollisionTime = DateTime.Now;
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

        private Bitmap playerSprite = new Bitmap("playerSprite.png");

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
            foreach (string upgrade in upgrades)
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
            ]; // 10 in all stats, 15hp, warrior, name = tempname
            
            
            // Setting up player sprite
            Rectangle PlayerRect = new()
            {
                Name = "PlayerRect",
                Fill = new ImageBrush(playerSprite),
                Height = 54,
                Stroke = Brushes.Black,
                Width = 54
            };
            
            GameObject = new Game(playerStatTestingList, PlayerRect, 1);
            
            for (int i = 0; i < 5; i++)
            {
                enemies.Add(new Enemy(new Rectangle { Fill = new ImageBrush(GoblinTexture), Height = 50, Width = 50 }, enemystats));
            }
            playerAmmo = new()
            {
                Name = "AmmoCounter",
                Height = 30,
                Width = 50,
                FontSize = 25,
                Content = $"Ammo: {GameObject.player.GetAmmo()}",
                Background = Brushes.Aqua
            };
            MyCanvas.Children.Add(playerAmmo);
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
                Interval = TimeSpan.FromMilliseconds(16.66) // ~60 FPS, actually just under since 1/60 is 0.01666...
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
                if (CheckCollisionOfTwoRects(ammo, player.PlayerRectangle))
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

            // Remove marked enemies and projectiles
            
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
            
            bool PlayerCollision = false;
            foreach (Obstacle obstacle in obstacles)
            {
                if (CheckObstacleCollision(player.PlayerRectangle, obstacle.obstacle))
                {
                    PlayerCollision = true;
                }
            }
            if (keysPressed.Contains(Key.Escape) && !pauseMenuOpen)
            {
                PauseMenu();
            }
            if (!PlayerCollision)
            {
                if (keysPressed.Contains(Key.W)) { y -= moveConstant; }
                if (keysPressed.Contains(Key.S)) { y += moveConstant; }
                if (keysPressed.Contains(Key.A)) { x -= moveConstant; }
                if (keysPressed.Contains(Key.D)) { x += moveConstant; }    
            }
            else if (PlayerCollision)
            {
                
                if (keysPressed.Contains(Key.W)) { y += stuckMove; }
                if (keysPressed.Contains(Key.S)) { y -= stuckMove; }
                if (keysPressed.Contains(Key.A)) { x += stuckMove; }
                if (keysPressed.Contains(Key.D)) { x -= stuckMove; }  
            }
            
            
            x = Math.Clamp(x, 0, 800-player.PlayerRectangle.Width);
            y = Math.Clamp(y, 0, 600-player.PlayerRectangle.Height);
            Canvas.SetTop(player.PlayerRectangle, y);
            Canvas.SetLeft(player.PlayerRectangle, x);

            foreach (Rectangle projectile in playerProjectiles)
            {
                MoveProjectiles(projectile);
            }
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
            keysPressed.Add(e.Key);
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
            if (path == string.Empty)
            {
                return;
            }
            else
            {
                try
                {
                    using (StreamWriter sw = new StreamWriter(path))
                    {
                        sw.WriteLine(GameObject.floor);
                    };
                    saveClicked = true;
                    return;
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
                    using (StreamReader sw = new StreamReader(path))
                    {
                        GameObject.floor = int.Parse(sw.ReadLine()) - 1;
                        StartNextStage();
                    };
                    return;
                }
                catch (System.ArgumentNullException)
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
                                Text = $"Player Weapon: {GameObject.player.GetWeapon().GetItemName()}\nPlayer Damage: {GameObject.player.getPlayerDamageBase()}, Multiplier {GameObject.player.getPlayerDamage()}x \nPlayer Fire Rate: {PlayerFireRate / PlayerFireRateBoost} second(s) per shot ",
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
                    Title = $"HP: {GameObject.player.PlayerStats.Hp}";
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
                case "Hand Crossbow":
                    PlayerFireRate = 0.75;
                    GameObject.player.setPlayerDamageBase(2);
                    GameObject.player.setPlayerAmmoMax(25);
                    break;
                case "Heavy Crossbow":
                    PlayerFireRate = 2.5;
                    GameObject.player.setPlayerDamageBase(4);
                    GameObject.player.setPlayerAmmoMax(15);
                    break;
                case "Toxicarp":
                    PlayerFireRate = 0.25;
                    GameObject.player.setPlayerDamageBase(1);
                    GameObject.player.setPlayerAmmoMax(100);
                    break;
                case "Light Crossbow":
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
                case "Scoped Rifle":
                    PlayerFireRate = 5;
                    GameObject.player.setPlayerDamageBase(10);
                    GameObject.player.setPlayerAmmoMax(15);
                    break;
                case "Pistol":
                    PlayerFireRate = 0.75;
                    GameObject.player.setPlayerDamageBase(2);
                    GameObject.player.setPlayerAmmoMax(12);
                    break;
                case "Molten Fury":
                    PlayerFireRate = 0.5;
                    GameObject.player.setPlayerDamageBase(2);
                    GameObject.player.setPlayerAmmoMax(20);
                    break;
                case "Aerial Bane":
                    PlayerFireRate = 0;
                    GameObject.player.setPlayerDamageBase(1);
                    GameObject.player.setPlayerAmmoMax(40);
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
                button.Click += (sender, e) => 
                { 
                    OnItemBought(item); 
                    (((Button)sender).GetVisualRoot() as Window)?.Close(); 
                };
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
                Width = 450,
                Height = 300,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Content = stackPanel

            };
            await itemBuyBox.ShowDialog(this);
            

        }
        private async void StartNextStage()
        {
            Task task1 = PickUpgrade();
            GameObject.floor += 1;
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
                    Text = $"Boss stage",
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
            SpawnObstacles();
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
                Enemy newEnemy = new(new Rectangle 
                { 
                    Fill = new ImageBrush(GoblinTexture), 
                    Height = 50, 
                    Width = 50 
                }, enemystats);
                
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
        private void SpawnBoss(){
            // Clear any remaining projectiles
            foreach (var projectile in playerProjectiles.ToList())
            {
                MyCanvas.Children.Remove(projectile);
            }
            playerProjectiles.Clear();

            Boss newBoss = new(new Rectangle{
                Fill = Brushes.Green, 
                Height = 45, 
                Width = 45 
            }, bossstats, 1);
            // Initialize boss hits based on current stage
            newBoss.InitializeForStage(currentStage);
            enemies.Add(newBoss);

            // Add boss to canvas and position it in centre
            MyCanvas.Children.Add(newBoss.enemy);
            double centerX = MyCanvas.Bounds.Width / 2 - newBoss.enemy.Width / 2;
            double centerY = MyCanvas.Bounds.Height / 2 - newBoss.enemy.Height / 2;
            Canvas.SetLeft(newBoss.enemy, centerX);
            Canvas.SetTop(newBoss.enemy, centerY);
        }
        private Rect RectConverter(Rectangle rectangle) // currently unused after transition to Avalonia as the avalonia Rectangle class works slightly differently to WPF
        // takes a rectangle and outputs the position and size as a Rect to be used in CheckCollisionOfTwoRects in
        // IntersectsWith method to allow to check collisions of player w/ enemy
        {
            double x = Canvas.GetLeft(rectangle);
            double y = Canvas.GetTop(rectangle);
            double width = rectangle.Width;
            double height = rectangle.Height;
            
            return new Rect(x, y, width, height);
        }
        private static bool CheckObstacleCollision(Rectangle mover, Rectangle Obstacle)
        {
            return CheckCollisionOfTwoRects(mover, Obstacle);
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
                if (CheckObstacleCollision(enemy.enemy, obstacle.obstacle))
                {
                    nextX = currentEnemyX - 2 * xToMove;
                    nextY = currentEnemyY - 2 * yToMove;
                }
            }
            
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
            //double AngleToRotate = Math.Atan2(yDist, xDist) * (180 / Math.PI);
            //enemy.enemy.RenderTransform = new RotateTransform(AngleToRotate);

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

            // Add small buffer (3 pixels) to make collisions cleaner
            const double buffer = 3.0;

            // Check for intersection with buffer
            return !(x1 + rect1.Width + buffer < x2 || x2 + rect2.Width + buffer < x1 || y1 + rect1.Height + buffer < y2 || y2 + rect2.Height + buffer < y1);
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
            Rectangle projectile = new() { Fill = Brushes.Black, Height = GameObject.player.GetProjSize(), Width = GameObject.player.GetProjSize() };
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
    
            Canvas.SetTop(projectile, startY);
            Canvas.SetLeft(projectile, startX);
            playerProjectiles.Add(projectile);
            Sender.SetAmmo(Sender.GetAmmo() - 1);
            lastShotTime = DateTime.Now;
        }        
        private static void MoveProjectiles(Rectangle projectile)
        {
            #pragma warning disable CS8605 //Disables warning (it got annoying)
            Vector direction = (Vector)projectile.Tag;
            #pragma warning restore CS8605

            double speed = moveConstant * 0.5;
    
            Canvas.SetLeft(projectile, Canvas.GetLeft(projectile) + direction.X * speed);
            Canvas.SetTop(projectile, Canvas.GetTop(projectile) + direction.Y * speed);
        }
    }
}
using Avalonia.Controls;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia; //avalonia is a FOSS cross-platform WPF port to allow for development on Linux
using Avalonia.Controls.Shapes;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Avalonia.Interactivity;
using Tmds.DBus.Protocol;
using System.Dynamic;



namespace NEA{
public partial class MainMenu : Window
{
    Button StartButton = new Button(); 
    Button SettingsButton = new Button();
    Button CloseButton = new Button();
    Label IntroText = new Label();
    Slider ScaleSlider = new Slider();
    Label SliderVal = new Label();
    private double _sliderValue;
    public double SliderValue{
        get => _sliderValue;
        set
        {
            _sliderValue = value;
        }
    }
    TextBox seedIn = new TextBox();
    Button SeedSet = new Button();
    int seedParam = 0;
    Bitmap settingbg = new Bitmap("settingsBackground.png");
    Image menubg = new Image();
    bool SliderValueChanged = false;
    bool settingsMenuExists = false;
    Window settingsMenu = null;
    
    public MainMenu()
    {
        InitializeComponent();
        SliderVal.Content = "Scale: " + SliderValue;
        SliderVal.HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center;
        SliderVal.Margin = new Thickness(20);
                                
        IntroText.Content = "Welcome to \nEnter the Dungeon";
        IntroText.HorizontalContentAlignment = Avalonia.Layout.HorizontalAlignment.Center;
        IntroText.VerticalContentAlignment = Avalonia.Layout.VerticalAlignment.Center;
        IntroText.Width = 150;
        IntroText.Background = Brushes.Wheat;

        seedIn.Watermark = "Enter seed...";

        StartButton.Content = "   Enter \n\tThe \n Dungeon";
        StartButton.Background = Brushes.Wheat;
        StartButton.HorizontalContentAlignment = Avalonia.Layout.HorizontalAlignment.Center;
        StartButton.VerticalContentAlignment = Avalonia.Layout.VerticalAlignment.Center;
        StartButton.Click += Start_Click;
        StartButton.Width = 120;
        StartButton.Height = 80;
        SettingsButton.Content = "Settings";
        SettingsButton.Background = Brushes.Wheat;
        SettingsButton.Click += Settings_Click;
        CloseButton.Content = "Quit";
        CloseButton.Background = Brushes.Wheat;
        CloseButton.Click += Exit_Click;
        
        menubg.Stretch = Stretch.None;
        menubg.Height = 420;
        menubg.Width = 690;
        menubg.ZIndex = -1;

        Dispatcher.UIThread.Invoke(() => //used to force the map function to be run and assigned to menubg in the UI Thread, as otherwise there are thread ownership issues 
        //since menubg is a child of the canvas, it belongs to the UI thread, while map doesnt
        {
          menubg.Source = new Bitmap("bg.png");
        });

        MenuCanvas.Children.Add(menubg);
        MenuCanvas.Children.Add(StartButton);
        MenuCanvas.Children.Add(SettingsButton);
        MenuCanvas.Children.Add(CloseButton);
        MenuCanvas.Children.Add(IntroText);

        Canvas.SetLeft(IntroText, 270);
        Canvas.SetLeft(StartButton, 280);
        Canvas.SetTop(StartButton, 180);
        Canvas.SetRight(CloseButton, 0);

        ScaleSlider.ValueChanged += SliderChanged;
        SeedSet.Click += seedChanged;
        SeedSet.Content = "Set Seed";
        SeedSet.HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center;
        SeedSet.Margin = new Thickness(20);
    }
    private void SliderChanged(object? sender, RoutedEventArgs e){
        SliderValueChanged = true;
        SliderValue = ScaleSlider.Value;
        SliderVal.Content = "Scale: " + Math.Round(SliderValue, 2);
    }
    private void seedChanged(object? sender, RoutedEventArgs e){
        try
        {
            seedParam = int.Parse(seedIn.Text);
        }
        catch (System.FormatException)
        {
            seedIn.Watermark = "Not a valid seed - seeds must be 0-65 535, consisting of only numbers";
            seedIn.Text = "";
            
        }

        
    }

    private void Start_Click(object? sender, RoutedEventArgs e)
    {
        if (!SliderValueChanged)
        {
            _sliderValue = 0.06f;
        }
        var gameWindow = new NEA.MainWindow(_sliderValue, seedParam);
        gameWindow.Show();
        this.Close();
    }

    private async void Settings_Click(object? sender, RoutedEventArgs e)
    {
       

            ScaleSlider.Value = 0.06f;
            ScaleSlider.Maximum = 0.25f;
            ScaleSlider.Minimum = 0f;
            ScaleSlider.Width = 100;
            Button closeMenu = new Button
            {
                Content = "Close Menu",
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                Margin = new Thickness(20),

            };
            closeMenu.Click += Exit_Menu;
            
            if (!settingsMenuExists)
            {
            settingsMenu = new Window()
                {
                    
                    Title = "Settings",
                    Width = 800,
                    Height = 600,
                    WindowStartupLocation = WindowStartupLocation.CenterOwner,
                    Background = new ImageBrush(settingbg),
                    Content = new StackPanel
                    {
                        Children =
                        {
                            
                            new Label
                            {
                                Content = "Settings",
                                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                                Margin = new Thickness(20)
                            },
                            new Label
                            {
                                Content = "Scale values above 0.1 cause issues, alter with caution",
                                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                                Margin = new Thickness(20)
                            },
                            SliderVal,
                            ScaleSlider,
                            new Label
                            {
                                Content = "Enter seed",
                                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                                Margin = new Thickness(20)
                            },
                            seedIn,
                            SeedSet,
                            closeMenu
                            
                        }
                    }
                };
                settingsMenuExists = true;
                await settingsMenu.ShowDialog(this);
                }
                else
                {
                    settingsMenu.Show();
                }
                
    }

    private void Exit_Click(object? sender, RoutedEventArgs e)
    {
        Close();
    }
    private void Exit_Menu(object? sender, RoutedEventArgs e)
    {
        (((Button)sender).GetVisualRoot() as Window)?.Hide();
    }
                            
}
}
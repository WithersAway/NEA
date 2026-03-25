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

namespace NEA{
public partial class MainMenu : Window
{
    Button StartButton = new Button(); 
    Button SettingsButton = new Button();
    Button CloseButton = new Button();
    TextBlock IntroText = new TextBlock();
    Slider ScaleSlider = new Slider();
    Label SliderVal = new Label();
    private double _sliderValue;
    public double SliderValue
{
    get => _sliderValue;
    set
    {
        _sliderValue = value;
    }
}
    public MainMenu()
    {
        InitializeComponent();
        SliderVal.Content = "Scale: " + SliderValue;
        SliderVal.HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center;
        SliderVal.Margin = new Thickness(20);
                                
        StartButton.Content = "Start";
        StartButton.Click += Start_Click;
        SettingsButton.Content = "Settings";
        SettingsButton.Click += Settings_Click;
        CloseButton.Content = "Quit";
        CloseButton.Click += Exit_Click;


        MenuCanvas.Children.Add(StartButton);
        MenuCanvas.Children.Add(SettingsButton);
        MenuCanvas.Children.Add(CloseButton);

        Canvas.SetLeft(StartButton, 345);
        Canvas.SetTop(StartButton, 210);
        Canvas.SetRight(CloseButton, 0);

        ScaleSlider.ValueChanged += SliderChanged;
    }
    private void SliderChanged(object? sender, RoutedEventArgs e){
        SliderValue = ScaleSlider.Value;
        SliderVal.Content = "Scale: " + Math.Round(SliderValue, 2);
    }

    private void Start_Click(object? sender, RoutedEventArgs e)
    {
        var gameWindow = new NEA.MainWindow(ScaleSlider.Value);
        gameWindow.Show();
        this.Close();
    }

    private async void Settings_Click(object? sender, RoutedEventArgs e)
    {
       

            ScaleSlider.Value = 0.06f;
            ScaleSlider.Maximum = 1;
            ScaleSlider.Minimum = 0;
            ScaleSlider.Width = 100;

            
            var settingsMenu = new Window()
                {
                    
                    Title = "Settings",
                    Width = 450,
                    Height = 300,
                    WindowStartupLocation = WindowStartupLocation.CenterOwner,
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
                            SliderVal,
                            ScaleSlider,
                        }
                    }
                };
                await settingsMenu.ShowDialog(this);
    }

    private void Exit_Click(object? sender, RoutedEventArgs e)
    {
        Close();
    }
                            
}
}
using System;
using System.Collections.Generic;
using Avalonia.Controls.Shapes;

namespace NEA;

public class Player(List<string> InitArgs, Rectangle rectIN)
{
    public Rectangle PlayerRectangle { get; set; } = rectIN;
    public string Name { get; set; } = InitArgs[0];
    internal Item[] Items { get; set; }
    public Stats PlayerStats { get; set; } = new Stats(Convert.ToInt32(InitArgs[1]), Convert.ToInt32(InitArgs[2]), Convert.ToInt32(InitArgs[3]), Convert.ToInt32(InitArgs[4]), Convert.ToInt32(InitArgs[5]), Convert.ToInt32(InitArgs[6]), Convert.ToInt32(InitArgs[7]), Convert.ToInt32(InitArgs[8]));
    internal Specialise Specialise { get; } = (Specialise)Convert.ToInt32(InitArgs[9]); //specialise is for class
    protected int invMax;

    //add playerdead check
    public bool IsPlayerDead()
        {
            bool PlayerAlive = !this.PlayerStats.AnyStatsZero();
            return PlayerAlive;
        }
    }
    enum Specialise
    {
        Warrior = 0,
        Archer = 1,
        Mage = 2,
        Thief = 3
    }
    enum InvMaxes
    {
        Warrior = 5,
        Archer = 5,
        Mage = 5,
        Thief = 8
    }

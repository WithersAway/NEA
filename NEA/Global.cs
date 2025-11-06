using System.Collections.Generic;
using Avalonia.Controls.Shapes;

namespace NEA;

public class Global{}
public class Game
{
    
    public int floor;
    public Player player;
    public Difficulty mode { get; set; }
    List<Enemy> enemies = [];
    public Game(List<string> args, Rectangle rect, int difficulty)
    {
        floor = 1;
        player = new Player(args, rect);
        mode = (Difficulty)difficulty;

        
    }

    public enum DamageTypes
    {
        Acid,
        Blunt,
        Cold,
        Electric,
        Fire,
        Magic,
        Necrotic,
        Piercing,
        Poison,
        Psychic,
        Radiant,
        Slashing,
        Thunder
    }
    public enum Difficulty {
        Sandbox,
        Easy,
        Medium, 
        Hard,
    }
}
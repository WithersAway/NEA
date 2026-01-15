using System.Collections.Generic;
using Avalonia.Controls.Shapes;

namespace NEA;

public class Global{}
public class Game
{
    public int floor;
    public Player player;
    public Difficulty mode { get; set; }
    public NoiseGenerator Level;
    List<Enemy> enemies = [];
    public Game(List<string> args, Rectangle rect, int difficulty, int hNG, int wNG, int? seed = null)
    {
        floor = 1;
        Level = new(wNG, hNG, seed);
        player = new Player(args, rect);
        mode = (Difficulty)difficulty;
        Level = new(800,600,null);
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

public enum TileType{
    Wall,
    Floor
}

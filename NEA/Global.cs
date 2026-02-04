using System.Collections.Generic;
using Avalonia.Controls.Shapes;
using System.Runtime.InteropServices;

namespace NEA;

public static class Global{
    [DllImport("libc")] internal static extern int getuid();}
public class Game
{
    public int floor;
    public Player player;
    public Difficulty mode { get; set; }
    public NoiseGenerator Level;
    public Game(List<string> args, Rectangle rect, int difficulty, int wNG, int hNG, int? seed = null)
    {
        floor = 1;
        Level = new(wNG, hNG, seed);
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

public enum TileType{
    Wall,
    Floor
}

using System;
using System.Collections.Generic;
using System.Reflection.Metadata.Ecma335;
using System.Runtime.Intrinsics.X86;
using Avalonia.Controls.Shapes;
using Avalonia.Metadata;

namespace NEA;

public class Global{}
public class Game
{
    
    public int floor;
    public Player player;
    public Difficulty mode { get; set; }
    public NoiseGenerator Level;
    List<Enemy> enemies = [];
    public Game(List<string> args, Rectangle rect, int difficulty)
    {
        floor = 1;
        player = new Player(args, rect);
        mode = (Difficulty)difficulty;
        Level = new(800,600,1);
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

public class NoiseGenerator
{
    public int Width {get;}    
    public int Height {get;}
    public TileType[,] map {get; private set;}

    private readonly Random r;
    private readonly int seed;


    public NoiseGenerator(int width, int height, int? customseed = null)
    {
        Width = width;
        Height = height;
        //if user provides a custom seed, then use it, otherwise hash a GUID to a 32 bit integer for a unique seed
        if (customseed.HasValue)
        {
            seed = customseed.Value;
        }
        else
        {
            seed = Guid.NewGuid().GetHashCode();
        }
        r = new Random(seed);
        map = new TileType[Width, Height];
    }

    public void Generate(){
        //Should have: generate noise, smooth map, ensure connectivity
        GenerateNoise();
    }

    //generate using perlin noise

    private void GenerateNoise()
    {
        const float scale = 0.12f;
        for (int i = 0; i < Width; i++)
        {
            for (int j = 0; j < Height; j++)
            {
                float perlin = Perlin(i * scale + seed, j * scale + seed);
                if (perlin > 0.45)
                {
                    map[i,j] = TileType.Floor;
                }
                else
                {
                    map[i,j] = TileType.Wall;
                }
            }
        }
    }


    //smooth with cellular automata
    #region cellularAutomata
    #endregion


    //connectivity


    #region Perlin
    //perlin

    private float Perlin(float x, float y) //takes in point (x, y)
    {
        //ix -> intx, fx -> floatx
        //abcd are corners
        //u and v are weights
        int ix = (int)Math.Floor(x);
        int iy = (int)Math.Floor(y);

        float fx = x - ix; //separate floating point into integer part and floating part
        float fy = y - iy;

        //this gets the 4 grid corners surrounding (x, y)

        //get dot product for each corner
        float a = DotProductNoise(ix, iy, fx, fy);
        float b = DotProductNoise(ix + 1, iy, fx - 1, fy);
        float c = DotProductNoise(ix, iy + 1, fx, fy - 1);
        float d = DotProductNoise(ix + 1, iy + 1, fx - 1, fy - 1);

        //calculate interpolation weights
        float u = fadeT(fx);
        float v = fadeT(fy);

        return Lerp(Lerp(a, b, u), Lerp(c, d, u), v); //blend using lerp for a noise value (left to right (a -> b, c -> d) with u, top to botton with v)
    }


    //lerp

    private float Lerp (float a, float b, float t) => a + t * (b - a); //linear interpolation

    //fade

    private float fadeT (float t) => t * t * t * (t * (t * 6 - 15) + 10); 
    //smoothes curve to fit a fifth degree polynomial, 
    //taken directly from Ken Perlin (creator of Perlin noise)
    //expands to: 6t^5 − 15t^4 + 10t^3


    //dotnoise

    private float DotProductNoise (int ix, int iy, float x, float y)
    {
        //use large primes together with bitwise XOR for a pseudorandom seed hash
        int hash = (ix * 73856093) ^ (iy * 19349663) ^ seed; //xor gives different seeds every time, but the same world for the same seed every time
        hash &= 7; //bitwise and limits number 0-7 for one of 8 gradients
        float[][] gradMap = 
        { 
            new[]{1f,1f}, new[]{-1f,1f}, new[]{1f,-1f}, new[]{-1f,-1f},
            new[]{1f,0f}, new[]{-1f,0f}, new[]{0f, 1f}, new[]{ 0f,-1f}
        }; //make a vector map of 8 unique directional gradients (cardinal directions + diags)
        float[] g = gradMap[hash];
        return g[0] * x + g[1] * y; //dot product formula
    }
    #endregion
}

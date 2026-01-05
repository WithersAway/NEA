using System;
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
    public Game(List<string> args, Rectangle rect, int difficulty)
    {
        floor = 1;
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

public class NoiseGenerator
{
    public int Width {get;}
    public int Height {get;}
    public TileType[,] map {get; private set;}

    private readonly int seed;


    public NoiseGenerator(int width, int height, int? customseed = null)
    {
        Width = width;
        Height = height;
        //if user provides a custom seed, then use it, otherwise hash a GUID to a 32 bit integer for a semi-unique seed
        if (customseed.HasValue)
        {
            seed = customseed.Value;
        }
        else
        {
            seed = Guid.NewGuid().GetHashCode(); //GUIDs are 128 bit but random seeds are 32 bit so hash the GUID for a small enough number
        }
        map = new TileType[Width, Height];
    }

    public void Generate(){
        //Should have: generate noise, smooth map, ensure connectivity
        GenerateNoise();
        SmoothMap(4);
        EnsureConnectivity();
    }

    //generate using perlin noise

    private void GenerateNoise()
    {
        const float scale = 0.15f;
        for (int i = 0; i < Width; i++)
        {
            for (int j = 0; j < Height; j++)
            {
                float perlin = Perlin(i * scale, j * scale);
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
    private void SmoothMap(int iterations)
    {
        for (int i = 0; i < iterations; i++)
        {
            TileType[,] newMap = (TileType[,])map.Clone();

            for (int x = 0; x < Width; x++)
            for (int y = 0; y < Height; y++)
            {
                int walls = CountWallsAround(x, y);

                if (walls > 4) newMap[x, y] = TileType.Wall;
                else if (walls < 4) newMap[x, y] = TileType.Floor;
            }

            map = newMap;
        }
    }

    private int CountWallsAround(int x, int y)
    {
        int count = 0;

        for (int nx = x - 1; nx <= x + 1; nx++)
        for (int ny = y - 1; ny <= y + 1; ny++)
        {
            if (nx < 0 || ny < 0 || nx >= Width || ny >= Height)
            {
                count++;
            }
            else if (map[nx, ny] == TileType.Wall)
            {
                count++;
            }
        }

        return count;
    }
    #endregion


    //connectivity
    private void EnsureConnectivity()
    {
        // Find a starting floor tile
        (int sx, int sy) = FindFirstFloor();

        bool[,] visited = new bool[Width, Height];
        FloodFill(sx, sy, visited);

        // Convert unreachable floors into walls to avoid enemies spawning in pockets inaccessible to the player, causign a softlock
        for (int x = 0; x < Width; x++)
            for (int y = 0; y < Height; y++)
            {
                if (map[x, y] == TileType.Floor && !visited[x, y])
                    map[x, y] = TileType.Wall;
            }
    }

    private (int,int) FindFirstFloor() //simple function to find first floor tile
    {
        for (int x = 0; x < Width; x++)
            for (int y = 0; y < Height; y++)
                if (map[x, y] == TileType.Floor)
                    return (x, y);

        return (Width / 2, Height / 2);
    }
    //recursive subroutine to set whether a tile is reachable or not
    private void FloodFill(int x, int y, bool[,] visited)
    {
        if (x < 0 || y < 0 || x >= Width || y >= Height) return;
        if (visited[x, y]) return;
        if (map[x, y] == TileType.Wall) return;

        visited[x, y] = true;

        FloodFill(x + 1, y, visited);
        FloodFill(x - 1, y, visited);
        FloodFill(x, y + 1, visited);
        FloodFill(x, y - 1, visited);
    }


    #region Perlin
    //perlin

    private float Perlin(float x, float y) //takes in point (x, y)
    {
        //ix -> intx, fx -> floatx
        //abcd are corners
        //u and v are weights
        int ix = (int)Math.Floor(x);
        int iy = (int)Math.Floor(y);

        float fx = x - ix; //separate fractional position in cell from grid co-ordinate, fx ∈ [0,1)
        float fy = y - iy; //separate fractional position in cell from grid co-ordinate, fy ∈ [0,1)

        //this gets the 4 grid corners surrounding (x, y)

        //get dot product for each corner
        float a = DotProductNoise(ix, iy, fx, fy);
        float b = DotProductNoise(ix + 1, iy, fx - 1, fy);
        float c = DotProductNoise(ix, iy + 1, fx, fy - 1); //vector. Comitting crimes with both direction and MAGNITUDE!!! OH YEAH!
        float d = DotProductNoise(ix + 1, iy + 1, fx - 1, fy - 1);

        //calculate interpolation weights
        float u = fadeT(fx); // fx ∈ [0,1), otherwise fadeT returns a massive value
        float v = fadeT(fy); // fy ∈ [0,1), otherwise fadeT returns a massive value

        return Lerp(Lerp(a, b, u), Lerp(c, d, u), v); //blend using lerp for a noise value (left to right (a -> b, c -> d) with u, top to botton with v)
        //three Lerps for bilinear interpolation, i.e. vertically and horizontally
        //one Lerp interpolates along one line, two Lerps interpolates horizontally but never blends vertically.
        //Three lerps is the minimum for smooth 2d interpolation
    }


    //lerp

    private float Lerp(float a, float b, float t) => a + t * (b - a); //linear interpolation

    //fade (interpolation weights)

    private float fadeT (float t) => t * t * t * (t * (t * 6 - 15) + 10);
    //smoothes curve to fit a fifth degree polynomial,
    //taken directly from Ken Perlin (creator of Perlin noise)
    //expands to: 6t^5 − 15t^4 + 10t^3


    //dot product for noise values

    private float DotProductNoise (int ix, int iy, float x, float y)
    {
        //use large primes together with bitwise xor for a pseudorandom seed hash
        int hash = (ix * 73856093) ^ (iy * 19349663) ^ seed; //bitwise xor gives different seeds every time, but the same world for the same seed every time
        //using xor and large primes for a simple pseudorandom hashs seed
        hash &= 7; //the bitwise and limits number 0-7 for one of 8 gradients for gradMap
        //using 8 gradients rather than Perlin's usual 12 as this simplifies maths and eliminates the need for matrix transforms
        float[][] gradMap =
        {
            new[]{1f,1f}, new[]{-1f,1f}, new[]{1f,-1f}, new[]{-1f,-1f},
            new[]{1f,0f}, new[]{-1f,0f}, new[]{0f, 1f}, new[]{ 0f,-1f}
        }; //make a vector map of 8 unique directional gradients (cardinal directions + diags)
        float[] g = gradMap[hash];
        return g[0] * x + g[1] * y; //dot product formula as g is a 2x1 matrix and x and y are position vectors, therefore x,y is a positional matrix 2x2
        //this must be converted to a scalar to have a singular float output
    }
    #endregion
}

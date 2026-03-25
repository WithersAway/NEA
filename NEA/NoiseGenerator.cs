using System;
using System.Diagnostics;
using System.Runtime.InteropServices;


namespace NEA;

public class NoiseGenerator
{
    
    public int Width {get;}
    public int Height {get;}
    public int Seed {get;}
    public TileType[,] map {get; private set;}
    public double Scale{get; set;}



    private readonly int seed;

    //a simplified version of perlin's noise algorithm, using fewer gradient values, a seed workaround and simplified versions of the smoothing and connectivity functions
    public NoiseGenerator(int width, int height, int? customseed = null, double scale = 0.06f)
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
            seed = Guid.NewGuid().GetHashCode(); //GUIDs are 128 bit but System.Random seeds are 32 bit so hash the GUID for a small enough number
        }
        Seed = seed;
        Scale = scale;
        map = new TileType[Width, Height];
        Generate();
        
    }

   

    public void Generate(){
        //Should have: generate noise, smooth map, ensure connectivity
        GenerateNoise();
        SmoothMap(4);
        //EnsureConnectivity(); currently out of use as flood fill doesnt work properly and stack overflows
    }

    //generate using perlin noise

    private void GenerateNoise()
    {
        double scale = Scale;
        const int blocksize = 8;
        
        for (int i = 0; i < Width; i++)
        {
            for (int j = 0; j < Height; j++)
            {
                double perlin = Perlin(i/blocksize * scale, j/blocksize * scale); 
                //splitting into 8x8 pixel chunks allows for larger blocks of terrain,
                //making a nicer environment for the player to move about in. 
                //this also helps with randomness
                
                if (((perlin+1)/2) > 0.35)//standardisation
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

        
        for (int x = 0; x < Width; x++)
        {
            for (int y = 0; y < Height; y++)
            {
                if (map[x, y] == TileType.Floor && !visited[x, y]) map[x, y] = TileType.Wall;
            }
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

    private double Perlin(double x, double y) //takes in point (x, y)
    {
        //ix -> integer part of x, fx -> fractional part of x
        //abcd are corners
        //u and v are weights
        int ix = (int)Math.Floor(x);
        int iy = (int)Math.Floor(y);

        double fx = x - ix; //separate fractional position in cell from grid co-ordinate, fx ∈ [0,1)
        double fy = y - iy; //separate fractional position in cell from grid co-ordinate, fy ∈ [0,1)

        //this gets the 4 grid corners surrounding (x, y)

        //get dot product for each corner
        double a = DotProductNoise(ix, iy, fx, fy);
        double b = DotProductNoise(ix + 1, iy, fx - 1, fy);
        double c = DotProductNoise(ix, iy + 1, fx, fy - 1); //vector. Comitting crimes with both direction and MAGNITUDE!!! OH YEAH!
        double d = DotProductNoise(ix + 1, iy + 1, fx - 1, fy - 1);

        //calculate interpolation weights
        double u = fadeT(fx); // fx ∈ [0,1), otherwise fadeT returns a massive value
        double v = fadeT(fy); // fy ∈ [0,1), otherwise fadeT returns a massive value

        return Lerp(Lerp(a, b, u), Lerp(c, d, u), v) * Math.Sqrt(2); //blend using lerp for a noise value (left to right (a -> b, c -> d) with u, top to botton with v)
        //three Lerps for bilinear interpolation, i.e. vertically and horizontally
        //one Lerp interpolates along one line, two Lerps interpolates horizontally but never blends vertically.
        //Three lerps is the minimum for smooth 2d interpolation
    }


    //lerp

    private double Lerp(double a, double b, double t) => a + t * (b - a); //linear interpolation using two points, finding midpoint and using the interpolation weights returned by fadeT

    //fade (interpolation weights)

    private double fadeT (double t) => t * t * t * (t * (t * 6 - 15) + 10);
    //smoothes curve to fit a fifth degree polynomial,
    //taken directly from Ken Perlin (creator of Perlin noise)
    //expands to: 6t^5 − 15t^4 + 10t^3
    //values less than one don't grow to insane sizes with exponentiation, so this curve is a good fit


    //dot product for noise values

    private double DotProductNoise (int ix, int iy, double x, double y)
    {
        
        //for direction of noise travel

        //use large primes together with bitwise xor for a pseudorandom seed hash
        int hash = (ix * 73856093) ^ (iy * 19349663) ^ seed; //bitwise xor gives different seeds every time, but the same world for the same seed every time
        //using xor and large primes for a simple pseudorandom hash seed
        hash &= 0b111; //the bitwise and limits number 0-7 for one of 8 gradients for gradMap
        //using 8 gradients rather than Perlin's usual 12 as this simplifies maths and eliminates the need for matrix transforms
        
        double norm = 1.0 / Math.Sqrt(2); //short for normaliser as otherwise diagonals are weighted differently in the Perlin subroutine
        //allows for smoother terrain generation due to using blocks
        double[][] gradMap =
        {
            new[]{ norm,  norm}, new[]{-norm,  norm},
            new[]{ norm, -norm}, new[]{-norm, -norm},
            new[]{ 1d, 0d}, new[]{-1d, 0d},
            new[]{ 0d, 1d}, new[]{ 0d,-1d}
        };
        double[] g = gradMap[hash];
        return g[0] * x + g[1] * y; //dot product formula as g is a 2x1 matrix and x and y are position vectors, therefore x,y is a positional matrix 2x2
        //this must be converted to a scalar to have a singular double output
    }
    #endregion
}

using System;

namespace NEA;

public class Stats
    {
        //int is a data type and therefore Intelligence => Smart
        //Setting up stats for use in combat
        public int StrMod { get; set; }
        public int DexMod { get; set; }
        public int ConMod { get; set; }
        public int SmartMod { get; set; }
        public int WisMod { get; set; }
        public int ChaMod { get; set; }
        public int SanityMod { get; set; }
        public int Str { get; set; }
        public int Dex { get; set; }
        public int Con { get; set; }
        public int Smart { get; set; }
        public int Wis { get; set; }
        public int Cha { get; set; }
        public int Sanity { get; set; }
        public int Hp { get; set; }
        //sub called to check if player should die
        public bool AnyStatsZero()
        {
            bool StatsNotZero = false;
            if (Hp > 0 && Str > 0 && Dex > 0 && Con > 0 && Smart > 0 && Wis > 0 && Cha > 0 && Sanity > 0)
            {
                StatsNotZero = true;
            }
            return StatsNotZero;
        }
    private static int GetMod(int stat) => (int)Math.Ceiling(stat / 2d) - 5; //calculates modifier for a stat using the D&D 5e stat modifier formula

    public Stats(int str, int dex, int con, int smart, int wis, int cha, int sanity, int hp) //constructor function to set stats and modifiers
        {
            Str = str;
            StrMod = GetMod(Str);
            Dex = dex;
            DexMod = GetMod(Dex);
            Con = con;
            ConMod = GetMod(Con);
            Smart = smart;
            SmartMod = GetMod(Smart);
            Wis = wis;
            WisMod = GetMod(Wis);
            Cha = cha;
            ChaMod = GetMod(Cha);
            Sanity = sanity;
            SanityMod = GetMod(Sanity);
            Hp = hp;
        }
    }
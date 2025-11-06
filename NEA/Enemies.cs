using System;
using System.Collections.Generic;
using Avalonia.Controls.Shapes;

namespace NEA;

public class Enemy
{
    
    internal Stats EnemyStats { get; set; }
    internal Item[] Loot { get; set; } = new Item[3];
    internal Rectangle enemy { get; set; }
    public DateTime LastDamageTime { get; set; } = DateTime.MinValue;
    
    public Enemy(Rectangle rectangleParameter, List<int> Stats)
    {
        enemy = rectangleParameter;
        EnemyStats = new Stats(Convert.ToInt32(Stats[0]),Convert.ToInt32(Stats[1]), Convert.ToInt32(Stats[2]), Convert.ToInt32(Stats[3]), Convert.ToInt32(Stats[4]), Convert.ToInt32(Stats[5]), Convert.ToInt32(Stats[6]), Convert.ToInt32(Stats[7]));
    }
}

public enum BossGimmick
{
    Tank,
    Damage,
    Speed
}

using System;
using System.Collections.Generic;
using Avalonia.Controls.Shapes;

namespace NEA;

public class Enemy
{
    
        internal Stats EnemyStats { get; set; }
        internal Item[] Loot { get; set; } = new Item[3];
        internal Weapon Weapon { get; set; }
        internal Rectangle enemy { get; set; }
        public DateTime LastDamageTime { get; set; } = DateTime.MinValue;
    
    public Enemy(Rectangle rectangleParameter, List<int> Stats)
    {
        enemy = rectangleParameter;
    }
}

public enum BossGimmick
{
    Tank,
    Damage,
    Speed
}

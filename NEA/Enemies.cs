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

public class Boss : Enemy
{
    public Boss(Rectangle rectangleParameter, List<int> stats, int BossSpecial) : base(rectangleParameter, stats)
    {

        Gimmick = (BossGimmick)BossSpecial;

    }

    public string Name { get; set; }
    internal BossGimmick Gimmick { get; set; }

    // Number of hits remaining for this boss in the current stage
    public int HitsRemaining { get; private set; }

    // Initialize boss hits as 5 * stage
    public void InitializeForStage(int stage)
    {
        HitsRemaining = Math.Max(1, 5 * stage);
        if(Gimmick == BossGimmick.Tank){
            HitsRemaining *= 2;
        }
        
    }

    // Apply a single hit; returns true when boss is defeated
    public bool ApplyHit()
    {
        if (HitsRemaining > 0)
        {
            HitsRemaining--;
        }
        return HitsRemaining <= 0;
    }
}

public enum BossGimmick
{
    Tank,
    Damage,
    Speed
}

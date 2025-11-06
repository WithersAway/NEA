using System;
using System.Collections.Generic;
using Avalonia.Controls.Shapes;

namespace NEA;

public class Boss : Enemy
{
    public Boss(Rectangle rectangleParameter, List<int> stats, int BossSpecial) : base(rectangleParameter, stats)
    {

        Gimmick = (BossGimmick)BossSpecial;

    }

    public string? Name { get; set; }
    internal BossGimmick Gimmick { get; set; }

    // Number of hits remaining for this boss in the current stage
    public double HitsRemaining { get; private set; }

    // Initialize boss hits as 5 * stage
    public void InitializeForStage(int stage)
    {
        HitsRemaining = Math.Max(1, 5 * stage);
        if(Gimmick == BossGimmick.Tank){
            HitsRemaining *= 2;
        }
        
    }


    public bool ApplyHit(double damage)
    {
        
        if (HitsRemaining > 0)
        {
            HitsRemaining -= damage;
        }
        return HitsRemaining <= 0;
    }
}

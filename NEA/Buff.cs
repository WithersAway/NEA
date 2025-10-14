using System;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using System.Xml.Schema;

namespace NEA;
public class Buff
{
    public Buff(){
        double valueBase;
    }
    public void ApplyBuff(Buff BuffToApply){
        if (BuffToApply.ToString().Contains('.'))
        {
            new SurvivalBuff(BuffToApply)
        }
    }
}
public class DamageBuff : Buff{
    int DamageToIncreaseBy;
    public DamageBuff(int value){
        DamageToIncreaseBy = value;
    }
}
public class SurvivalBuff : Buff {
    double survivalModifier;
    public SurvivalBuff(double value){
        survivalModifier = value;
    }
}
public class EnemyNerfBuff : Buff{

}
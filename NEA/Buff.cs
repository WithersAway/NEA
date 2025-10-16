using System;
using System.Collections.Generic;

namespace NEA;
public class Buff
{
    string buffIdentifier;
    public Buff(string buffID){
        buffIdentifier = buffID;
        double valueBase;
    }
    public void ApplyBuff(Buff BuffToApply){
        if (BuffToApply.ToString().Contains('.'))
        {
            //new SurvivalBuff(BuffToApply)
        }
    }
    public string getBuffID(){
        return buffIdentifier;
    }
}

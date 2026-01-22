using System;
using System.Collections.Generic;

namespace NEA;
public class Buff //Container class
{
    readonly string buffIdentifier;
    public Buff(string buffID){
        buffIdentifier = buffID;
        
    }
    public string getBuffID(){
        return buffIdentifier;
    }
}

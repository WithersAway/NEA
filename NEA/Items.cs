using System;
using System.Collections.Generic;

namespace NEA;

public class Item
{
    const double RelicMod = 1.25;
    const double ConsumableMod = 0.5;
    public Item(string name, int value, int rarity, bool relic, bool consumable, bool magic)
    {
        Name = name;
        Value = (int)Math.Round(CalculateCost(value));
        Rarity = (Rarity)rarity;
        Relic = relic;
        Consumable = consumable;
    }

    readonly List<double> RarityModifier = new List<double> { 1, 1.1, 1.25, 1.5, 2 };
    protected string Name { get; set; }
    int Value { get; set; }
    Rarity Rarity { get; }
    bool Relic { get; }
    bool Consumable { get; }
        
    public double CalculateCost(double cost)
    {
        cost = cost * (5 * Game.It.floor) * RarityModifier[(int)Rarity] * (int)Game.It.mode;
        if (Relic)
        {
            cost *= RelicMod;
        }
        if (Consumable)
        {
            cost *= ConsumableMod;
        }
        return cost;
    }
    //Price = [5 * Floor] * RarityMod * DifficultyMod * ConsumableMod * RelicMod

}
public class Weapon : Item
{
    Game.DamageTypes damageType;
    public Weapon(string name, int value, int rarity, bool relic, bool consumable, int damagetype, bool magic) : base(name, value, rarity, relic, consumable, magic)
    {
        damageType = (Game.DamageTypes)damagetype;
    }
}
public class Armor : Item
{
    public Armor(string name, int value, int rarity, bool relic, bool consumable, bool magic) : base(name, value, rarity, relic, consumable, magic)
    {
        relic = false;
        consumable = false;
    }
        
}
enum Rarity
{
    Common,
    Rare,
    Epic,
    Legendary,
    Unique
}

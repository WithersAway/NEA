using System;
using System.Collections.Generic;

namespace NEA;

public class Item
{
    const double RelicMod = 1.25;
    const double ConsumableMod = 0.5;
    bool magic;
    public Item(string name, int value, int rarity, bool relic, bool consumable, bool magic)
    {
        Name = name;
        SetValue((int)Math.Round(CalculateCost(value)));
        Rarity = (Rarity)rarity;
        Relic = relic;
        Consumable = consumable;
        this.magic = magic;
    }

    readonly List<double> RarityModifier = [1, 1.1, 1.25, 1.5, 2];
    protected string Name { get;}
    public string GetItemName(){
        return Name;
    }
    private int value;

    public int GetValue()
    {
        return value;
    }

    private void SetValue(int value)
    {
        this.value = value;
    }
    public bool GetMagic(){
        return magic;
    }

    public int GetRarity(){
        return (int)Rarity;
    }
    Rarity Rarity { get; }
    bool Relic { get; }
    bool Consumable { get; }
    string itemType;

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

    public string GetItemType(){
        return itemType;
    }
    
}
public class Weapon : Item
{
    Game.DamageTypes damageType;
    public Weapon(string name, int value, int rarity, bool relic, bool consumable, int damagetype, bool magic) : base(name, value, rarity, relic, consumable, magic)
    {
        relic = false;
        consumable = false;
        damageType = (Game.DamageTypes)damagetype;
    }
    public static Weapon convertToWeapon(Item item){
        Random r = new();
        Weapon weapon = new Weapon(item.GetItemName(), item.GetValue(), item.GetRarity(), false, false, r.Next(0, 13), item.GetMagic());
        return weapon;
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

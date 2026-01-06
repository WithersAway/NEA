using System;
using System.Collections.Generic;

namespace NEA;

public class Item
{
    const double RelicMod = 1.25;
    const double ConsumableMod = 0.5;
    bool magic;
    public Item(string name, int value, int rarity, bool relic, bool consumable, bool magic, int floor)
    {
        Name = name;
        SetValue((int)Math.Round(CalculateCost(value, floor)));
        Rarity = (Rarity)rarity;
        Relic = relic;
        Consumable = consumable;
        this.magic = magic;
        itemType = name[(name.IndexOf(' ')+1)..];
    }

    readonly List<double> RarityPriceModifier = [1, 1.1, 1.25, 1.5, 2];
    protected string Name { get;}
    public string GetItemName(){
        return Name;
    }
    private int value;

    public int GetValue()
    {
        return value;
    }
    public bool GetRelic(){
        return Relic;
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

    public double CalculateCost(double cost, int floor)
    {
        cost = cost * (5 * floor) * RarityPriceModifier[(int)Rarity];
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
    public Weapon(string name, int value, int rarity, bool relic, bool consumable, int damagetype, bool magic, int floor) : base(name, value, rarity, relic, consumable, magic, floor)
    {
        relic = false;
        consumable = false;
        damageType = (Game.DamageTypes)damagetype;
    }
    public static Weapon convertToWeapon(Item item, int floor){
        Random r = new();
        Weapon weapon = new Weapon(item.GetItemName(), item.GetValue(), item.GetRarity(), false, false, r.Next(0, 13), item.GetMagic(), floor);
        return weapon;
    }
    public static (string, string) GetWeaponAndModifier(Weapon weapon){
        string[] splitweapon = weapon.Name.Split(' ');
        return (splitweapon[0], splitweapon[1]);
    }
}
public class Armor : Item
{
    public Armor(string name, int value, int rarity, bool relic, bool consumable, bool magic, int floor) : base(name, value, rarity, relic, consumable, magic, floor)
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

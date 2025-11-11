using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Security.Cryptography;
using Avalonia.Controls.Shapes;

namespace NEA;

public class Shop{

    public List<Item> ItemsAvailable = new();
    public Shop(int floor){
        for (int i = 0; i < floor; i++)
        {
            ItemsAvailable.Add(GenItem(floor));
        }
    }
    public Item GenItem(int floor){
        string name;
        int value;
        int rarity;
        bool relic, consumable, magic;
        List<string> Adjective = ["Strong", "Warped", "Sighted", "Deadly", "Fine", "Grand", "Hasty", "Neat", "Rapid", "Unreal", "Precise", "Masterful", "Antique"];
        List<string> weaponType = ["Longbow", "Shortbow", "Crossbow", "Hand Crossbow", "Heavy Crossbow", "Light Crossbow", "Handgun", "Rifle", "Scoped Rifle", "Pistol", "Molten Fury", "Aerial Bane", "Toxicarp"];
        Random r = new();
        relic = false;
        consumable = false;
        if (!relic && !consumable)
        {
            name = Adjective[r.Next(0, Adjective.Count)] + " " + weaponType[r.Next(0, weaponType.Count)];    
        }
        else{
            name = "Placeholder Name For Relic/Consumable";
        }
        
        value = 0;
        rarity = 0;
        switch (r.Next(1,101)){
            case < 40:
                rarity = 0;
                name = "Common " + name;
                break;
            case < 60:
                rarity = 1;
                name = "Rare " + name;
                break;
            case < 75:
                rarity = 2;
                name = "Epic " + name;
                break;
            case < 95:
                rarity = 3;
                name = "Legendary " + name;
                break;
            case < 100:
                rarity = 4;
                name = "Unique " + name;
                break;
        }
        if (rarity >= 3)
        {
            magic = true;
        }
        else{
            magic = false;
        }
        Item itemToReturn;
        if (!relic && !consumable)
        {
            Weapon weapon = new(name, value, rarity, relic, consumable, 1, magic, floor);
            itemToReturn = weapon;
        }
        else{
            Item item = new(name, value, rarity, relic, consumable, magic, floor);
            itemToReturn = item;
        }
        
        
        return itemToReturn;
    }

    
}
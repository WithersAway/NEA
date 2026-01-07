using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection.Metadata;
using System.Security.Cryptography;
using Avalonia.Controls.Shapes;

namespace NEA;

public class Shop{

    public List<Item> ItemsAvailable = new();
    public Shop(int floor){
        for (int i = 1; i < floor; i++)
        {
            ItemsAvailable.Add(GenItem(floor));
        }
        ItemsAvailable.Add(GenRelic(floor));
    }
    public Item GenItem(int floor){
        string name;
        int value;
        int rarity;
        bool relic, consumable, magic;
        List<string> Adjective = ["Strong", "Warped", "Sighted", "Deadly", "Fine", "Grand", "Hasty", "Neat", "Rapid", "Unreal", "Precise", "Masterful", "Antique"];
        List<string> weaponType = ["Longbow", "Shortbow", "Crossbow", "HandCrossbow", "HeavyCrossbow", "LightCrossbow", "Handgun", "Rifle", "ScopedRifle", "Pistol", "MoltenFury",
         "AerialBane", "Toxicarp"];
        Random r = new();
            name = Adjective[r.Next(0, Adjective.Count)] + " " + weaponType[r.Next(0, weaponType.Count)];    
        value = 0;
        rarity = 0;
        switch (r.Next(1,101)){
            case < 40:
                rarity = 0;
                
                break;
            case < 60:
                rarity = 1;
                
                break;
            case < 75:
                rarity = 2;
                
                break;
            case < 95:
                rarity = 3;
                
                break;
            case < 100:
                rarity = 4;
                
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
            Weapon weapon = new(name, value, rarity, false, false, 1, magic, floor);
            itemToReturn = weapon;
        
        
        return itemToReturn;
    }
    public Item GenRelic(int floor){
        List<string> relicNames = new()
        {
            "AxiomCore",
            "ChronicleOfAshAndLight",
            "NullSigil",
            "EonLens",
            "SeveranceRelic",
            "VaultedStar",
            "ParadoxKeystone",
            "PaleEngine",
            "EchoReliquary",
            "MeridianShard"
        };

        string name;
        
        int value;
        int rarity;
        bool relic, consumable, magic;
        relic = true;
        consumable = false;
        Random r = new();
        name = relicNames[r.Next(0,9)];
        value = 0;
        rarity = 0;
        switch (r.Next(1,101)){
            case < 40:
                rarity = 0;
                
                break;
            case < 60:
                rarity = 1;
                
                break;
            case < 75:
                rarity = 2;
                
                break;
            case < 95:
                rarity = 3;
                
                break;
            case < 100:
                rarity = 4;
                
                break;
        }
        if (rarity >= 3)
        {
            magic = true;
        }
        else{
            magic = false;
        }
        
        Item item = new Item(name, value, rarity, relic, consumable, magic, floor);
        return item;
    }

    
}
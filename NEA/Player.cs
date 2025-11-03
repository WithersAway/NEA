using System;
using System.Collections.Generic;
using Avalonia.Controls.Shapes;

namespace NEA;

public class Player(List<string> InitArgs, Rectangle rectIN)
{
    public Rectangle PlayerRectangle { get; set; } = rectIN;
    public List<Buff> PlayerUpgrades { get; set; } = [];
    public string Name { get; set; } = InitArgs[0];
    internal List<Item> Items { get; set; } = [];
    internal Weapon playerWeapon = new("Basic Gun", 1, 0, false, false, 7, false, 1);
    public Stats PlayerStats { get; set; } = new Stats(Convert.ToInt32(InitArgs[1]), Convert.ToInt32(InitArgs[2]), Convert.ToInt32(InitArgs[3]), Convert.ToInt32(InitArgs[4]), Convert.ToInt32(InitArgs[5]), Convert.ToInt32(InitArgs[6]), Convert.ToInt32(InitArgs[7]), Convert.ToInt32(InitArgs[8]));
    internal Specialise specialise { get; } = (Specialise)Convert.ToInt32(InitArgs[9]); //specialise is for class
    protected static int invMax = 5;
    private int Ammo = 10;
    private int playerAmmoMax = 10;

    public int getPlayerAmmoMax(){
        return playerAmmoMax;
    }
    public Weapon GetWeapon(){
        return playerWeapon;
    }
    public void setPlayerAmmoMax(int value){
        playerAmmoMax = value;
    }
    private int playerDamageBase = 3;
    public int getPlayerDamageBase(){
        return playerDamageBase;
    }
    public void setPlayerDamageBase(int value){
        playerDamageBase = value;
    }
    private double scavengeModifier = 0.1;
    public double getScavengeMod(){
        return scavengeModifier;
    }
    public void setScavengerModifier(double value){
        scavengeModifier += value;
    }
    private double playerDamage = 1;
    public double getPlayerDamage(){
        return playerDamage;
    }
    public void setPlayerDamage(double value){
        playerDamage = value;
    }
    private double projectileSize = 10;

    //add playerdead check
    public bool IsPlayerDead()
    {
        {
            bool PlayerAlive = !this.PlayerStats.AnyStatsZero();
            return PlayerAlive;
        }
    }
    public void AddItemToInventory(Item item){
        if (Items.Count <= invMax)
        {
            Items.Add(item);
        }
    }
    public void SetAmmo(int NewAmmo)
    {
        Ammo = NewAmmo;
    }
    public int GetAmmo(){
        return Ammo;
    }
    public void SetProjSize(double newProjSize)
    {
        projectileSize = newProjSize;
    }
    public double GetProjSize(){
        return projectileSize;
    }

    internal enum Specialise
    {
        Warrior = 0,
        Archer = 1,
        Mage = 2,
        Thief = 3
    }
    enum InvMaxes
    {
        Warrior = 5,
        Archer = 5,
        Mage = 5,
        Thief = 8
    }
}
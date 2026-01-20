// GameEnums.cs - Central enum definitions for Hold the Line
// Location: Assets/_HoldTheLine/Scripts/Core/

namespace HoldTheLine
{
    /// <summary>
    /// Main game states controlling flow and UI
    /// </summary>
    public enum GameState
    {
        Menu,
        Playing,
        WaveTransition,
        UpgradeObtained,
        Fail
    }

    /// <summary>
    /// Weapon upgrade tiers - each tier improves stats
    /// </summary>
    public enum WeaponTier
    {
        Tier1_Pistol = 0,
        Tier2_SMG = 1,
        Tier3_Rifle = 2,
        Tier4_Shotgun = 3,
        Tier5_Minigun = 4
    }

    /// <summary>
    /// What the weapon system is currently targeting
    /// </summary>
    public enum TargetPriority
    {
        Zombies,
        UpgradeTarget
    }

    /// <summary>
    /// Types of multiplier pickups
    /// </summary>
    public enum MultiplierType
    {
        DamageX2,
        DamageX3,
        FireteamPlus1,
        FireteamPlus3
    }

    /// <summary>
    /// Pool types for object pooling system
    /// </summary>
    public enum PoolType
    {
        Bullet,
        Zombie,
        Pickup,
        UpgradeTarget
    }
}

// ***************************************************************************************************
// DuelMod settings
//
// DuelMod is a 1v1 dueling game type based on the T1/T2 Duel MOD by Sly One, system-z and Kahlua,
// with the MA ruleset from SquirrelOfDeath's MA Duel MOD.
//
// Loaded by scripts/autoexec/zzzDuelModSupport.cs at startup.
// ***************************************************************************************************

$Duel_PrefsLoaded = 1;

//-------------------------------------------------------------------------------
// Server settings
//-------------------------------------------------------------------------------

// Ruleset used when a DuelMod map loads: "Duel" or "MA".
// "Duel" - any hit from your duel partner counts.
// "MA"   - MA practice ruleset (see $Duel_MAMode below).
// Players can vote to change it (the map restarts when the vote passes).
$Duel_Ruleset = "Duel";

// After a DuelMod map ends, put players straight onto a team on the next map (1),
// or bring them in as observers (0).
$Duel_AutoJoinAfterMap = 1;

//-------------------------------------------------------------------------------
// Duel tuning
//-------------------------------------------------------------------------------

// Starting distance between players on duel start (in meters)
$Duel_SpawnRadius = 25;

// Acceptable terrain angles for player spawn points in degrees
// (0 = perfectly horizontal terrain, 90 = vertical wall)
$Duel_MinSpawnAngle = 0;
$Duel_MaxSpawnAngle = 20;

// Acceptable difference in player elevation at the start of the duel (in meters)
$Duel_MaxElevationDiff = 2;

// Time (in ms) before the duel victor is script-killed after killing the loser
$Duel_VictorKillTime = 3000;

// Time (in ms) between the victor being script-killed and being placed in observer mode.
// This is the window in which the duel partners can re-request the duel.
$Duel_VictorForceObserverTime = 6000;

// Time (in ms) that players remain invincible after the countdown ends
$Duel_StartInvincibleTime = 2000;

// Time (in ms) that a duel offer lasts before expiring
$Duel_OfferExpirationTime = 10000;

// Interval (in ms) for checking partner distance, and the distance at which
// duel partners become illuminated (waypointed) to each other.
$Duel_IlluminationInterval = 1000;
$Duel_IlluminationDistance = 135;

// Interval (in ms) for checking partner distance, and the distance at which the duel is a draw
$Duel_DistanceDrawInterval = 3000;
$Duel_DistanceDrawDistance = 1000;

// Interval (in ms) for checking whether both players are out of ammo (draw)
$Duel_AmmoDrawInterval = 5000;

// Minimum time (in seconds) left in the map for duels to be requested
$Duel_RequestAllowTime = 1 * 60;

// Height above terrain (in meters) that counts as a mid-air (MA) hit
$Duel_MAThreshold = 5;

//-------------------------------------------------------------------------------
// MA ruleset
//-------------------------------------------------------------------------------

// MA sub-modes, cycled by admins (commandToServer('MADuelNextMode') or the admin vote menu).
$Duel_MAModes = "HoppingBotsEasy\tHoppingBotsMedium\tHoppingBotsHard\tGroundShots\tDiscLauncher\tChaingun\tShocklance\tNormal";

// Starting MA sub-mode (index into $Duel_MAModes; 7 = Normal)
$Duel_MAMode = 7;

// Ammo given back on each shot in the unlimited-ammo sub-modes
$Duel_MAAmmoAmount = 100;

// Delay (in ms) before a deployed mine detonates under the MA ruleset
$Duel_MAMineDetonateTime = 2000;

// Hopping bot movement at the three difficulty levels (easy, medium, hard)
$Duel_HopLateralScale[0] = 0;
$Duel_HopLateralScale[1] = 400;
$Duel_HopLateralScale[2] = 1200;
$Duel_HopVerticalBase[0] = 2200;
$Duel_HopVerticalBase[1] = 2500;
$Duel_HopVerticalBase[2] = 3000;
$Duel_HopVerticalScale[0] = 200;
$Duel_HopVerticalScale[1] = 500;
$Duel_HopVerticalScale[2] = 750;

//-------------------------------------------------------------------------------
// Maps
//
// Missions offered for DuelMod. Missing missions are skipped automatically.
// Players vote for DuelMod to play them; while a DuelMod map is running, the server rotates
// through these maps (other game types' rotations are unaffected).
//-------------------------------------------------------------------------------

$Duel_MapCount = -1;
$Duel_Map[$Duel_MapCount++] = "Abominable";
$Duel_Map[$Duel_MapCount++] = "Alcatraz";
$Duel_Map[$Duel_MapCount++] = "BeggarsRun";
$Duel_Map[$Duel_MapCount++] = "Damnation";
$Duel_Map[$Duel_MapCount++] = "DustToDust";
$Duel_Map[$Duel_MapCount++] = "Equinox";
$Duel_Map[$Duel_MapCount++] = "Escalade";
$Duel_Map[$Duel_MapCount++] = "Gehenna";
$Duel_Map[$Duel_MapCount++] = "IceBound";
$Duel_Map[$Duel_MapCount++] = "Insalubria";
$Duel_Map[$Duel_MapCount++] = "Invictus";
$Duel_Map[$Duel_MapCount++] = "JacobsLadder";
$Duel_Map[$Duel_MapCount++] = "Masada";
$Duel_Map[$Duel_MapCount++] = "Oasis";
$Duel_Map[$Duel_MapCount++] = "Tombstone";
$Duel_Map[$Duel_MapCount++] = "UltimaThule";
$Duel_Map[$Duel_MapCount++] = "Whiteout";
$Duel_Map[$Duel_MapCount++] = "TWL_Stonehenge";
$Duel_Map[$Duel_MapCount++] = "TWL_Katabatic";
$Duel_Map[$Duel_MapCount++] = "TWL_BlueMoon";
$Duel_Map[$Duel_MapCount++] = "TWL_Harvester";
$Duel_Map[$Duel_MapCount++] = "TWL_BeachBlitz";
$Duel_Map[$Duel_MapCount++] = "SkinnyDipLak";   // LakRabbit version of the TR2 map (no TR2 objects)
$Duel_Map[$Duel_MapCount++] = "Crater71Lak";    // LakRabbit version of the TR2 map (no TR2 objects)
$Duel_Map[$Duel_MapCount++] = "WalledIn";
$Duel_Map[$Duel_MapCount++] = "SmogArena";
$Duel_Map[$Duel_MapCount++] = "Aeroena";
$Duel_Map[$Duel_MapCount++] = "Checkmate";
$Duel_MapCount++;

//-------------------------------------------------------------------------------
// Banned items per ruleset (1 = banned)
//-------------------------------------------------------------------------------

// Shared by both rulesets
$Duel_Ban["All", "CameraGrenade"] = 1;
$Duel_Ban["All", "Beacon"] = 1;
$Duel_Ban["All", "InventoryDeployable"] = 1;
$Duel_Ban["All", "PulseSensorDeployable"] = 1;
$Duel_Ban["All", "MotionSensorDeployable"] = 1;
$Duel_Ban["All", "TurretIndoorDeployable"] = 1;
$Duel_Ban["All", "TurretOutdoorDeployable"] = 1;
$Duel_Ban["All", "ElfBarrelPack"] = 1;
$Duel_Ban["All", "MortarBarrelPack"] = 1;
$Duel_Ban["All", "PlasmaBarrelPack"] = 1;
$Duel_Ban["All", "AABarrelPack"] = 1;
$Duel_Ban["All", "MissileBarrelPack"] = 1;

// Duel ruleset
$Duel_Ban["Duel", "Mine"] = 1;

// MA ruleset
$Duel_Ban["MA", "MissileLauncher"] = 1;
$Duel_Ban["MA", "CloakingPack"] = 1;
$Duel_Ban["MA", "SatchelCharge"] = 1;
$Duel_Ban["MA", "FlareGrenade"] = 1;

// Every item named above (used to rebuild the DuelMod ban list when the ruleset changes)
$Duel_BanItems = "CameraGrenade Beacon InventoryDeployable PulseSensorDeployable MotionSensorDeployable" SPC
                 "TurretIndoorDeployable TurretOutdoorDeployable ElfBarrelPack MortarBarrelPack PlasmaBarrelPack" SPC
                 "AABarrelPack MissileBarrelPack Mine MissileLauncher CloakingPack SatchelCharge FlareGrenade";

![Tribes 2](https://playt2.com/images/avatar300.png)

# TacoServer
## A Tribes 2 server with competitive features that is also open-source.

---


Discord: [Tribes 2 Discord](https://playt2.com/discord)


### Features
 - Built Light-Weight
 - Team Autobalancing
 - Team-Balance and Base Rape Messages
 - Pack Restrictions based on Population
 - No Base Rape based on Population
 - Turret Activation based on Population
 - Loading screen customization
 - Low MatchTime warnings
 - Late Time Voting
 - Vote Reminders and Restrictions
 - Vote System Rework
 - Various Bug Fixes and Exploit Fixes
 - Full Players Stats that can be viewed In-game
 - Discord Bot Connectivity
 - Improved Map Rotation
 - Pickup game improvements
 - Ability to Lock Teams in tournament mode
 - LakRabbit improvements
 - Various tweaks for PUBs
 - AFK Timeouts and Observer Kicking
 - Improved Ban System
 - Added Deathmatch, LCTF
 - Added DuelMod (1v1 duels with Duel and MA rulesets)
 - Support for Siege
 - Shocklance Only Deathmatch Mode
 - Various logging improvements
 - Teamkill Warnings
 - Flag throw and Flag collision fixes
 - Various crashing fixes and Anti-Spew Lag protection
 - Item toss duplication fix
 - Unified water viscosity
 - Persistent Gagging (Globally muting a player)

---

### Important Gameplay Changes over stock classic
 - OG blaster buffed against Heavy Armors (Not Tournament Mode Only)
 - Max FOV 120 >> 138 (For Wide Screens)
 - Item respawn time can be changed (Optional)
 - All water viscosity follows a global preset (3)
 - <del>No Vehicle Zone around flag is bigger 5 >> 15</del>
 - <del>Heavy Shield Nerfed (54 to block 1 DMG, is now 60)</del>
 - <del>Cloak Sound 50m >> 55m</del>
 - <del>Bigger ground dust puffs (For cloak) 0.4 larger</del>
 - <del>Sensor Jammer jam radius is buffed to jam cloaks 30 >> 45</del>
 - <del>Sensor Jammer can passively see cloaker's triangle</del>
 - <del>Mortor reload rework (Player can throw, but must wait until reload time has passed)</del>
 - <del>Tank Shield Nerf (135 to block 1 DMG, is now 142)</del>
 - <del>Whiteout grenades are less seizurely (Due to spamming) 0.9 >> 0.78</del>
 - <del>Spawn fade-in effect</del>

#### DuelMod
 - 1v1 duels: everyone starts as an observer; open the score screen (F2) and click a name to challenge them
 - Accept or decline a challenge with VOTE YES/NO; VOTE YES after a duel asks for a re-duel
 - Two rulesets: Duel (any hit from your opponent counts) and MA (mid-air practice modes)
 - Players can vote to change the ruleset (the map restarts); admins can change the MA mode
 - Settings and the DuelMod map list are in Classic/Prefs/DuelPrefs.cs
 - Some DuelMod maps come from map packs ([TacoMaps](https://github.com/ChocoTaco1/TacoMaps) and arenaMaps.vl2); missing maps are skipped
 - Stats are tracked separately for each ruleset
 - Not available in tournament mode

---


## Installation


### Prerequisites
 - Update to Classic 1.5.2 (Found here: https://tribes2stats.com/files/mods/)

### Setup
 - Meant to be installed on top of Classic 1.5.2
 - If a file isnt on this github it is unmodified in Classic 1.5.2
 - Delete old classic folder (tribes2gsi.exe comes with 1.0.0)
 - Extract Classic 1.5.2 to GameData folder
 - Place files from this github in Classic folder (Overwriting old files)
 - Run once to create Serverprefs
 - Open Classic/Prefs/Serverprefs.cs in notepad and modify to your wishes
 - Run using Classic_dedicated_server.bat

----

### Credits & Thanks
 - BattleLore for this template and help
 - The T2 community for the free testing
 - DarkTiger, Ravin, Krash
 - Sly One, system-z and Kahlua for the original Duel MOD, and SquirrelOfDeath for MA Duel MOD
 - The TribesNext community

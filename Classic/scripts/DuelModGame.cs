// DisplayName = Duel MOD

//--- GAME RULES BEGIN ---
//Original T1 and T2 DuelMOD by Sly One, system-z and Kahlua; MA ruleset by SquirrelOfDeath
//Select your inventory loadout as usual (NUM ENTER)
//Request a duel by clicking a light blue name in the Score Screen (F2)
//Accept/decline a duel by pressing VOTE YES/NO
//Re-request a duel by pressing VOTE YES
//Duel ruleset: any hit from your duel partner counts
//MA ruleset: mid-air practice modes, changed by admins
//--- GAME RULES END ---

// DuelMod - 1v1 duels for TacoServer
//
// Everything that changes how the server behaves lives in package DuelModGame (activated only
// while a DuelMod map is running) or in DuelModGame:: methods. Helper functions are prefixed
// Duel_ and settings $Duel_ (see prefs/DuelPrefs.cs).

if(!$Duel_PrefsLoaded)
   exec("prefs/DuelPrefs.cs");

$VoteMessage["VoteDuelRuleset"] = "change";
$VoteMessage["DuelMAMode"] = "change";

datablock SensorData(DuelPlayerSensor)
{
   detects = false;
   detectsUsingLOS = false;
   detectsPassiveJammed = false;
   detectRadius = 0;
   detectionPings = false;
   detectsFOVOnly = true;
   detectFOVPercent = 1.3;
   useObjectFOV = true;
};

package DuelModGame
{

function Armor::damageObject(%data, %targetObject, %sourceObject, %position, %amount, %damageType, %momVec, %mineSC)
{
   %targetObject.lastMomVec = %momVec;
   %targetClient = %targetObject.client;

   // Mines carry their owner in %mineSC, which survives the owner's player object
   if(isObject(%mineSC))
      %sourceClient = %mineSC;
   else
      %sourceClient = isObject(%sourceObject) ? %sourceObject.client : 0;

   if(Duel_shouldApplyDamage(%targetObject, %sourceObject, %sourceClient, %amount, %damageType))
   {
      %targetClient.DoNotApplyImpulse = false;
      if(%targetObject != %sourceObject && isObject(%sourceClient))
         messageClient(%sourceClient, 'DuelPartnerDamaged', '~wgui/buttonOver.wav');
      parent::damageObject(%data, %targetObject, %sourceObject, %position, %amount, %damageType, %momVec, %mineSC);
   }
   else
      %targetClient.DoNotApplyImpulse = true;
}

function Armor::shouldApplyImpulse(%data, %obj)
{
   return (!%obj.client.InCountdown) && (!%obj.client.DoNotApplyImpulse);
}

function Armor::onTrigger(%data, %player, %triggerNum, %val)
{
   // MA unlimited-ammo modes top up the mode's ammo when firing
   %ammo = %player.client.duelUnlimitedAmmo;
   if(%triggerNum == 0 && %ammo !$= "" && %player.getInventory(%ammo) < $Duel_MAAmmoAmount)
      %player.setInventory(%ammo, $Duel_MAAmmoAmount);

   if(!%player.client.InCountdown)
      parent::onTrigger(%data, %player, %triggerNum, %val);
}

function Observer::setMode(%data, %obj, %mode, %targetObj)
{
   // Players arriving from the previous map have no player to orbit until they duel
   if(%mode $= "pre-game" && !isObject(%targetObj))
      %mode = "observerFly";

   parent::setMode(%data, %obj, %mode, %targetObj);

   if(%mode $= "observerFly")
      %obj.setTransform("0 0 200 1 0 0 1.4");
}

function Observer::onTrigger(%data, %obj, %trigger, %state)
{
   if(((%obj.mode $= "observerFly") && (%trigger != 0)) || (%obj.mode $= "observerFollow"))
      parent::onTrigger(%data, %obj, %trigger, %state);
}

function serverCmdDeclineTask(%client, %issueClient, %description, %teamCmd)
{
   if(getSubstr(%description, strlen(%description) - 44, 44) $= "Press ACCEPT COMMAND key to accept challenge")
      Game.DeclineDuelOffer(%issueClient);
   else
      parent::serverCmdDeclineTask(%client, %issueClient, %description, %teamCmd);
}

function serverCmdClientJoinGame(%client)
{
   messageClient(%client, 'LeaveMissionArea', '\c1You can only join the game by requesting or accepting a duel.');
}

function serverCmdClientAddToGame(%client, %targetClient)
{
   messageClient(%client, 'LeaveMissionArea', '\c1You can only join the game by requesting or accepting a duel.');
}

function serverCmdClientMakeObserver(%client)
{
   messageClient(%client, 'LeaveMissionArea', '\c1You cannot leave the duel by becoming an observer.');
}

function ELFProjectileData::zapTarget(%data, %projectile, %target, %targeter)
{
   parent::zapTarget(%data, %projectile, %target, %targeter);

   if((%target.invincible) || (%target.client.clientDuelingWith != %targeter.client))
   {
      %targeter.elf_zap = true;
      %oldERate = %target.getRechargeRate();
      %target.setRechargeRate(%oldERate + %data.drainEnergy);
   }
   else
      %targeter.elf_zap = false;
}

function ELFProjectileData::unzapTarget(%data, %projectile, %target, %targeter)
{
   parent::unzapTarget(%data, %projectile, %target, %targeter);

   if(!%target.isDisabled() && %targeter.elf_zap)
   {
      %oldERate = %target.getRechargeRate();
      %target.setRechargeRate(%oldERate - %data.drainEnergy);
   }
}

function serverCmdSetPlayerVote(%client, %vote)
{
   if(%client.clientDuelRequestedBy != 0)
   {
      if(%vote)
         Game.AcceptDuelOffer(%client.clientDuelRequestedBy);
      else
         Game.DeclineDuelOffer(%client.clientDuelRequestedBy);
   }
   else if((%client.clientDuelingWith == 0) && (%client.JustDueled != 0) && %vote && Duel_allowRequest())
   {
      if(%client.JustDueled.WantsReDuel || %client.JustDueled.isAIControlled())
      {
         if(isObject(%client.player))
         {
            cancel(%client.killTimer);
            Game.DuelScriptKill(%client);
         }

         if(isObject(%client.JustDueled.player))
         {
            cancel(%client.JustDueled.killTimer);
            Game.DuelScriptKill(%client.JustDueled);
         }

         if(%client.JustDueled.isAIControlled())
            Duel_equipBotAfterPlayer(%client.JustDueled, %client);

         cancel(%client.reduelTimer);
         cancel(%client.JustDueled.reduelTimer);
         Game.SetUpDuel(%client, %client.JustDueled);
         Game.CountDownDuel(%client, %client.JustDueled, 5);
      }
      else if(!isObject(%client.player))
      {
         bottomPrint(%client.JustDueled, "\nPlayer " @ getTaggedString(%client.name) @ " wants a re-duel.  Press VOTE YES to accept.", 0, 3);
         bottomPrint(%client, "\nDuel re-request sent to player " @ getTaggedString(%client.JustDueled.name) @ ".", 0, 3);
         %client.WantsReDuel = true;
      }
   }
   else
      parent::serverCmdSetPlayerVote(%client, %vote);
}

function displayObserverHud(%client, %targetClient, %potentialClient)
{
   if(!Duel_allowRequest())
      bottomPrint(%client, "\nDuels cannot be requested now.  You have to wait for mission change.", 0, 3);
   else if(%client.clientDuelRequestedBy == 0)
   {
      if(%targetClient > 0)
         bottomPrint(%client, "\nYou are now observing: " @ getTaggedString(%targetClient.name), 0, 3);
      else
         bottomPrint(%client, "<color:ffffff>Welcome to Duel MOD! Ruleset: <color:ffff00>" @ Duel_rulesetName() @ "<color:ffffff>\nOriginal T1 and T2 DuelMOD by Sly One, system-z and Kahlua.\nPress F2 and click on a <color:00dcdc>light blue <color:ffffff>name in the lower half to request a duel.", 0, 3);
   }
}

// Team chat also reaches your duel partner (who is on a different team).
// Global chat needs no override: TacoServer already shows observer chat to everyone.
function chatMessageTeam(%sender, %team, %msgString, %a1, %a2, %a3, %a4, %a5, %a6, %a7, %a8, %a9, %a10)
{
   parent::chatMessageTeam(%sender, %team, %msgString, %a1, %a2, %a3, %a4, %a5, %a6, %a7, %a8, %a9, %a10);

   // spamAlert() in the parent sets isSpamming when it blocked the message
   %partner = %sender.clientDuelingWith;
   if(%msgString !$= "" && !%sender.isSpamming && isObject(%partner) && %partner.team != %sender.team)
      chatMessageClient(%partner, %sender, %sender.voiceTag, %sender.voicePitch, %msgString, %a1, %a2, %a3, %a4, %a5, %a6, %a7, %a8, %a9, %a10);
}

// Skip the match start countdown (duels have their own)
function Countdown(%timeMS)
{
   $countdownStarted = true;
   Game.StartMatch();
}

function detonateFlashGrenade(%hg)
{
   $Duel_WhiteoutLastThrownBy = %hg.sourceObject.client;
   parent::detonateFlashGrenade(%hg);
}

function Player::setWhiteout(%player, %val)
{
   if(($Duel_WhiteoutLastThrownBy == %player.client) || ($Duel_WhiteoutLastThrownBy == %player.client.clientDuelingWith))
      parent::setWhiteout(%player, %val);
}

// onFire returns the projectile: callers above (missile locking, dtStats shot counting) need it
function MissileLauncherImage::onFire(%data, %obj, %slot)
{
   %target = %obj.getLockedTarget();
   if(%target.client == %obj.client.clientDuelingWith)
      return parent::onFire(%data, %obj, %slot);
   return 0;
}

function ShapeBaseImageData::onFire(%data, %obj, %slot)
{
   if(!%obj.client.InCountdown)
      return parent::onFire(%data, %obj, %slot);
   return 0;
}

function AIConnection::startMission(%client)
{
   %client.team = 0;
   %client.Initialized = true;
   Game.AIHasJoined(%client);
}

function ProjectileData::onCollision(%data, %projectile, %targetObject, %modifier, %position, %normal)
{
   %projName = %projectile.dataBlock.getName();
   %isPartner = (%targetObject.client == %projectile.sourceObject.client.clientDuelingWith);

   // MA ruleset: only hits from the mode's projectile mark the target as damageable
   if(%isPartner && %targetObject.client.duelMAProjectile !$= "" && %projName $= %targetObject.client.duelMAProjectile)
      %targetObject.client.MADuelApplyDamage = true;

   // MA detection sound
   if(%isPartner && %projName $= "DiscProjectile" && Duel_playerHeight(%targetObject) > $Duel_MAThreshold)
      messageClient(%projectile.sourceObject.client, 'DuelMADetected', '~wfx/misc/flag_lost.wav');

   parent::onCollision(%data, %projectile, %targetObject, %modifier, %position, %normal);
}

// MA ruleset: mines detonate on their own shortly after being deployed
function deployMineCheck(%mineObj, %player)
{
   if($Duel_RulesetActive $= "MA")
      schedule($Duel_MAMineDetonateTime, %mineObj, "explodeMine", %mineObj, true);
   else
      parent::deployMineCheck(%mineObj, %player);
}

// Hopping bots (MA ruleset)
function Player::performHop(%player)
{
   %difficulty = %player.client.duelHopDifficulty;
   %vector = getRandom() @ " " @ getRandom() @ " 0";
   %vector = vectorSub(%vector, ".5 .5 0");
   %vector = vectorScale(%vector, 2 * $Duel_HopLateralScale[%difficulty]);
   %vector = getWords(%vector, 0, 1);
   %vector = %vector @ " " @ ($Duel_HopVerticalBase[%difficulty] + getRandom() * $Duel_HopVerticalScale[%difficulty]);
   %player.applyImpulse(%player.position, %vector);
}

function Player::monitorHop(%player)
{
   %delay = 0;
   if(Duel_playerHeight(%player) < 2.0)
   {
      // Don't hop right away to prevent skiing, and delay the next check so we don't double hop
      %player.schedule(1000, "performHop");
      %delay = 2000;
   }

   if(%player.client.duelHoppingBots)
      %player.client.hopMonitor = %player.schedule(100 + %delay, "monitorHop");
}

// Admin: cycle the MA sub-mode. Same command name as the MA Duel MOD client keybind.
function serverCmdMADuelNextMode(%client)
{
   if(!isObject(%client))
      return;

   if(!%client.isAdmin)
   {
      messageClient(%client, 'MsgClient', "\c2You must be an admin to change the mode.");
      return;
   }

   Game.nextMAMode(%client);
}

};

//-----------------------------------------------------------------------------
// Rulesets
//-----------------------------------------------------------------------------

function Duel_rulesetName()
{
   if($Duel_RulesetActive $= "MA")
      return "MA (" @ getField($Duel_MAModes, $Duel_MAMode) @ ")";
   return "Duel";
}

// Build the DuelMod ban list for the active ruleset
function Duel_applyBanList()
{
   for(%i = 0; (%item = getWord($Duel_BanItems, %i)) !$= ""; %i++)
      $InvBanList[DuelMod, %item] = ($Duel_Ban["All", %item] || $Duel_Ban[$Duel_RulesetActive, %item]) ? 1 : 0;
}

// Record the rules for a duel on one of its players: the active ruleset and, for MA, the current
// sub-mode. Set when the duel is set up, so an admin's mode change applies from the next duel.
function Duel_setDuelMode(%client)
{
   %client.duelMAProjectile = "";
   %client.duelUnlimitedAmmo = "";
   %client.duelHoppingBots = false;
   %client.duelMAOnly = false;
   %client.duelGroundShots = false;
   %client.duelHopDifficulty = 0;

   if($Duel_RulesetActive !$= "MA")
      return;

   switch$(getField($Duel_MAModes, $Duel_MAMode))
   {
      case "HoppingBotsEasy":
         %client.duelUnlimitedAmmo = "DiscAmmo";
         %client.duelMAOnly = true;
         %client.duelHoppingBots = true;
         %client.duelHopDifficulty = 0;
      case "HoppingBotsMedium":
         %client.duelUnlimitedAmmo = "DiscAmmo";
         %client.duelMAOnly = true;
         %client.duelHoppingBots = true;
         %client.duelHopDifficulty = 1;
      case "HoppingBotsHard":
         %client.duelUnlimitedAmmo = "DiscAmmo";
         %client.duelMAOnly = true;
         %client.duelHoppingBots = true;
         %client.duelHopDifficulty = 2;
      case "GroundShots":
         %client.duelUnlimitedAmmo = "DiscAmmo";
         %client.duelGroundShots = true;
         %client.duelHoppingBots = true;
      case "DiscLauncher":
         %client.duelMAProjectile = "DiscProjectile";
         %client.duelUnlimitedAmmo = "DiscAmmo";
      case "Chaingun":
         %client.duelMAProjectile = "ChaingunBullet";
         %client.duelUnlimitedAmmo = "ChaingunAmmo";
      case "Shocklance":
         %client.duelMAProjectile = "BasicShocker";
   }
}

// Bots stand still and only hop in the hopping-bot modes. Called at each duel start because
// StartMatch() turns the AI back on. AISystemEnabled() is global and reset on every mission
// load (server.cs), so this cannot outlive the DuelMod map.
function Duel_applyAIMode(%client)
{
   if(%client.duelHoppingBots && !%client.duelGroundShots)
   {
      AISystemEnabled(false);
      $Duel_AIDisabled = true;
   }
   else if($Duel_AIDisabled)
   {
      AISystemEnabled(true);
      $Duel_AIDisabled = false;
   }
}

function Duel_shouldApplyDamage(%targetObject, %sourceObject, %sourceClient, %amount, %damageType)
{
   %targetClient = %targetObject.client;
   %self = (%targetObject == %sourceObject) || (%sourceClient != 0 && %sourceClient == %targetClient);

   // Damage from the duel partner
   %apply = (%sourceClient != 0) && (%targetClient.clientDuelingWith == %sourceClient);

   if($Duel_RulesetActive $= "MA")
   {
      // Only while the target is in the air
      if(%targetClient.duelMAOnly)
         %apply = %apply && (Duel_playerHeight(%targetObject) > $Duel_MAThreshold);

      // Only while the target is near the ground
      if(%targetClient.duelGroundShots)
         %apply = %apply && (Duel_playerHeight(%targetObject) <= $Duel_MAThreshold);

      // Only after a hit from the mode's projectile (see ProjectileData::onCollision)
      %apply = %apply && (%targetClient.MADuelApplyDamage || %targetClient.duelMAProjectile $= "" ||
                          (%targetClient.duelMAProjectile $= "BasicShocker" && %damageType == $DamageType::Shocklance));
   }

   %apply = %apply || %self || (%damageType == $DamageType::Suicide);

   // Ground damage, except for hopping bots
   if(%damageType == $DamageType::Ground)
      %apply = %apply || !(%targetClient.duelHoppingBots && %targetObject.isBot);

   %apply = %apply || %targetObject.scriptKilled || (%damageType == $DamageType::Lava);

   if(%apply && %amount > 0)
      %targetClient.MADuelApplyDamage = false;

   return %apply && !%targetClient.DuelInvincible;
}

//-----------------------------------------------------------------------------
// Game type (these supersede DefaultGame)
//-----------------------------------------------------------------------------

function DuelModGame::activatePackages(%game)
{
   // The ruleset is fixed for the whole map; changing it restarts the map
   if($Duel_Ruleset !$= "MA")
      $Duel_Ruleset = "Duel";
   $Duel_RulesetActive = $Duel_Ruleset;
   $Duel_AIDisabled = false;
   Duel_applyBanList();

   DefaultGame::activatePackages(%game);
}

function DuelModGame::deactivatePackages(%game)
{
   // Decide how players arrive on the next map. A lastTeam above any team count makes
   // serverSetClientTeamState() auto-assign and spawn them; 0 brings them in as observers.
   for(%i = 0; %i < ClientGroup.getCount(); %i++)
   {
      %cl = ClientGroup.getObject(%i);

      // Bots: AIConnection::startMission moves bots to lastTeam, so leave it unset
      if(%cl.isAIControlled())
         %cl.lastTeam = "";
      else if($Duel_AutoJoinAfterMap && !%cl.isWatchOnly)
         %cl.lastTeam = 32;
      else
         %cl.lastTeam = 0;
   }

   DefaultGame::deactivatePackages(%game);
}

function DuelModGame::setUpTeams(%game)
{
   DefaultGame::setUpTeams(%game);
   %game.numTeams = 1;
   setSensorGroupCount(32);

   $teamDamage = 1;
}

function DuelModGame::allowsProtectedStatics(%game)
{
   return true;
}

function DuelModGame::initGameVars(%game)
{
   DefaultGame::initGameVars(%game);
   %game.IlluminationSchedule = %game.schedule($Duel_IlluminationInterval, "CheckPlayerIllumination");
   %game.DistanceDrawSchedule = %game.schedule($Duel_DistanceDrawInterval, "CheckDistanceDraw");
   %game.AmmoDrawSchedule = %game.schedule($Duel_AmmoDrawInterval, "CheckAmmoDraw");
}

function DuelModGame::missionLoadDone(%game)
{
   DefaultGame::missionLoadDone(%game);
   Duel_deleteNonDuelObjects();
   Duel_initSpawnArea();
}

function DuelModGame::clientMissionDropReady(%game, %client)
{
   messageClient(%client, 'MsgClientReady', "", BountyGame);  // Borrow the Bounty objective HUD
   messageClient(%client, 'MsgYourScoreIs', "", 0);
   messageClient(%client, 'MsgMissionDropInfo', '\c0You are in mission %1 (%2).', $MissionDisplayName, $MissionTypeDisplayName, $ServerName);
   %game.InitClientVars(%client);

   // Everyone starts as a free-flying observer; you join by dueling
   if(isObject(%client.camera) && %client.camera.mode !$= "observerFly")
      %client.camera.getDataBlock().setMode(%client.camera, "observerFly");

   DefaultGame::clientMissionDropReady(%game, %client);
}

function DuelModGame::clientJoinTeam(%game, %client, %team, %respawn)
{
}

// Only duel partners get a team (and sensor group). Everyone else stays an observer.
function DuelModGame::assignClientTeam(%game, %client)
{
   if(%client.clientDuelingWith == 0)
      return;

   for(%i = 1; %i < 32; %i++)
      %used[%i] = false;

   %maxSensorGroup = 0;
   %count = ClientGroup.getCount();
   for(%i = 0; %i < %count; %i++)
   {
      %cl = ClientGroup.getObject(%i);
      if(%cl != %client)
      {
         %used[%cl.team] = true;
         if(%cl.team > %maxSensorGroup)
            %maxSensorGroup = %cl.team;
      }
   }

   // Look for an empty team
   for(%i = 1; %i < 32; %i++)
   {
      if(!%used[%i])
      {
         %client.team = %i;
         if(%client.team > %maxSensorGroup)
            %maxSensorGroup = %client.team;
         break;
      }
   }

   setTargetSkin(%client.target, %client.skin);
   updateCanListenState(%client);
   setSensorGroupCount(%maxSensorGroup + 1);

   // Joining a duel cancels the observer kick, as joining a team does elsewhere
   if(isEventPending(%client.okschedule))
      cancel(%client.okschedule);
}

function DuelModGame::spawnPlayer(%game, %client, %respawn)
{
   if(%client.clientDuelingWith == 0)
      return;

   if(%client.isAIControlled())
      onAIRespawn(%client);
   else
      DefaultGame::spawnPlayer(%game, %client, %respawn);
}

function DuelModGame::createPlayer(%game, %client, %spawnLoc, %respawn)
{
   // Spawn invincibility covers the countdown; swap it in only for this call
   %invincibleTime = $InvincibleTime;
   $InvincibleTime = Duel_invincibleTime();
   DefaultGame::createPlayer(%game, %client, %spawnLoc, %respawn);
   $InvincibleTime = %invincibleTime;

   %client.player.setRespawnCloakOff();
}

function DuelModGame::playerSpawned(%game, %player)
{
   commandToClient(%player.client, 'DisplayHuds');
   DefaultGame::playerSpawned(%game, %player);

   %player.clearInventory();
   buyFavorites(%player.client);
   %player.schedule(250, "selectWeaponSlot", 0);
   %player.setEnergyLevel(%player.getDatablock().maxEnergy);
}

function DuelModGame::pickPlayerSpawn(%game, %client, %respawn)
{
   if(%client.SpawnPoint != 0)
      return %client.SpawnPoint;
   else
      return "0 0 200";
}

function DuelModGame::leaveMissionArea(%game, %playerData, %player)
{
}

function DuelModGame::enterMissionArea(%game, %playerData, %player)
{
}

function DuelModGame::InitClientVars(%game, %client)
{
   %client.clientRequestedDuelWith = 0;
   %client.clientDuelRequestedBy = 0;
   %client.clientDuelingWith = 0;
   %client.JustDueled = 0;
   %client.WantsReDuel = false;
   %client.InCountdown = false;
   %client.DuelInvincible = false;
   %client.IsIlluminated = false;
   %client.MADuelApplyDamage = false;
   %client.team = 0;
   %client.Initialized = true;
   %game.resetScore(%client);
}

//-----------------------------------------------------------------------------
// Scoring
//-----------------------------------------------------------------------------

function DuelModGame::resetScore(%game, %client)
{
   %client.score = 0;
   %client.kills = 0;
   %client.deaths = 0;
}

function DuelModGame::recalcScore(%game, %client)
{
   if(%client.kills > 0)
      %client.score = %client.kills / (%client.deaths + %client.kills) * 100;
   else
      %client.score = 0;

   messageClient(%client, 'MsgYourScoreIs', "", Duel_formatScore(%client.score));
   %game.recalcTeamRanks(%client);
   %game.checkScoreLimit(%client);
}

function DuelModGame::updateKillScores(%game, %clVictim, %clKiller, %damageType, %implement)
{
   if(!%clVictim.HasWonDuel && !%clVictim.DrawDuel)
   {
      %winner = %clVictim.clientDuelingWith;
      %winner.HasWonDuel = true;
      %winner.kills++;
      %clVictim.deaths++;
      %game.recalcScore(%clVictim);
      %game.recalcScore(%winner);

      // Remove the long-range waypoints so they don't linger after the duel
      if(%clVictim.IsIlluminated || %winner.IsIlluminated)
         Duel_disilluminate(%clVictim, %winner);
   }
}

function DuelModGame::checkScoreLimit(%game, %client)
{
   %scoreLimit = MissionGroup.Duel_scoreLimit;
   if(%scoreLimit $= "")
      %scoreLimit = 1000;

   if(%client.score >= %scoreLimit)
      %game.scoreLimitReached();
}

function DuelModGame::timeLimitReached(%game)
{
   logEcho("game over (timelimit)");
   %game.gameOver();
   cycleMissions();
}

function DuelModGame::scoreLimitReached(%game)
{
   logEcho("game over (scorelimit)");
   %game.gameOver();
   cycleMissions();
}

function DuelModGame::gameOver(%game)
{
   DefaultGame::gameOver(%game);

   messageAll('MsgGameOver', "Match has ended.~wvoice/announcer/ann.gameover.wav");
   messageAll('MsgClearObjHud', "");

   for(%i = 0; %i < ClientGroup.getCount(); %i++)
   {
      %client = ClientGroup.getObject(%i);
      %game.resetScore(%client);
      %game.ResetDuelVariables(%client);
   }
}

// Map end: everyone goes to observer. Players are not killed (that would be scored and recorded
// as a duel result after dtStats has taken its end-of-map snapshot); any later death counts as a draw.
function DuelModGame::ResetDuelVariables(%game, %client)
{
   %game.InitClientVars(%client);
   %client.DrawDuel = true;
   cancel(%client.reduelTimer);
   cancel(%client.killTimer);

   %game.forceObserver(%client, "mapEnd");
}

function DuelModGame::updateScoreHud(%game, %client, %tag)
{
   messageClient(%client, 'SetScoreHudHeader', "", "");
   messageClient(%client, 'SetScoreHudSubheader', "", '<lmargin:10><just:left>PLAYER<lmargin:185><just:left>ACCURACY<lmargin:330><just:left>PLAYER<lmargin:505><just:left>ACCURACY');

   for(%i = 0; %i < ClientGroup.getCount(); %i++)
      ClientGroup.getObject(%i).WasDisplayed = false;

   // Duels in progress
   %index = 0;
   for(%i = 0; %i < ClientGroup.getCount(); %i++)
   {
      %col1Client = ClientGroup.getObject(%i);
      if((%col1Client.clientDuelingWith == 0) || (%col1Client.WasDisplayed) || !isObject(%col1Client.player))
         continue;

      %col1Style = (%col1Client == %client) ? "<color:dcdcdc>" : "<color:00dcdc>";
      %col2Client = %col1Client.clientDuelingWith;
      %col2Client.WasDisplayed = true;
      %col2Style = (%col2Client == %client) ? "<color:dcdcdc>" : "<color:00dcdc>";

      if(%client.team != 0)
      {
         messageClient(%client, 'SetLineHud', "", %tag, %index, '%5<lmargin:10><just:left><clip:180>%1</clip><lmargin:200><just:left>%2<rmargin:305><just:right><color:ff0000>versus<rmargin:570>%6<lmargin:330><just:left><clip:180>%3</clip><lmargin:515><just:left>%4',
            %col1Client.name, Duel_formatScore(%col1Client.score), %col2Client.name, Duel_formatScore(%col2Client.score),
            %col1Style, %col2Style);
      }
      else // observers get links so they can observe the duelers
      {
         messageClient(%client, 'SetLineHud', "", %tag, %index, '%5<lmargin:10><just:left><clip:180><a:gamelink\t1%7>%1</a></clip><lmargin:200><just:left>%2<rmargin:305><just:right><color:ff0000>versus<rmargin:570>%6<lmargin:330><just:left><a:gamelink\t1%8><clip:180>%3</clip></a><lmargin:515><just:left>%4',
            %col1Client.name, Duel_formatScore(%col1Client.score), %col2Client.name, Duel_formatScore(%col2Client.score),
            %col1Style, %col2Style, %col1Client, %col2Client);
      }
      %index++;
   }

   if(%index == 0)
   {
      messageClient(%client, 'SetLineHud', "", %tag, %index, "");
      %index++;
   }

   // Everyone, sorted by score
   for(%i = 0; %i < ClientGroup.getCount(); %i++)
      ClientGroup.getObject(%i).sorted = false;

   %clientSortCount = 0;
   while(true)
   {
      %maxScore = -1;
      for(%i = 0; %i < ClientGroup.getCount(); %i++)
      {
         %cl = ClientGroup.getObject(%i);
         if(!%cl.sorted && %cl.score > %maxScore)
         {
            %clientSort[%clientSortCount] = %cl;
            %maxScore = %cl.score;
         }
      }

      if(%maxScore == -1)
         break;

      %clientSort[%clientSortCount].sorted = true;
      %clientSortCount++;
   }

   messageClient(%client, 'SetLineHud', "", %tag, %index, "<just:center>-----------------------------------------------------------------------------------------------------------------");
   %index++;
   messageClient(%client, 'SetLineHud', "", %tag, %index, '<tab:10, 310><font:Univers Condensed:22>\tClick on a name to duel<rmargin:315><just:right>KILLS<rmargin:390>DEATHS<rmargin:440><just:right>ACC<rmargin:510><just:right>RANK<rmargin:570><just:right>TIME');
   %index++;

   for(%i = 0; %i < %clientSortCount; %i++)
   {
      %cl = %clientSort[%i];

      %clientTimeStr = "";
      if(%cl.observerStartTime != 0)
         %clientTimeStr = %game.formatTime(getSimTime() - %cl.observerStartTime, false);

      if((%cl != %client) && (%cl.team == 0) && (%client.team == 0) && %cl.Initialized)
         messageClient(%client, 'SetLineHud', "", %tag, %index, '<tab:20, 310>\t<clip:150><a:gamelink\t1%3>%1</a></clip><just:right><rmargin:310><color:ffff00>%6<rmargin:370>%7<rmargin:450><color:ffffff>%5<color:00dcdc><rmargin:510>%4<rmargin:570>%2',
            %cl.name, %clientTimeStr, %cl, "none", Duel_formatScore(%cl.score), %cl.kills, %cl.deaths);
      else
         messageClient(%client, 'SetLineHud', "", %tag, %index, '<tab:20, 310>\t<clip:150><color:0000dc>%1</a></clip><color:00dcdc><just:right><rmargin:310><color:ffff00>%6<rmargin:370>%7<rmargin:450><color:ffffff>%5<color:00dcdc><rmargin:510>%4<rmargin:570>%2',
            %cl.name, %clientTimeStr, %cl, "none", Duel_formatScore(%cl.score), %cl.kills, %cl.deaths);
      %index++;
   }

   messageClient(%client, 'SetLineHud', "", %tag, %index, "");
   %index++;

   if(%client.clientRequestedDuelWith != 0)
   {
      messageClient(%client, 'SetLineHud', "", %tag, %index, 'Your duel request with %1 is pending (<a:gamelink\t2>CANCEL</a>).', %client.clientRequestedDuelWith.name);
      %index++;
   }

   if(%client.clientDuelRequestedBy != 0)
   {
      messageClient(%client, 'SetLineHud', "", %tag, %index, 'Player %1 has requested a duel (<a:gamelink\t3>ACCEPT</a>, <a:gamelink\t4>DECLINE</a>).', %client.clientDuelRequestedBy.name);
      %index++;
   }

   // Clear the rest of the HUD so old lines don't hang around. The index is returned so dtStats can
   // add its [View Server Stats] link below.
   messageClient(%client, 'ClearHud', "", %tag, %index);
   return %index;
}

function DuelModGame::processGameLink(%game, %client, %arg1, %arg2, %arg3, %arg4, %arg5)
{
   switch(getSubStr(%arg1, 0, 1))
   {
      case 1:
         %targetClient = getSubStr(%arg1, 1, strlen(%arg1) - 1);

         if((%targetClient.team != 0) || (%client.team != 0))
         {
            serverCmdHideHud(%client, 'scoreScreen');
            commandToClient(%client, 'setHudMode', 'Standard', "", 0);
            DefaultGame::processGameLink(%game, %client, %targetClient, %arg2, %arg3, %arg4, %arg5);
         }
         else
            %game.RequestDuel(%client, %targetClient);
      case 2:
         %game.CancelDuelOffer(%client);
         serverCmdHideHud(%client, 'scoreScreen');
         commandToClient(%client, 'setHudMode', 'Standard', "", 0);
      case 3:
         %game.AcceptDuelOffer(%client.clientDuelRequestedBy);
         serverCmdHideHud(%client, 'scoreScreen');
         commandToClient(%client, 'setHudMode', 'Standard', "", 0);
      case 4:
         %game.DeclineDuelOffer(%client.clientDuelRequestedBy);
         serverCmdHideHud(%client, 'scoreScreen');
         commandToClient(%client, 'setHudMode', 'Standard', "", 0);
   }
}

//-----------------------------------------------------------------------------
// Death and observer mode
//-----------------------------------------------------------------------------

function DuelModGame::onClientKilled(%game, %clVictim, %clKiller, %damageType, %implement, %damageLocation)
{
   %clVictim.player.setMomentumVector(%clVictim.lastMomVec);
   %clVictim.player.blowup();
   DefaultGame::onClientKilled(%game, %clVictim, %clKiller, %damageType, %implement, %damageLocation);

   if(%clVictim.respawnTimer)
      cancel(%clVictim.respawnTimer);

   if(%clVictim.hopSchedule)
   {
      cancel(%clVictim.hopSchedule);
      %clVictim.hopSchedule = "";
   }

   if(%clVictim.hopMonitor)
   {
      cancel(%clVictim.hopMonitor);
      %clVictim.hopMonitor = "";
   }

   if(%clVictim.clientDuelingWith != 0)
      %clVictim.clientDuelingWith.clientDuelingWith = 0;
   %clVictim.clientRequestedDuelWith = 0;
   %clVictim.clientDuelRequestedBy = 0;

   if(!%clVictim.DrawDuel)
   {
      if(!%clVictim.HasWonDuel)
      {
         messageAll('MsgClientJoinTeam', '\c2%1 has won the duel over %2.', %clVictim.clientDuelingWith.name, %clVictim.name);
         %clVictim.clientDuelingWith.player.setInvincible(1);
         %clVictim.clientDuelingWith.killTimer = %game.schedule($Duel_VictorKillTime, "DuelScriptKill", %clVictim.clientDuelingWith);
         %clVictim.clientDuelingWith = 0;
         messageClient(%clVictim, 'msgBountyTargetIs', "", "<none>");
         messageClient(%clKiller, 'msgBountyTargetIs', "", "<none>");
         if(Duel_allowRequest())
            bottomPrint(%clVictim, "\nPress VOTE YES key to request re-duel with " @ getTaggedString(%clVictim.JustDueled.name) @ ".", ($Duel_VictorKillTime + $Duel_VictorForceObserverTime) / 1000, 3);
         %clVictim.reduelTimer = %game.schedule($Duel_VictorKillTime + $Duel_VictorForceObserverTime, "forceObserver", %clVictim, "playerChoose");
      }
      else
      {
         if(!%clVictim.JustDueled.WantsReDuel)
            bottomPrint(%clVictim, "\nPress VOTE YES key to request re-duel with " @ getTaggedString(%clVictim.JustDueled.name) @ ".", $Duel_VictorForceObserverTime / 1000, 3);

         %clVictim.reduelTimer = %game.schedule($Duel_VictorForceObserverTime, "forceObserver", %clVictim, "playerChoose");
      }
   }
}

// "playerChoose": back to observer after a duel (starts the observer kick timer, as TacoServer
// does when a player becomes an observer). "mapEnd": the map is over (no kick timer).
function DuelModGame::forceObserver(%game, %client, %reason)
{
   if(%client <= 0)
      return;

   // Forced out of a duel (e.g. admin "force observer"): end the duel as a draw for both players
   if(%reason !$= "playerChoose" && %reason !$= "mapEnd" && %client.clientDuelingWith != 0)
   {
      %game.DrawDuel(%client, "The duel ended because " @ getTaggedString(%client.name) @ " was moved to observer.");
      cancel(%client.reduelTimer);
   }

   %game.removeFromTeamRankArray(%client);

   %client.lastObserverSpawn = -1;
   %client.observerStartTime = getSimTime();
   %client.camera.getDataBlock().setMode(%client.camera, "observerFly");

   %client.team = 0;
   %client.player.team = 0;
   setTargetSensorGroup(%client.target, %client.team);
   %client.setSensorGroup(%client.team);

   %client.setControlObject(%client.camera);
   commandToClient(%client, 'setHudMode', 'Observer');

   if(%reason $= "playerChoose" && !%client.isAdmin && !%client.isWatchOnly && !%client.isAIControlled() &&
      !$Host::TournamentMode && $Host::KickObserverTimeout)
   {
      if(isEventPending(%client.okschedule))
         cancel(%client.okschedule);

      %minutes = $Host::KickObserverTimeout / 60;
      messageClient(%client, 'MsgNoObservers', '\c2You have %1 minutes to start a duel or you will be kicked.', %minutes);

      %key = mFloor(getRandom() * 1000);
      %client.okkey = %key;
      %client.okschedule = schedule(($Host::KickObserverTimeout * 1000), 0, "cmdAutoKickObserver", %client, %key);
   }

   updateObserverFlyHud(%client);
   updateCanListenState(%client);

   %client.JustDueled = 0;
   %game.onClientEnterObserverMode(%client);
}

function DuelModGame::DuelScriptKill(%game, %client)
{
   // Duel invincibility (countdown and start grace period) would block the kill
   %client.DuelInvincible = false;
   %client.InCountdown = false;
   if(isObject(%client.player))
      %client.player.scriptKill(0);
}

function DuelModGame::onClientLeaveGame(%game, %client)
{
   // Let the parent's scriptKill end a duel that is still in its countdown
   %client.DuelInvincible = false;
   %client.InCountdown = false;

   cancel(%client.DuelOfferExpireThread);
   cancel(%client.reduelTimer);

   // Clear duel offers and re-duel links that point at this client
   for(%i = 0; %i < ClientGroup.getCount(); %i++)
   {
      %cl = ClientGroup.getObject(%i);
      if(%cl == %client)
         continue;

      if(%cl.clientDuelRequestedBy == %client)
      {
         %cl.clientDuelRequestedBy = 0;
         clearBottomPrint(%cl);
      }
      if(%cl.clientRequestedDuelWith == %client)
      {
         cancel(%cl.DuelOfferExpireThread);
         %cl.DuelOfferExpireThread = 0;
         %cl.clientRequestedDuelWith = 0;
      }
      if(%cl.JustDueled == %client)
         %cl.JustDueled = 0;
   }

   DefaultGame::onClientLeaveGame(%game, %client);
}

//-----------------------------------------------------------------------------
// Duel requests
//-----------------------------------------------------------------------------

function DuelModGame::RequestDuel(%game, %clientRequesting, %clientRequested)
{
   serverCmdHideHud(%clientRequesting, 'scoreScreen');
   commandToClient(%clientRequesting, 'setHudMode', 'Standard', "", 0);

   if(!Duel_allowRequest(%clientRequesting, %clientRequested))
      return;

   // Cross-request: both asked each other
   if(%clientRequested.clientRequestedDuelWith == %clientRequesting)
   {
      %game.AcceptDuelOffer(%clientRequested);
      return;
   }

   if(%clientRequesting.clientRequestedDuelWith != 0)
      messageClient(%clientRequesting, 'LeaveMissionArea', '\c2Your duel request to player %1 is still pending...', %clientRequesting.clientRequestedDuelWith.name);
   else if(%clientRequested.clientDuelRequestedBy != 0)
      messageClient(%clientRequesting, 'LeaveMissionArea', '\c2Player %1 is currently considering someone elses duel request.', %clientRequested.name);
   else
   {
      %clientRequesting.clientRequestedDuelWith = %clientRequested;
      %clientRequested.clientDuelRequestedBy = %clientRequesting;
      %clientRequesting.DuelOfferExpireThread = %game.schedule($Duel_OfferExpirationTime, "DuelOfferExpired", %clientRequesting);

      messageClient(%clientRequested, 'LeaveMissionArea', '\c2Player %1 wants to duel with you.  Press VOTE YES/NO to respond.~wfx/misc/warning_beep.wav', %clientRequesting.name);
      clearBottomPrint(%clientRequested);
      bottomPrint(%clientRequested, "<color:00dcdc>Player <color:ffffff>" @ getTaggedString(%clientRequesting.name) @ " <color:00dcdc>wants to duel with you.\n\nPress VOTE YES to accept, VOTE NO to decline.", 0, 3);

      serverCmdHideHud(%clientRequested, 'scoreScreen');
      commandToClient(%clientRequested, 'setHudMode', 'Standard', "", 0);

      messageClient(%clientRequesting, 'LeaveMissionArea', '\c2Your duel request has been sent to player %1.', %clientRequested.name);
   }

   if(%clientRequested.isAIControlled())
   {
      Duel_equipBotAfterPlayer(%clientRequested, %clientRequesting);
      %game.AcceptDuelOffer(%clientRequesting);
   }
}

function DuelModGame::AcceptDuelOffer(%game, %clientRequesting)
{
   if(%clientRequesting.clientRequestedDuelWith == 0)
      return;

   messageClient(%clientRequesting, 'LeaveMissionArea', '\c2Player %1 has accepted your duel challenge.~wfx/misc/warning_beep.wav', %clientRequesting.clientRequestedDuelWith.name);

   if(%clientRequesting.DuelOfferExpireThread != 0)
   {
      cancel(%clientRequesting.DuelOfferExpireThread);
      %clientRequesting.DuelOfferExpireThread = 0;
   }

   // Cancel the partner's own outstanding offer and decline any offer made to this player
   %game.CancelDuelOffer(%clientRequesting.clientRequestedDuelWith);
   %game.DeclineDuelOffer(%clientRequesting.clientDuelRequestedBy);

   %game.SetUpDuel(%clientRequesting, %clientRequesting.clientRequestedDuelWith);
   %game.CountDownDuel(%clientRequesting, %clientRequesting.clientDuelingWith, 5);
}

function DuelModGame::DeclineDuelOffer(%game, %clientRequesting)
{
   if(%clientRequesting.clientRequestedDuelWith == 0)
      return;

   messageClient(%clientRequesting, 'LeaveMissionArea', '\c2Player %1 has rejected your duel challenge.', %clientRequesting.clientRequestedDuelWith.name);
   messageClient(%clientRequesting.clientRequestedDuelWith, 'LeaveMissionArea', '\c2You have rejected the duel challenge from player %1.', %clientRequesting.name);
   clearBottomPrint(%clientRequesting.clientRequestedDuelWith);
   %clientRequesting.clientRequestedDuelWith.clientDuelRequestedBy = 0;
   %clientRequesting.clientRequestedDuelWith = 0;

   if(%clientRequesting.DuelOfferExpireThread != 0)
   {
      cancel(%clientRequesting.DuelOfferExpireThread);
      %clientRequesting.DuelOfferExpireThread = 0;
   }
}

function DuelModGame::CancelDuelOffer(%game, %clientRequesting)
{
   if(%clientRequesting.clientRequestedDuelWith == 0)
      return;

   messageClient(%clientRequesting.clientRequestedDuelWith, 'LeaveMissionArea', '\c2Player %1 cancelled their duel offer.', %clientRequesting.name);
   clearBottomPrint(%clientRequesting.clientRequestedDuelWith);
   %clientRequesting.clientRequestedDuelWith.clientDuelRequestedBy = 0;
   %clientRequesting.clientRequestedDuelWith = 0;

   if(%clientRequesting.DuelOfferExpireThread != 0)
   {
      cancel(%clientRequesting.DuelOfferExpireThread);
      %clientRequesting.DuelOfferExpireThread = 0;
   }
}

function DuelModGame::DuelOfferExpired(%game, %clientRequesting)
{
   messageClient(%clientRequesting, 'LeaveMissionArea', '\c2Player %1 has not accepted your duel challenge.', %clientRequesting.clientRequestedDuelWith.name);
   %game.CancelDuelOffer(%clientRequesting);
}

//-----------------------------------------------------------------------------
// Running a duel
//-----------------------------------------------------------------------------

function DuelModGame::SetUpDuel(%game, %clientChallenger, %clientDefender)
{
   %clientDefender.clientDuelingWith = %clientChallenger;
   %clientChallenger.clientDuelingWith = %clientDefender;
   %clientChallenger.clientRequestedDuelWith = 0;
   %clientDefender.clientDuelRequestedBy = 0;
   %clientDefender.HasWonDuel = false;
   %clientChallenger.HasWonDuel = false;
   %clientDefender.DrawDuel = false;
   %clientChallenger.DrawDuel = false;
   %clientChallenger.JustDueled = %clientDefender;
   %clientDefender.JustDueled = %clientChallenger;
   %clientDefender.WantsReDuel = false;
   %clientChallenger.WantsReDuel = false;
   %clientDefender.DuelInvincible = true;
   %clientChallenger.DuelInvincible = true;
   %clientDefender.IsIlluminated = false;
   %clientChallenger.IsIlluminated = false;
   %clientDefender.MADuelApplyDamage = false;
   %clientChallenger.MADuelApplyDamage = false;

   Duel_setDuelMode(%clientDefender);
   Duel_setDuelMode(%clientChallenger);

   Duel_pickSpawnPoints(%clientDefender, %clientChallenger);
   messageAll('MsgClientJoinTeam', '\c2Players %1 and %2 are now dueling.', %clientDefender.name, %clientChallenger.name);

   %game.assignClientTeam(%clientDefender);
   %game.spawnPlayer(%clientDefender, true);
   %clientDefender.camera.setFlyMode();
   if(!%clientDefender.isAIControlled())
      %clientDefender.setControlObject(%clientDefender.player);
   %clientDefender.player.disableMove(1);
   %clientDefender.InCountdown = true;
   clearBottomPrint(%clientDefender);
   messageClient(%clientDefender, 'msgBountyTargetIs', "", %clientChallenger.name);

   if(%clientChallenger != %clientDefender)
   {
      %game.assignClientTeam(%clientChallenger);
      %game.spawnPlayer(%clientChallenger, true);
      %clientChallenger.camera.setFlyMode();
      %clientChallenger.setControlObject(%clientChallenger.player);
      %clientChallenger.player.disableMove(1);
      %clientChallenger.InCountdown = true;
      clearBottomPrint(%clientChallenger);
      messageClient(%clientChallenger, 'msgBountyTargetIs', "", %clientDefender.name);
   }

   setTargetSensorData(%clientDefender.target, DuelPlayerSensor);
   setTargetSensorData(%clientChallenger.target, DuelPlayerSensor);
   setSensorGroupAlwaysVisMask(%clientDefender.getSensorGroup(), 0);
   setSensorGroupAlwaysVisMask(%clientChallenger.getSensorGroup(), 0);

   %visMask = getSensorGroupAlwaysVisMask(%clientDefender.getSensorGroup());
   %visMask |= (1 << %clientChallenger.getSensorGroup());
   setSensorGroupAlwaysVisMask(%clientDefender.getSensorGroup(), %visMask);

   %visMask = getSensorGroupAlwaysVisMask(%clientChallenger.getSensorGroup());
   %visMask |= (1 << %clientDefender.getSensorGroup());
   setSensorGroupAlwaysVisMask(%clientChallenger.getSensorGroup(), %visMask);

   Duel_faceEachOther(%clientDefender, %clientChallenger);
}

function DuelModGame::CountDownDuel(%game, %clientChallenger, %clientDefender, %seconds)
{
   if(%seconds > 2)
   {
      messageClient(%clientChallenger, 'MsgMissionStart', '~wfx/misc/hunters_%1.wav', %seconds);
      messageClient(%clientDefender, 'MsgMissionStart', '~wfx/misc/hunters_%1.wav', %seconds);
      %game.schedule(1000, "CountDownDuel", %clientChallenger, %clientDefender, %seconds - 1);
   }
   else if(%seconds == 2)
   {
      messageClient(%clientChallenger, 'MsgMissionStart', '~wvoice/announcer/ann.match_begins.wav');
      messageClient(%clientDefender, 'MsgMissionStart', '~wvoice/announcer/ann.match_begins.wav');
      %game.schedule(2000, "CountDownDuel", %clientChallenger, %clientDefender, %seconds - 2);
   }
   else
      %game.StartDuel(%clientChallenger, %clientDefender);
}

function DuelModGame::StartDuel(%game, %clientChallenger, %clientDefender)
{
   Duel_applyAIMode(%clientDefender);

   %clientChallenger.player.disableMove(0);
   %clientChallenger.InCountdown = false;

   if(%clientDefender.isAIControlled())
   {
      %clientDefender.setControlObject(%clientDefender.player);
      %clientDefender.setEngageTarget(%clientChallenger);
      %clientDefender.setTargetObject(%clientChallenger.player);
   }

   %clientDefender.player.disableMove(0);
   %clientDefender.InCountdown = false;
   %game.schedule($Duel_StartInvincibleTime, "RemoveDuelInvincibility", %clientDefender);
   %game.schedule($Duel_StartInvincibleTime, "RemoveDuelInvincibility", %clientChallenger);
}

function DuelModGame::RemoveDuelInvincibility(%game, %client)
{
   %client.DuelInvincible = false;
}

function DuelModGame::DrawDuel(%game, %client1, %reason)
{
   if(%client1.clientDuelingWith == 0)
      return 0;

   %client2 = %client1.clientDuelingWith;
   messageAll('DuelResultDraw', '\c2Duel between %1 and %2 ends in a draw.', %client1.name, %client2.name);

   Duel_disilluminate(%client1, %client2);

   %client1.DrawDuel = true;
   %client1.clientDuelingWith = 0;
   %game.DuelScriptKill(%client1);
   %client1.reduelTimer = %game.schedule($Duel_VictorKillTime + $Duel_VictorForceObserverTime, "forceObserver", %client1, "playerChoose");
   bottomPrint(%client1, "\n<color:ffffff>" @ %reason, ($Duel_VictorKillTime + $Duel_VictorForceObserverTime) / 1000, 3);
   messageClient(%client1, 'msgBountyTargetIs', "", "<none>");

   %client2.DrawDuel = true;
   %client2.clientDuelingWith = 0;
   %game.DuelScriptKill(%client2);
   %client2.reduelTimer = %game.schedule($Duel_VictorKillTime + $Duel_VictorForceObserverTime, "forceObserver", %client2, "playerChoose");
   bottomPrint(%client2, "\n<color:ffffff>" @ %reason, ($Duel_VictorKillTime + $Duel_VictorForceObserverTime) / 1000, 3);
   messageClient(%client2, 'msgBountyTargetIs', "", "<none>");
}

function DuelModGame::CheckPlayerIllumination(%game)
{
   for(%i = 0; %i < ClientGroup.getCount(); %i++)
   {
      %cl1 = ClientGroup.getObject(%i);
      if(%cl1.clientDuelingWith == 0)
         continue;

      %cl2 = %cl1.clientDuelingWith;
      %distance = Duel_playerDistance(%cl1, %cl2);
      if(%cl1.IsIlluminated && %distance < $Duel_IlluminationDistance)
         Duel_disilluminate(%cl1, %cl2);
      else if(!%cl1.IsIlluminated && %distance > $Duel_IlluminationDistance)
         Duel_illuminate(%cl1, %cl2);
   }

   cancel(%game.IlluminationSchedule);
   %game.IlluminationSchedule = %game.schedule($Duel_IlluminationInterval, "CheckPlayerIllumination");
}

function DuelModGame::CheckDistanceDraw(%game)
{
   for(%i = 0; %i < ClientGroup.getCount(); %i++)
      ClientGroup.getObject(%i).DistanceDrawChecked = false;

   for(%i = 0; %i < ClientGroup.getCount(); %i++)
   {
      %cl1 = ClientGroup.getObject(%i);
      %cl2 = %cl1.clientDuelingWith;

      if((%cl2 != 0) && !%cl1.DistanceDrawChecked)
      {
         %cl2.DistanceDrawChecked = true;
         if(Duel_playerDistance(%cl1, %cl2) > $Duel_DistanceDrawDistance)
            %game.DrawDuel(%cl1, "Duel is a draw due to excessive distance between duel partners");
      }
   }

   cancel(%game.DistanceDrawSchedule);
   %game.DistanceDrawSchedule = %game.schedule($Duel_DistanceDrawInterval, "CheckDistanceDraw");
}

function DuelModGame::CheckAmmoDraw(%game)
{
   for(%i = 0; %i < ClientGroup.getCount(); %i++)
      ClientGroup.getObject(%i).AmmoDrawChecked = false;

   for(%i = 0; %i < ClientGroup.getCount(); %i++)
   {
      %cl1 = ClientGroup.getObject(%i);
      %cl2 = %cl1.clientDuelingWith;

      if((%cl2 != 0) && !%cl1.AmmoDrawChecked)
      {
         %cl2.AmmoDrawChecked = true;
         if(Duel_playerOutOfAmmo(%cl1) && Duel_playerOutOfAmmo(%cl2))
            %game.DrawDuel(%cl1, "Duel is a draw because both players have run out of ammo.");
      }
   }

   cancel(%game.AmmoDrawSchedule);
   %game.AmmoDrawSchedule = %game.schedule($Duel_AmmoDrawInterval, "CheckAmmoDraw");
}

function DuelModGame::DumpClientStates(%game)
{
   for(%i = 0; %i < ClientGroup.getCount(); %i++)
   {
      %cl = ClientGroup.getObject(%i);
      echo("Name: " @ getTaggedString(%cl.name));
      echo("  clientRequestedDuelWith: " @ getTaggedString(%cl.clientRequestedDuelWith.name));
      echo("  clientDuelRequestedBy: " @ getTaggedString(%cl.clientDuelRequestedBy.name));
      echo("  clientDuelingWith: " @ getTaggedString(%cl.clientDuelingWith.name));
      echo("  team: " @ %cl.team);
      echo("  InCountdown: " @ %cl.InCountdown);
      echo("  HasWonDuel: " @ %cl.HasWonDuel);
      echo(" ");
   }
}

//-----------------------------------------------------------------------------
// Bots
//-----------------------------------------------------------------------------

function DuelModGame::AIInit(%game)
{
   AIInit();
}

function DuelModGame::onAIRespawn(%game, %client)
{
   // Give the bot another object to control so it can't move until the duel starts
   %client.setControlObject(%client.clientDuelingWith.camera);

   // onAIRespawn() doesn't call createPlayer() with the respawn flag set
   %invincibleTime = Duel_invincibleTime();
   %player = %client.player;
   %player.isBot = true;
   %player.setInvincible(true);
   %player.setInvincibleMode(%invincibleTime, 0.02);
   %player.schedule(%invincibleTime * 1000, "setInvincible", false);

   if(%client.duelHoppingBots)
   {
      %client.hopSchedule = %player.schedule(5000, "performHop");
      %client.hopMonitor = %player.schedule(7000, "monitorHop");
   }

   if(!%client.defaultTasksAdded)
   {
      %client.defaultTasksAdded = true;
      %client.addTask(AIEngageTask);
      %client.addTask(AITauntCorpseTask);
      %client.addTask(AIPatrolTask);
   }

   %client.spawnUseInv = true;
}

function DuelModGame::onAIKilled(%game, %clVictim, %clKiller, %damageType, %implement)
{
   DefaultGame::onAIKilled(%game, %clVictim, %clKiller, %damageType, %implement);
   cancel(%clVictim.respawnThread);
}

//-----------------------------------------------------------------------------
// Votes (ruleset) and admin options (MA sub-mode)
//-----------------------------------------------------------------------------

function DuelModGame::sendGameVoteMenu(%game, %client, %key)
{
   DefaultGame::sendGameVoteMenu(%game, %client, %key);

   %isAdmin = (%client.isAdmin || %client.isSuperAdmin);
   if(!%client.canVote && !%isAdmin)
      return;

   if(%game.scheduleVote !$= "" || %client.ForceVote $= "skip_confirm+")
      return;

   if(!%isAdmin || (%isAdmin && %client.ForceVote))
   {
      if($Duel_RulesetActive $= "MA")
         messageClient(%client, 'MsgVoteItem', "", %key, 'VoteDuelRuleset', 'switch to the Duel ruleset', 'Vote for the Duel ruleset (restarts map)');
      else
         messageClient(%client, 'MsgVoteItem', "", %key, 'VoteDuelRuleset', 'switch to the MA ruleset', 'Vote for the MA ruleset (restarts map)');
   }
   else
   {
      if($Duel_RulesetActive $= "MA")
         messageClient(%client, 'MsgVoteItem', "", %key, 'VoteDuelRuleset', 'switch to the Duel ruleset', 'Switch to the Duel ruleset (restarts map)');
      else
         messageClient(%client, 'MsgVoteItem', "", %key, 'VoteDuelRuleset', 'switch to the MA ruleset', 'Switch to the MA ruleset (restarts map)');

      if($Duel_RulesetActive $= "MA")
         messageClient(%client, 'MsgVoteItem', "", %key, 'DuelMAMode', 'change the MA mode', "Next MA Mode (now: " @ getField($Duel_MAModes, $Duel_MAMode) @ ")");
   }
}

function DuelModGame::evalVote(%game, %typeName, %admin, %arg1, %arg2, %arg3, %arg4)
{
   switch$(%typeName)
   {
      case "VoteDuelRuleset":
         %game.voteDuelRuleset(%admin, %arg1, %arg2, %arg3, %arg4);
         return;
      case "DuelMAMode":
         if(isObject(%admin))
            %game.nextMAMode(%admin);
         return;
   }

   DefaultGame::evalVote(%game, %typeName, %admin, %arg1, %arg2, %arg3, %arg4);
}

// Kick votes are server-wide in DuelMod (every dueler is alone on their team, see VoteMenu.cs),
// so count every vote rather than only the target's team.
function DuelModGame::voteKickPlayer(%game, %admin, %client)
{
   if(!isObject(%admin))
   {
      %game.votesFor[%game.kickTeam] = %game.totalVotesFor;
      %game.votesAgainst[%game.kickTeam] = %game.totalVotesAgainst;
   }

   DefaultGame::voteKickPlayer(%game, %admin, %client);
}

function DuelModGame::voteDuelRuleset(%game, %admin, %arg1, %arg2, %arg3, %arg4)
{
   %newRuleset = ($Duel_RulesetActive $= "MA") ? "Duel" : "MA";

   if(isObject(%admin))
   {
      messageAll('MsgAdminForce', '\c2The Admin %1 has switched to the %2 ruleset. Restarting the map...', %admin.name, %newRuleset);
      adminLog(%admin, " has switched DuelMod to the " @ %newRuleset @ " ruleset.");
   }
   else
   {
      %totalVotes = %game.totalVotesFor + %game.totalVotesAgainst;
      %voters = ClientGroup.getCount() - $HostGameBotCount - %game.totalVotesNone;
      if(%totalVotes > 0 && %voters > 0 && (%game.totalVotesFor / %voters) > ($Host::VotePasspercent / 100))
         messageAll('MsgVotePassed', '\c2The %1 ruleset was chosen by vote. Restarting the map...', %newRuleset);
      else
      {
         messageAll('MsgVoteFailed', '\c2Ruleset vote did not pass: %1 percent.', mFloor(%voters > 0 ? (%game.totalVotesFor / %voters * 100) : 0));
         return;
      }
   }

   $Duel_Ruleset = %newRuleset;
   $voteNext = 0;
   %game.gameOver();
   loadMission($CurrentMission, $CurrentMissionType, false);
}

function DuelModGame::nextMAMode(%game, %admin)
{
   if($Duel_RulesetActive !$= "MA")
   {
      messageClient(%admin, 'MsgClient', "\c2MA modes are only available with the MA ruleset.");
      return;
   }

   $Duel_MAMode++;
   if($Duel_MAMode >= getFieldCount($Duel_MAModes))
      $Duel_MAMode = 0;

   // Takes effect when the next duel starts
   %mode = getField($Duel_MAModes, $Duel_MAMode);
   centerPrintAll("\nMA mode changed to " @ %mode @ " (from the next duel)", 3, 3);
   adminLog(%admin, " has changed the MA mode to " @ %mode @ ".");
}

//-----------------------------------------------------------------------------
// Helpers
//-----------------------------------------------------------------------------

// Spawn invincibility for duel players: the 5 second countdown plus the start grace period
function Duel_invincibleTime()
{
   return 5 + $Duel_StartInvincibleTime / 1000;
}

function Duel_formatScore(%score)
{
   if(getSubStr(%score, strlen(%score) - 1, 1) $= "%")
      return %score;
   else
      return getSubStr((mFloor(%score * 10) / 10) + 0.0001, 0, 4) @ "%";
}

function Duel_allowRequest(%clientChallenger, %clientDefender)
{
   %secondsLeft = ($Host::TimeLimit * 60 * 1000 + $missionStartTime - getSimTime()) / 1000;

   if(%secondsLeft < $Duel_RequestAllowTime)
   {
      if(%clientChallenger != 0)
         messageClient(%clientChallenger, 'DuelRequestNotAllowed', '\c2Duel requesting is not allowed now.  You have to wait for mission change.');
      return false;
   }

   return true;
}

function Duel_playerOutOfAmmo(%client)
{
   if(!isObject(%client.player))
      return true;

   for(%i = 0; %i < $numAmmoItems - 1; %i++)
      if(%client.player.getInventory($AmmoItem[%i]) > 0)
         return false;

   return (%client.player.getInventory("SniperRifle") == 0) && (%client.player.getInventory("Grenade") == 0);
}

function Duel_equipBotAfterPlayer(%clientBot, %clientPlayer)
{
   for(%i = 0; %i < 11; %i++)
      %clientBot.favorites[%i] = %clientPlayer.favorites[%i];

   %clientBot.weaponIndex = %clientPlayer.weaponIndex;
   %clientBot.packIndex = %clientPlayer.packIndex;
   %clientBot.grenadeIndex = %clientPlayer.grenadeIndex;
   %clientBot.mineIndex = %clientPlayer.mineIndex;
}

// Height of a player above the terrain
function Duel_playerHeight(%player)
{
   %posFrom = getWords(%player.getTransform(), 0, 2);
   %posTo = VectorAdd(%posFrom, "0 0 -10000");
   %hit = containerRayCast(%posFrom, %posTo, $TypeMasks::TerrainObjectType, 0);
   if(%hit $= "" || %hit == 0)
      return 0;
   return VectorDist(%posFrom, getWords(%hit, 1, 3));
}

function Duel_playerDistance(%client1, %client2)
{
   return VectorDist(%client1.player.getTransform(), %client2.player.getTransform());
}

// Show each partner a waypoint on the other
function Duel_illuminate(%client1, %client2)
{
   %client2.setTargetId(%client1.target);
   %client2.sendTargetTo(%client2, true);
   %client1.setTargetId(%client2.target);
   %client1.sendTargetTo(%client1, true);

   %client1.IsIlluminated = true;
   %client2.IsIlluminated = true;
}

function Duel_disilluminate(%client1, %client2)
{
   removeClientTargetType(%client1, "AssignedTask");
   removeClientTargetType(%client2, "AssignedTask");

   %client1.IsIlluminated = false;
   %client2.IsIlluminated = false;
}

//-----------------------------------------------------------------------------
// Spawning
//-----------------------------------------------------------------------------

// MissionArea.area is "x y width height". Spawns stay 2 * $Duel_SpawnRadius inside it.
function Duel_initSpawnArea()
{
   %area = "-1024 -1024 2048 2048";
   for(%i = 0; %i < MissionGroup.getCount(); %i++)
   {
      %obj = MissionGroup.getObject(%i);
      if(%obj.getClassName() $= "MissionArea")
      {
         %area = %obj.area;
         break;
      }
   }

   %margin = 2 * $Duel_SpawnRadius;
   $Duel_SpawnMinX = getWord(%area, 0) + %margin;
   $Duel_SpawnMinY = getWord(%area, 1) + %margin;
   $Duel_SpawnWidth = getWord(%area, 2) - 2 * %margin;
   $Duel_SpawnHeight = getWord(%area, 3) - 2 * %margin;

   // Very small mission areas: use the whole area
   if($Duel_SpawnWidth <= 0 || $Duel_SpawnHeight <= 0)
   {
      $Duel_SpawnMinX = getWord(%area, 0);
      $Duel_SpawnMinY = getWord(%area, 1);
      $Duel_SpawnWidth = getWord(%area, 2);
      $Duel_SpawnHeight = getWord(%area, 3);
   }
}

function Duel_terrainAngleAtPoint(%pos)
{
   %pos1 = VectorAdd(%pos, "0 0 1000");
   %pos2 = VectorAdd(%pos, "0 0 -1000");

   %hit = containerRayCast(%pos1, %pos2, $TypeMasks::TerrainObjectType, 0);
   return getTerrainAngle(getWords(%hit, 4, 6));
}

function Duel_pickSpawnPoints(%client1, %client2)
{
   for(%attempt1 = 0; %attempt1 < 50; %attempt1++)
   {
      for(%attempt2 = 0; %attempt2 < 200; %attempt2++)
      {
         %client1.SpawnPoint = Duel_findRandomSpawnPoint();
         %spawnAngle = Duel_terrainAngleAtPoint(%client1.SpawnPoint);
         if((%spawnAngle >= $Duel_MinSpawnAngle) && (%spawnAngle <= $Duel_MaxSpawnAngle))
            break;
      }

      if(%attempt2 == 200)
         break;

      for(%attempt3 = 0; %attempt3 < 50; %attempt3++)
      {
         %client2.SpawnPoint = Duel_findNearbySpawnPoint(%client1.SpawnPoint, $Duel_SpawnRadius);
         %spawnAngle = Duel_terrainAngleAtPoint(%client2.SpawnPoint);
         if((%spawnAngle >= $Duel_MinSpawnAngle) && (%spawnAngle <= $Duel_MaxSpawnAngle))
            if(mAbs(getWord(VectorSub(%client1.SpawnPoint, %client2.SpawnPoint), 2)) < $Duel_MaxElevationDiff)
               break;
      }

      if(%attempt3 != 50)
         break;
   }

   if((%attempt1 == 50) || (%attempt2 == 200) || (%attempt3 == 50))
   {
      %client1.SpawnPoint = "10 0 200";
      %client2.SpawnPoint = "-10 0 200";
   }
}

function Duel_spawnBlockMask()
{
   return $TypeMasks::VehicleObjectType     | $TypeMasks::MoveableObjectType   |
          $TypeMasks::StaticShapeObjectType | $TypeMasks::StaticTSObjectType   |
          $TypeMasks::ForceFieldObjectType  | $TypeMasks::ItemObjectType       |
          $TypeMasks::PlayerObjectType      | $TypeMasks::TurretObjectType     |
          $TypeMasks::InteriorObjectType    | $TypeMasks::WaterObjectType;
}

function Duel_findRandomSpawnPoint()
{
   %mask = Duel_spawnBlockMask();

   for(%attempts = 0; %attempts < 20; %attempts++)
   {
      %x = getRandom($Duel_SpawnWidth) + $Duel_SpawnMinX;
      %y = getRandom($Duel_SpawnHeight) + $Duel_SpawnMinY;
      %z = getTerrainHeight(%x @ " " @ %y);
      %loc = %x @ " " @ %y @ " " @ %z;

      InitContainerRadiusSearch(%loc, 20, %mask);
      if(ContainerSearchNext() == 0)
      {
         InitContainerRadiusSearch(%loc, 20, $TypeMasks::PlayerObjectType);
         if(ContainerSearchNext() == 0)
            return %loc;
      }
   }

   return "0 0 0";
}

function Duel_findNearbySpawnPoint(%loc, %distance)
{
   %mask = Duel_spawnBlockMask();

   for(%attempts = 0; %attempts < 20; %attempts++)
   {
      %angle = 3.14159265 * getRandom(360) / 180;
      %x = getWord(%loc, 0) + %distance * mCos(%angle);
      %y = getWord(%loc, 1) + %distance * mSin(%angle);
      %z = getTerrainHeight(%x @ " " @ %y);
      %newLoc = %x @ " " @ %y @ " " @ %z;

      InitContainerRadiusSearch(%newLoc, 2, %mask);
      if(ContainerSearchNext() == 0 && Duel_checkLocLOS(%loc, %newLoc))
         return %newLoc;
   }

   return "0 0 0";
}

function Duel_checkLocLOS(%loc1, %loc2)
{
   %loc1 = VectorAdd(%loc1, "0 0 1");
   %loc2 = VectorAdd(%loc2, "0 0 1");

   return containerRayCast(%loc1, %loc2, $TypeMasks::TerrainObjectType, 0) == 0;
}

function Duel_faceEachOther(%client1, %client2)
{
   %pi = 3.14159265;
   %loc1 = getWords(%client1.player.getTransform(), 0, 2);
   %loc2 = getWords(%client2.player.getTransform(), 0, 2);

   %loc1xy = setWord(%loc1, 2, 0);
   %loc2xy = setWord(%loc2, 2, 0);

   if(VectorDist(%loc1xy, %loc2xy) == 0)
      return;

   %vec12 = VectorNormalize(VectorSub(%loc1xy, %loc2xy));
   %angle = mAcos(getWord(%vec12, 1));

   if(getWord(%vec12, 0) > 0)
   {
      %rot1 = " 0 0 1 " @ (%angle + %pi);
      %rot2 = " 0 0 1 " @ %angle;
   }
   else
   {
      %rot1 = " 0 0 1 " @ (%pi - %angle);
      %rot2 = " 0 0 1 " @ ((%pi - %angle) + %pi);
   }

   %client1.player.setTransform(%loc1 @ %rot1);
   %client2.player.setTransform(%loc2 @ %rot2);
}

//-----------------------------------------------------------------------------
// Map cleanup: remove objectives, stations, turrets, vehicles etc. from the map
//-----------------------------------------------------------------------------

function Duel_deleteNonDuelObjects()
{
   Duel_deleteObjectsFromGroupByType(MissionGroup, "Item");
   Duel_deleteObjectsFromGroupByType(MissionGroup, "Waypoint");
   Duel_deleteObjectsFromGroupByType(MissionGroup, "Turret");
   Duel_deleteObjectsFromGroupByType(MissionGroup, "StaticShape");
   Duel_deleteObjectsFromGroupByType(MissionGroup, "TSStatic");
   Duel_deleteObjectsFromGroupByType(MissionGroup, "ForceFieldBare");
   Duel_deleteObjectsFromGroupByType(MissionGroup, "FlyingVehicle");
   Duel_deleteObjectsFromGroupByType(MissionGroup, "WheeledVehicle");
}

function Duel_deleteObjectsFromGroupByType(%group, %type)
{
   for(%i = %group.getCount() - 1; %i >= 0; %i--)
   {
      %obj = %group.getObject(%i);

      if(%obj.getClassName() $= "SimGroup")
         Duel_deleteObjectsFromGroupByType(%obj, %type);
      else if(%obj.getClassName() $= %type)
         %obj.delete();
   }
}

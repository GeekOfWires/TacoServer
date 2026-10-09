// DuelMod support
//
// The only DuelMod code that is active outside DuelMod maps. Everything here acts only when the
// DuelMod game type is involved:
//  - Lists the DuelMod maps from prefs/DuelPrefs.cs in the vote menu, for both map list systems
//    (the rotation file, and dtStats' map list when an admin has enabled it).
//  - Keeps DuelMod out of tournament mode: hidden from the mission type list, and never loaded while
//    in tournament mode. Votes for it are refused in VoteMenu.cs.
//
// Executed by scripts/DuelModGame.cs, which CreateServer runs after every autoexec script and before
// the first mission, so this package sits above the TacoServer and dtStats packages it wraps (each
// override calls its parent) and below the per-mission game packages. It is deliberately not an
// autoexec script: their load order is not alphabetical, and dtStats' own file scans during the
// autoexec pass can make the engine skip the scripts that follow it.

package DuelModSupport
{

function buildMissionList()
{
   parent::buildMissionList();

   // dtStats' map list (ML) builds the vote list itself; DuelMod maps are injected into ML below
   if(isObject(ML) && ML.enable && $dtStats::MapStart)
      return;

   for(%i = 0; %i < $Duel_MapCount; %i++)
   {
      %map = $Duel_Map[%i];
      if(isFile("missions/" @ %map @ ".mis") && !Duel_isRotationMap(%map, "DuelMod"))
         addRotationMap(%map, "DuelMod", true, true);
   }
   getMissionTypeDisplayNames();
}

function dtBuildMissionList(%reset)
{
   parent::dtBuildMissionList(%reset);
   Duel_injectMapList();
}

// Hide DuelMod from the mission type list while in tournament mode (indices are unchanged)
function serverCmdGetMissionTypes(%client, %key)
{
   if(!$Host::TournamentMode)
   {
      parent::serverCmdGetMissionTypes(%client, %key);
      return;
   }

   for(%type = 0; %type < $HostTypeCount; %type++)
      if($HostTypeName[%type] !$= "DuelMod")
         messageClient(%client, 'MsgVoteItem', "", %key, %type, "", $HostTypeDisplayName[%type], true);
}

// Never load DuelMod in tournament mode while another map is running (e.g. a next mission set before
// the server went into tournament mode, or random mission type cycling): the current map is reloaded
// instead. A DuelMod map that loads anyway (server start) turns tournament mode off (MissionTypeOptions.cs).
function loadMission(%missionName, %missionType, %firstMission)
{
   if($Host::TournamentMode && %missionType $= "DuelMod" && $CurrentMission !$= "" && $CurrentMissionType !$= "DuelMod")
   {
      messageAll('MsgClient', '\c2Duel MOD is not available in Tournament Mode.');
      %missionName = $CurrentMission;
      %missionType = $CurrentMissionType;
   }

   parent::loadMission(%missionName, %missionType, %firstMission);
}

};

if(!isActivePackage(DuelModSupport))
   activatePackage(DuelModSupport);

// Is %map already listed under %gameType in the vote list?
function Duel_isRotationMap(%map, %gameType)
{
   for(%type = 0; %type < $HostTypeCount; %type++)
      if($HostTypeName[%type] $= %gameType)
         break;

   if(%type == $HostTypeCount)
      return false;

   for(%i = 0; %i < $HostMissionCount[%type]; %i++)
      if($HostMissionFile[$HostMission[%type, %i]] $= %map)
         return true;

   return false;
}

// Add the DuelMod maps to dtStats' map list (ML). ML is built from the "// MissionTypes =" line of
// every .mis file, and no map lists DuelMod there. New entries are enabled (voted for, and rotated
// through while a DuelMod map is running); once an entry exists, the settings an admin picks in the
// Map Config menu are left alone.
function Duel_injectMapList()
{
   if(!isObject(ML))
      return;

   %type = "duelmod";
   for(%i = 0; %i < ML.TypeCount; %i++)
      if(ML.TypeName[%i] $= %type)
         break;
   if(%i == ML.TypeCount)
   {
      ML.TypeCount++;
      ML.TypeName[%i] = %type;
      ML.TypeIndex[%type] = %i;
      $dtFixedMapCount[%type] = 0;
   }

   // enable 0 | voteOption 1 (0 = normal) | min 2 | max 3 | remaining fields: dtStats defaults
   %options = 1 TAB 0 TAB 0 TAB 64 TAB 3 TAB 0 TAB "1000000" TAB 0 TAB "1000000000000000000000000000000" TAB 0 TAB 12 TAB 60 TAB 28 TAB 12 TAB 2024 TAB 0 TAB 60 TAB 0;

   for(%m = 0; %m < $Duel_MapCount; %m++)
   {
      %file = $Duel_Map[%m];
      if(!isFile("missions/" @ %file @ ".mis"))
         continue;

      %name = cleanMapName(%file);
      if(isObject(%name) && %name.getGroup() == ML.getId())
         %mObj = %name.getId();
      else
      {
         // Not in ML (dtStats skips some types, e.g. TR2)
         %mObj = new ScriptObject(%name)
         {
            file = %file;
            name = Duel_missionDisplayName(%file);
            typeList = "";
         };
         ML.add(%mObj);
      }

      if(strstr(" " @ %mObj.typeList @ " ", " DuelMod ") == -1)
         %mObj.typeList = trim(%mObj.typeList SPC "DuelMod");
      %mObj.typeList[%type] = 1;

      for(%list = 0; %list < 3; %list++)
         if(%mObj.typeOptions[%type, %list] $= "")
            %mObj.typeOptions[%type, %list] = %options;
   }
}

function Duel_missionDisplayName(%file)
{
   %displayName = %file;
   %fo = new FileObject();
   if(%fo.openForRead("missions/" @ %file @ ".mis"))
   {
      while(!%fo.isEOF())
      {
         %line = %fo.readLine();
         if(getSubStr(%line, 0, 17) $= "// DisplayName = ")
         {
            %displayName = getSubStr(%line, 17, 1000);
            break;
         }
      }
      %fo.close();
   }
   %fo.delete();
   return %displayName;
}

// dtStats built its map list (ML) before this file ran. The vote list is rebuilt from the scheduler,
// outside the game script loop: buildMissionList can scan missions/*.mis, which would move the
// engine's file search out from under that loop.
Duel_injectMapList();
schedule(0, 0, "buildMissionList");

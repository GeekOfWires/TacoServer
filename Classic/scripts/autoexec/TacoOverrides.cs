//Various Overrides
//

package TacoOverrides
{

//Issue with the start grenade throw was very soft and bumped it up a tad
function serverCmdEndThrowCount(%client, %data)
{
   if(%client.player.throwStart == 5)
      return;

   // ---------------------------------------------------------------
   // z0dd - ZOD, 8/6/02. New throw str features
   %throwStrength = (getSimTime() - %client.player.throwStart) / 150;
   if(%throwStrength > $maxThrowStr) 
      %throwStrength = $maxThrowStr; 
   else if(%throwStrength < 0.5)
      %throwStrength = 0.5;
   // ---------------------------------------------------------------
   
   %throwScale = %throwStrength / 2;
   %client.player.throwStrength = %throwScale;

   %client.player.throwStart = 5; //was 0
}

//Tank UE code by Keen
//To fix tank UE by transporting the tank far away so nothing bumps into it causing a UE
function VehicleData::onDestroyed(%data, %obj, %prevState)
{
    if(%obj.lastDamagedBy)
    {
        %destroyer = %obj.lastDamagedBy;
        game.vehicleDestroyed(%obj, %destroyer);
        //error("vehicleDestroyed( "@ %obj @", "@ %destroyer @")");
    }
    
	radiusVehicleExplosion(%data, %obj);
	
   if(%obj.turretObject)
      if(%obj.turretObject.getControllingClient())
         %obj.turretObject.getDataBlock().playerDismount(%obj.turretObject);
	 
   for(%i = 0; %i < %obj.getDatablock().numMountPoints; %i++)
   {
      if (%obj.getMountNodeObject(%i)) {
         %flingee = %obj.getMountNodeObject(%i);
         %flingee.getDataBlock().doDismount(%flingee, true);
         %xVel = 250.0 - (getRandom() * 500.0);
         %yVel = 250.0 - (getRandom() * 500.0);
         %zVel = (getRandom() * 100.0) + 50.0;
         %flingVel = %xVel @ " " @ %yVel @ " " @ %zVel;
         %flingee.applyImpulse(%flingee.getTransform(), %flingVel);
         echo("got player..." @ %flingee.getClassName());
         %flingee.damage(0, %obj.getPosition(), 0.4, $DamageType::Crash); 
      }
   }
   
   // From AntiLou.vl2
   // Info: MPB just destroyed. Change the variable
   if(%data.getName() $= "MobileBaseVehicle") // If the vehicle is the MPB, change %obj.station.isDestroyed to 1
		%obj.station.isDestroyed = 1;

   %data.deleteAllMounted(%obj);
   // -----------------------------------------------------------------------------------------
   // z0dd - ZOD - Czar, 6/24/02. Move this vehicle out of the way so nothing collides with it.
   if(%data.getName() $="AssaultVehicle")
   {
      // %obj.setFrozenState(true);
      %obj.schedule(500, "delete"); //was 2000
      //%data.schedule(500, 'onAvoidCollisions', %obj);
	  
	  //Transfer the vehicle far away
      %obj.schedule(1, "setPosition", vectorAdd(%obj.getPosition(), "40 -27 10000")); //Lowered: was 500
   }
   else if(%data.getName() $="BomberFlyer" || %data.getName() $="MobileBaseVehicle")
   {
      // %obj.setFrozenState(true);
      %obj.schedule(2000, "delete"); //was 2000
      //%data.schedule(500, 'onAvoidCollisions', %obj);
	  
	  //Transfer the vehicle far away
      %obj.schedule(100, "setPosition", vectorAdd(%obj.getPosition(), "40 -27 10000")); //Lowered: was 500
   }
   else
   {
      %obj.setFrozenState(true); 
      %obj.schedule(500, "delete"); //was 500
   }
   // -----------------------------------------------------------------------------------------
}

//OG Blaster Buff
function Armor::damageObject(%data, %targetObject, %sourceObject, %position, %amount, %damageType, %momVec, %mineSC)
{	
    //Takes 10 blaster shots to kill a heavy, 13 normal.
	if(%targetObject.client.armor $= "Heavy" && %damageType $= $DamageType::Blaster)
		%amount *= 1.3;
	
	Parent::damageObject(%data, %targetObject, %sourceObject, %position, %amount, %damageType, %momVec, %mineSC);
}

//Admin Next Mission Message at beginning of matches
function DefaultGame::startMatch(%game)
{
	Parent::startMatch(%game);
	
	for(%idx = 0; %idx < ClientGroup.getCount(); %idx++) 
	{
		%cl = ClientGroup.getObject(%idx);
					  
		if(%cl.isAdmin)
		{ 
			messageClient( %cl, 'MsgNotifyEvoNextMission', '\crNext Mission: %1', $EvoCachedNextMission);
		}
	}
}

};

// Prevent package from being activated if it is already
if (!isActivePackage(TacoOverrides))
    activatePackage(TacoOverrides);
package NoBaseRape
{

//From Evolution MOD
//Modified for our needs
function StaticShapeData::damageObject(%data, %targetObject, %sourceObject, %position, %amount, %damageType)
{
   //echo( %targetObject.getDataBlock().getClassName() );
   //echo( %targetObject.getDataBlock().getName() );
   
   %targetname = %targetObject.getDataBlock().getName();    
   
   if(!$Host::TournamentMode && $Host::NoBaseRapeEnabled && $Host::NoBaseRapePlayerCount > $TotalTeamPlayerCount)
   {        		
		if( %targetname $= "GeneratorLarge" || %targetname $= "StationInventory" || %targetname $= "SolarPanel" ) 
		{
			//Notify only if asset is on other team
			if( %targetObject.team !$= %sourceObject.team )
				NBRAssetSound( %game, %sourceObject );
			
			return;
		}
   }
   
   parent::damageObject(%data, %targetObject, %sourceObject, %position, %amount, %damageType);  
}

function loadMissionStage2()
{
   parent::loadMissionStage2();
   
   if( $CurrentMission $= "SmallCrossing" || $CurrentMission $= "Bulwark" )
   {
	   if (!isActivePackage(InvincibleInvToggle))
			activatePackage(InvincibleInvToggle);
   }
   else if (isActivePackage(InvincibleInvToggle))
			deactivatePackage(InvincibleInvToggle);
}

};

// Prevent package from being activated if it is already
if (!isActivePackage(NoBaseRape))
    activatePackage(NoBaseRape);

package InvincibleInvToggle
{

function StaticShapeData::damageObject(%data, %targetObject, %sourceObject, %position, %amount, %damageType)
{
   %targetname = %targetObject.getDataBlock().getName();
   
   //Used on some maps to make invs invincible
   if( %targetObject.invincible && %targetname $= "StationInventory" )
		return;
   
   parent::damageObject(%data, %targetObject, %sourceObject, %position, %amount, %damageType);
}

};
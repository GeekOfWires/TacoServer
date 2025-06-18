// points players in the diretion of the enemy flag home position when outdoors
// Script By: DarkTiger
// version 1.0

function CTFGame::pickTeamSpawn(%game, %team) {
   return %game.pickTeamSpawnRot(%team);
}  
function LCTFGame::pickTeamSpawn(%game, %team) {
    return %game.pickTeamSpawnRot(%team);
}
function SCtFGame::pickTeamSpawn(%game, %team) {
    return %game.pickTeamSpawnRot(%team);
}
 
function DefaultGame::pickTeamSpawnRot(%game, %team){
// early exit if no nav graph
   if (!navGraphExists())
   {
      echo("No navigation graph is present.  Build one.");
      return -1;
   }
   
   for (%attempt = 0; %attempt < 20; %attempt++)
   {
      //  finds a random spawn sphere
      //  selects inside/outside on this random sphere
      //  if the navgraph exists, then uses it to grab a random node as spawn 
      //   location/rotation
      %sphere = %game.selectSpawnSphere(%team);
      if (%sphere == -1)
      {
         echo("No spawn spheres found for team " @ %team);   
         return -1;
      }

      %zone = %game.selectSpawnZone(%sphere);
      %useIndoor = %zone;
      %useOutdoor = !%zone;
      if (%zone)
         %area = "indoor";
      else
         %area = "outdoor";
                           
      %radius = %sphere.radius;
      %sphereTrans = %sphere.getTransform();
      %sphereCtr = getWord(%sphereTrans, 0) @ " " @ getWord(%sphereTrans, 1) @ " " @ getWord(%sphereTrans, 2);   //don't need full transform here, just x, y, z
      //echo("Selected Sphere is " @ %sphereCtr @ " with a radius of " @ %radius @ " meters.  Selecting from " @ %area @ " zone.");

      %avoidThese = $TypeMasks::VehicleObjectType  | $TypeMasks::MoveableObjectType |
                    $TypeMasks::PlayerObjectType   | $TypeMasks::TurretObjectType;

      for (%tries = 0; %tries < 10; %tries++)
      {
         %nodeIndex = navGraph.randNode(%sphereCtr, %radius, %useIndoor, %useOutdoor);
         if (%nodeIndex >= 0)
         {
            %loc = navGraph.randNodeLoc(%nodeIndex);
            %adjUp = VectorAdd(%loc, "0 0 1.0");   // don't go much below
            
            if (ContainerBoxEmpty( %avoidThese, %adjUp, 2.0))
               break;
         }
      }
      
      if (%nodeIndex >= 0)
      {
         %loc = navGraph.randNodeLoc(%nodeIndex);
         if (%zone)//spawn indoors
         {
            %fpos = getWords($TeamFlag[%team == 1 ? 2 : 1].originalPosition,0,2); 
            %flos = containerRayCast(vectorAdd(%loc,"0 0 1"), vectorAdd(%fpos, "0 0 1"), $TypeMasks::InteriorObjectType | $TypeMasks::StaticTSObjectType | $TypeMasks::ForceFieldObjectType);
            if(%flos){// do we have anything inbetween us and the flag if not face it 
               if(vectorDist(%loc, getWords(%flos,1,3)) < 25){
                   //error("indoor" SPC %loc);
                  %trns = %loc @ " 0 0 1 0";
                  %spawnLoc = whereToLook(%trns);
                  return %spawnLoc;
               }
            } 
             //error("outdoor" SPC %loc);
            %rot = %game.selectSpawnDir(%loc, %team, %zone);               
            %spawnLoc = %loc @ %rot;
            return %spawnLoc;
         }
         //error("outdoor" SPC %loc);
         %rot = %game.selectSpawnDir(%loc, %team, %zone);               
         %spawnLoc = %loc @ %rot;         
         return %spawnLoc;            
      }
   }   
} 

//face flag when spawn 
function DefaultGame::selectSpawnDir(%game, %loc, %team, %zone){
   %team = %team == 1 ? 2 : 1;
   %fpos = getWords($TeamFlag[%team].originalPosition,0,2);   
   //this used only when spawn loc is not on an interior.  This points spawning player to the ctr of spawnshpere
   %fpos = setWord(%fpos, 2, 0);      
   %loc = setWord(%loc, 2, 0);      
   
   if(VectorDist(%loc, %fpos) == 0)
      return " 0 0 1 0  ";
      
   %vec = VectorNormalize(VectorSub(%fpos, %loc));
   %angle = mAcos(getWord(%vec, 1));
   return (%loc < %fpos) ? (" 0 0 1 " @ %angle) : (" 0 0 1 " @ -%angle);// this works some how
}  

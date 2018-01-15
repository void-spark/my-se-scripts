// MoveIT Script by Brenner (http://steamcommunity.com/profiles/76561198032405437/myworkshopfiles/)                   
// Version 2.01  
// New features in 2.01: bugfix, now also works with non-english game setting 
// ----------------------------------------------------------------------------------------------------------------------------------------------                   
                                
void Main(string argument)                                  
{              
 	 var cmds = ParseCommandline(argument);                                         
   
	 for(var i = 0; i<cmds.Count;i++)                   
	 {                   
    		HandleBlocks(cmds[i]);                          
	 }             
}                               
                         
                         
void HandleBlocks(CommandLine cmd)                         
{                         
                              
 var blocks = GetGroupOrNamedBlocks(cmd.Name);      
 var foundSomething = false;                   
            
 for(int i = 0; i < blocks.Count;i++)                            
  {                             
    var block = blocks[i];                            
    if(HandleBlock(block,cmd)) foundSomething = true;                                     
  }                                
                            
 if(!foundSomething) {AddError("Error: there is no suitable block (or group with suitable Blocks) named "+cmd.Name);}                                 
}                         
                         
                           
bool HandleBlock(IMyTerminalBlock block,CommandLine cmd)                           
{                           
  if(block == null) return false;                           
                           
               
  //  handle stuff that is Supported by ALL Blocks. Basically the first 4 on / off choices at the top.                  
   HandleBoolParams(cmd,block,"OnOff","ShowInTerminal","ShowInToolbarConfig","ShowOnHUD");                 
            
   HandleActionParam(block,cmd);             
           
           
               
  // Is Rotor?                            
  if(block is IMyMotorStator)                            
  {                            
    HandleRotor(block as IMyMotorStator,cmd);                             
    return true;                             
  }                            
  // Is Piston?                         
  if(block is IMyPistonBase)                           
  {                           
   HandlePiston(block as IMyPistonBase,cmd);                           
   return true;                          
  }                           
   // is Grav Gen?                          
   if(block is IMyGravityGeneratorBase)                         
   {                          
    HandleGravGen(block as IMyGravityGeneratorBase,cmd);                         
    return true;                         
   }                          
   if(block is IMyThrust)                        
   {                        
      HandleThruster(block as IMyThrust, cmd);                              
      return true;                         
   }                        
   if(block is IMyGyro)                       
   {                       
      HandleGyro(block as IMyGyro, cmd);                              
      return true;                         
   }                       
   if(block is IMyLightingBlock)                  
   {                  
	      HandleLight(block as IMyLightingBlock, cmd);                              
      return true;                         
   }                  
   if(block is IMyRadioAntenna || block is IMyBeacon)                
   {                
    HandleGeneralParams(cmd,block,"Radius");                
    return true;              
   }                
   if(block is IMySensorBlock)                
   {                
    HandleGeneralParams(cmd,block,"Left","Right","Top","Bottom","Back","Front");                
    return true;                        
   }                
   if(block is IMyProjector)                
   {                
    HandleGeneralParams(cmd,block,"X","Y","Z","RotX", "RotY","RotZ");                
    return true;                        
   }                
   if(block is IMySoundBlock)                
   {                
    HandleGeneralParams(cmd,block,"VolumeSlider","RangeSlider","LoopableSlider");                
    return true;                        
   }                
   if(block is IMySpaceBall)                
   {                
    HandleGeneralParams(cmd,block,"Restitution","Friction","VirtualMass");                
    return true;                        
   }                
   if(block is IMyLandingGear)                
   {                
    HandleGeneralParams(cmd,block,"BreakForce");              
    return true;                        
   }                
   if(block is IMyTimerBlock)                
   {                
    HandleGeneralParams(cmd,block,"TriggerDelay");               
    return true;                         
   }                
                   
   if(block is IMyLargeTurretBase)                
   {                
    HandleGeneralParams(cmd,block,"Range");                
    return true;                        
   }                
   if(block is IMyWarhead)                
   {                
    HandleGeneralParams(cmd,block,"DetonationTime");                
    return true;                        
   }                
   if(block is IMyMotorSuspension)                
   {                
       HandleGeneralParams(cmd,block,"Power", "MaxSteerAngle","SteerSpeed","SteerReturnSpeed","Damping","Strength","Height","Travel");                   
       return true;                        
   }                
   if(block is IMyTextPanel)                
   {                
     HandleTextPanel(block as IMyTextPanel,cmd);                
     return true;                        
   }                
                
   // todo: jumpdrive, battery management                      
                        
  return false;                           
}                           
             
 
void HandleActionParam(IMyTerminalBlock block,CommandLine cmd) 
{ 
	if(cmd.Arguments.ContainsKey("ACTION"))               
    { 
		var action = cmd.Arguments["ACTION"].ValueAsString; 
		if(!String.IsNullOrEmpty(action))  
		{ 
			block.ApplyAction(action);	 
		} 
	} 
}             
 
                
void HandleTextPanel(IMyTextPanel panel,CommandLine cmd)                
{	                
    HandleGeneralParams(cmd,panel,"FontSize","ChangeIntervalSlider");              
              
    if(cmd.Arguments.ContainsKey("FONTCOLOR"))               
    {              
    	 panel.SetValue<Color>("FontColor",cmd.Arguments["FONTCOLOR"].ValueAsColor);              
    }              
	             
	if(cmd.Arguments.ContainsKey("BACKGROUNDCOLOR"))               
    {              
    	 panel.SetValue<Color>("BackgroundColor",cmd.Arguments["BACKGROUNDCOLOR"].ValueAsColor);              
    }	              
	           
	//            
	if(cmd.Arguments.ContainsKey("SETTEXT"))               
	{    	          
	  panel.WritePublicText(cmd.Arguments["SETTEXT"].ValueForTextPanel);   	         
	}           
	         
	if(cmd.Arguments.ContainsKey("ADDTEXT"))               
	{ 	     	          
	  panel.WritePublicText(panel.GetPublicText() + cmd.Arguments["ADDTEXT"].ValueForTextPanel);	         
	}           
          
	// Bug in SE: WritePrivateText doesn't work, nothing is ever written ???         
	if(cmd.Arguments.ContainsKey("SETPRIVATETEXT"))               
	{    	          
	  panel.WritePrivateText(cmd.Arguments["SETPRIVATETEXT"].ValueForTextPanel);   	         
	}   	         
	if(cmd.Arguments.ContainsKey("ADDPRIVATETEXT"))               
	{ 	     	          
	  panel.WritePrivateText(panel.GetPrivateText() + cmd.Arguments["ADDPRIVATETEXT"].ValueForTextPanel);   	         
	}           
        
	        
	if(cmd.Arguments.ContainsKey("ADDIMAGES"))        
	{	 	         
		 var imageNames = SuperSplit(cmd.Arguments["ADDIMAGES"].ValueAsString,',',true);        
		 panel.AddImagesToSelection(imageNames); 		        
	}        
        
	if(cmd.Arguments.ContainsKey("REMOVEIMAGES"))        
	{	 	         
		 var imageNames = SuperSplit(cmd.Arguments["REMOVEIMAGES"].ValueAsString,',',true);         
		 panel.RemoveImagesFromSelection(imageNames); 		        
	}         
       
	        
	if(cmd.Arguments.ContainsKey("SETIMAGES"))        
	{	 	         
		 var imageNames = SuperSplit(cmd.Arguments["SETIMAGES"].ValueAsString,',',true);        
      
		 panel.ClearImagesFromSelection();       
		 panel.AddImagesToSelection(imageNames);		 		        
	}           
    if(cmd.Arguments.ContainsKey("SHOW"))        
	{        
	  var what = (cmd.Arguments["SHOW"].ValueAsString+"").Trim().ToUpper();        
	  switch(what)        
	  {        
	   case "PUBLIC":        
	   panel.ShowPublicTextOnScreen();        
	   break;        
	   case "PRIVATE":        
	   panel.ShowPrivateTextOnScreen();         
	   break;        
	   case "TEXTURE":        
	   case "IMG":        
	   case "IMAGE":        
	   case "PIC":        
	   panel.ShowTextureOnScreen();        
	   break;        
	  }         
	}            
        
	               
	               
}                
                       
void HandleGyro(IMyGyro gyro,CommandLine cmd)                        
{                        
                  
  HandleGeneralParams(cmd,gyro,"Power");                  
                        
  bool anythingOvverridden = false;                      
  if(cmd.Arguments.ContainsKey("YAW"))                       
  {                       
    var arg = cmd.Arguments["YAW"];                       
	gyro.SetValue<float>("Yaw", arg.Value);                          
	if(arg.Value != 0) anythingOvverridden = true;                      
  }                         
  if(cmd.Arguments.ContainsKey("PITCH"))                       
  {                       
    var arg = cmd.Arguments["PITCH"];                       
	gyro.SetValue<float>("Pitch", arg.Value);                          
	if(arg.Value != 0) anythingOvverridden = true;                      
  }                         
  if(cmd.Arguments.ContainsKey("ROLL"))                       
  {                       
    var arg = cmd.Arguments["ROLL"];                       
	gyro.SetValue<float>("Roll", arg.Value);                          
	if(arg.Value != 0) anythingOvverridden = true;                      
  }                           
                        
  gyro.SetValue<bool>("Override",anythingOvverridden);                      
                         
}                        
                       
void HandleThruster(IMyThrust thruster,CommandLine cmd)                        
{                        
  if(cmd.Arguments.ContainsKey("MAIN"))     
  {                  
	  var arg = cmd.Arguments["MAIN"];                       
	  var val = arg.Value;                       
	  if(arg.IsRelative)                       
	  {                        
	   var currentThrust = thruster.GetValue<float>("Override");                       
	   val += currentThrust;                          
	  }                         
    thruster.SetValue<float>("Override", val);       
  }                   
                         
}                        
                           
                          
                          
void HandlePiston(IMyPistonBase piston,CommandLine cmd)                           
{                           
 HandleGeneralParams(cmd,piston,"Velocity","UpperLimit","LowerLimit");                   

  if(cmd.Arguments.ContainsKey("VELOCITYDELTA")) {
    var velocityDelta = cmd.Arguments["VELOCITYDELTA"].Value;
    var currentVelocity  = piston.GetValue<float>("Velocity");
    var newVelocity = currentVelocity + velocityDelta;
    piston.SetValue("Velocity", newVelocity);
  }

 if(cmd.Arguments.ContainsKey("MAIN"))                  
 {     
	 var meters = cmd.Arguments["MAIN"].Value;                         
	 var isRelative = cmd.Arguments["MAIN"].IsRelative;                         
                         
	 if(isRelative)                         
	 {                          
	   var currentLimit  = piston.GetValue<float>("UpperLimit");                          
	   meters = currentLimit + meters;                           
	 }                         
                      
	 if(cmd.Arguments.ContainsKey("UPPERLIMIT") && meters > cmd.Arguments["UPPERLIMIT"].Value)                      
	 {                      
	  meters = cmd.Arguments["UPPERLIMIT"].Value;                      
	 }                      
	 else if(cmd.Arguments.ContainsKey("LOWERLIMIT") && meters < cmd.Arguments["LOWERLIMIT"].Value)                      
	 {                      
	  meters = cmd.Arguments["LOWERLIMIT"].Value;                      
	 }                            
                         
	 var pos = GetCurrentPistonPos(piston);                         
                          
	 // Reverse velocity if necessary                                  
	 var velocity = piston.GetValue<float>("Velocity");                              
	 if( (meters > pos && velocity < 0 ) || (meters < pos && velocity > 0)) velocity = -velocity;                              
	 piston.SetValue("Velocity",velocity);                         
                            
                            
	 piston.SetValue("UpperLimit",meters);                            
	 piston.SetValue("LowerLimit",meters);                            
 }     
     
     
}                           
                           
                    
void HandleRotor(IMyMotorStator rotor,CommandLine cmd)                              
{                              
 // TODO: UpperLimit and LowerLimit?                  
 HandleGeneralParams(cmd,rotor,"Velocity","Displacement","BrakingTorque","Torque");                   
                  
                    
 if(cmd.Arguments.ContainsKey("MAIN"))                    
 {                    
  HandleRotorNormalMode(rotor,cmd);                     
 }                  
                 
   
 else                    
 {                     
  HandleRotorLimitMode(rotor,cmd);                      
 }                    
                          
}                            
                             
void HandleRotorNormalMode(IMyMotorStator rotor,CommandLine cmd)                   
{                   
var angleArg = cmd.Arguments["MAIN"];                         
                     
                              
 // Set rotation                              
                    
 var rotationNow = rotor.GetValue<float>("UpperLimit");                                    
 var rotation = rotationNow;                    
                        
 if(angleArg.IsRelative)   
 {  
  rotation+= angleArg.Value;           
     
    
    
   
  
 }  
 else rotation = angleArg.Value;                            
                      
 if(cmd.Arguments.ContainsKey("UPPERLIMIT") && rotation > cmd.Arguments["UPPERLIMIT"].Value)                       
 {                       
  rotation = cmd.Arguments["UPPERLIMIT"].Value;                       
 }                       
 else if(cmd.Arguments.ContainsKey("LOWERLIMIT") && rotation < cmd.Arguments["LOWERLIMIT"].Value)                       
 {                       
  rotation = cmd.Arguments["LOWERLIMIT"].Value;                       
 }                       
                      
                     
 // Correct velocity if necessary                                  
 var velocity = rotor.GetValue<float>("Velocity");                              
 if( (velocity > 0 && rotationNow > rotation) || (velocity < 0 && rotationNow <rotation)) velocity = -velocity;                              
 else if(velocity == 0) { if(rotation > 0) velocity = 10; else velocity = -10;}                               
 rotor.SetValue<float>("Velocity",velocity);                              
     
 if(angleArg.IsRelative)   
 {                
 // keep rotor inside of 0 to 360 degrees    
 while(rotation >= 360 && !float.IsInfinity(rotation)) rotation = rotation - 360;  
 while(rotation < 0 && !float.IsInfinity(rotation)) rotation = rotation+ 360;            
 }  
		              
 rotor.SetValue("LowerLimit",rotation);                                    
 rotor.SetValue("UpperLimit",rotation);                             
}                   
                   
void HandleRotorLimitMode(IMyMotorStator rotor,CommandLine cmd)                   
{                    
 var rotationNow = rotor.GetValue<float>("UpperLimit");                                     
 var rotation = rotationNow;                    
                   
 if(cmd.Arguments.ContainsKey("UPPERLIMIT") && rotation > cmd.Arguments["UPPERLIMIT"].Value)                        
 {                        
  rotation = cmd.Arguments["UPPERLIMIT"].Value;                        
 }                       
 else if(cmd.Arguments.ContainsKey("LOWERLIMIT") && rotation < cmd.Arguments["LOWERLIMIT"].Value)                   
 {                   
 rotation = cmd.Arguments["LOWERLIMIT"].Value;                        
  }                   
 else {return;}                   
                    
 rotor.SetValue("LowerLimit",rotation);                                     
 rotor.SetValue("UpperLimit",rotation);                       
                   
                   
}                      
                   
                         
void HandleGravGen(IMyGravityGeneratorBase grav,CommandLine cmd)                         
{                         
      
  if(cmd.Arguments.ContainsKey("MAIN"))      
  {     
	grav.SetValue("Gravity",cmd.Arguments["MAIN"].Value);     
  }     
       
                  
  if(grav is IMyGravityGeneratorSphere) HandleGeneralParams(cmd,grav,"Gravity","Radius");                  
  else HandleGeneralParams(cmd,grav,"Gravity","Depth","Width","Height");                    
      
}                         
                  
void HandleLight(IMyLightingBlock light, CommandLine cmd)                  
{                   
 HandleGeneralParams(cmd,light,"Falloff","Intensity","Radius");                  
                
                  
 HandleGeneralParam(cmd, light,"Blink Lenght","BLINKLENGTH"); // Yep, Keen spelled it "Lenght"                 
 HandleGeneralParam(cmd, light,"Blink Lenght","BLINKLENGHT"); // Accept keens wrong spelling too                 
 HandleGeneralParam(cmd, light,"Blink Interval","BLINKINTERVAL");                  
 HandleGeneralParam(cmd, light,"Blink Offset","BLINKOFFSET");                  
                 
 // Special: Color                      
if(cmd.Arguments.ContainsKey("COLOR"))                
{               
light.SetValue<Color>("Color",cmd.Arguments["COLOR"].ValueAsColor);               
}              
             
                 
}                              
                            
void HandleGeneralParam(CommandLine cmd, IMyTerminalBlock block,string paramInSE,string paramInMoveIt)                   
{                   
                    
    if (cmd.Arguments.ContainsKey(paramInMoveIt))                   
    {         
		try      
		{      
			block.SetValue<float>(paramInSE,cmd.Arguments[paramInMoveIt].Value);                  
		}      
		catch(Exception ex)      
		{      
			AddError("Error occured while trying to set "+paramInSE +" to "+cmd.Arguments[paramInMoveIt].Value+"\n" +ex);      
		}      
		                     
              
    }                   
}                   
                   
void HandleGeneralParams(CommandLine cmd,IMyTerminalBlock block,params string[] names)                   
{                   
    for (int i = 0; i < names.Length; i++)                   
    {                   
        HandleGeneralParam(cmd, block, names[i],names[i].ToUpper());                   
    }                   
}                  
               
void HandleBoolParam(CommandLine cmd, IMyTerminalBlock block,string paramInSE,string paramInMoveIt)               
{               
   if (cmd.Arguments.ContainsKey(paramInMoveIt))                   
    {          
		try      
		{      
			block.SetValue<bool>(paramInSE,cmd.Arguments[paramInMoveIt].ValueAsBool);   		               
		}      
		catch(Exception ex)      
		{      
			AddError("Error occured while trying to set "+paramInSE +" to "+cmd.Arguments[paramInMoveIt].Value+"\n" +ex);      
		} 		     
    }                   
}               
               
void HandleBoolParams(CommandLine cmd,IMyTerminalBlock block,params string[] names)                   
{                   
    for (int i = 0; i < names.Length; i++)                   
    {           	       
	    HandleBoolParam(cmd, block, names[i],names[i].ToUpper());   		                 
    }                   
}                
                   
                   
// ===========================================                            
// Helper Functions                            
// ===========================================                             
                            
					        
void AddError(string message)      
{      
	Echo(message); 
}					        
					        
					         
IMyBlockGroup GetGroup(string name)                              
{                              
 var allGroups = new List<IMyBlockGroup>();      
 GridTerminalSystem.GetBlockGroups(allGroups);      
   
 for(var i = 0;i<allGroups.Count;i++)                                     
 {                                     
   if(name == allGroups[i].Name)                                      
   {                                 
     return allGroups[i];                                 
   }                                      
 }                                  
 return null;                              
}                              
                            
List<IMyTerminalBlock> GetGroupBlocks(string name)                                  
{                               
 var group = GetGroup(name);                               
 return group!=null?group.Blocks:new List<IMyTerminalBlock>();                                 
}                              
    
List<IMyTerminalBlock> GetGroupOrNamedBlocks(string name)                                  
{    
 List<IMyTerminalBlock> res = new List<IMyTerminalBlock>();    
 var block = GridTerminalSystem.GetBlockWithName(name) as IMyTerminalBlock;                                    
 if(block != null)                            
 {                            
  res.Add(block);    
 }                            
                              
  var blocksInGroup = GetGroupBlocks(name);                            
  for(int i = 0; i < blocksInGroup.Count;i++)                            
  {                             
    res.Add(blocksInGroup[i]);                          
  }                                
    
  return res;    
}               
      
                          
float GetCurrentPistonPos(IMyPistonBase piston)                          
{                          
 return piston.CurrentPosition;
}                         
          
         
          
         
/// ------------------------------------------------------------------------------------                
/// Parse Commandline                 
/// ------------------------------------------------------------------------------------                
         
private static List<CommandLine> ParseCommandline(string s)         
{         
    var parts = SuperSplit(s);         
         
    var res = new List<CommandLine>(parts.Count);         
    for (int i = 0; i < parts.Count;i++)         
    {         
        res.Add(ParseCommandLinePart(parts[i]));         
    }         
         
    return res;         
}         
         
         
private static CommandLine ParseCommandLinePart(string s)         
{         
    const string defaultArg = "MAIN";         
         
    var cmd = new CommandLine();         
    cmd.Arguments = new Dictionary<string, Argument>();         
         
    s = (s + "").Trim();         
         
    if (s == "") return cmd;         
         
         
    if (s.StartsWith("\""))         
    {         
        s = s.Substring(1);         
        var endpos = s.IndexOf('"');         
         
        cmd.Name = s.Substring(0, endpos);         
        s = s.Substring(endpos + 1);         
    }         
    else         
    {         
        var words = s.Split(new char[] { ' ' }, 2);         
        cmd.Name = words[0];         
         
         
        if (words.Length > 1) s = words[1];         
        else return cmd;         
    }         
         
    s = s.Trim();         
    s = s.Replace("  ", " ");         
         
    //var parts = s.Split(' ');         
    var parts = SuperSplit(s,' ',false);         
         
    int argNr = 0;         
         
    var temp = 0.0f;         
    for (int i = 0; i < parts.Count;)         
    {         
        var arg = new Argument();         
         
        string val;         
                         
        if (i == 0 && !String.IsNullOrWhiteSpace(defaultArg) && float.TryParse(parts[i], out temp))         
        {         
            arg.Name = defaultArg;         
            val = parts[i];         
         
            i++;         
        }         
        else         
        {         
            arg.Name = (parts[i]+"").ToUpper();         
            val = parts[i + 1];         
            //if (val.StartsWith("\"")) val = val.Substring(1);         
            //if (val.EndsWith("\"")) val = val.Substring(0,val.Length-1);         
         
            i += 2;         
        }         
         
        arg.ValueAsString = val;         
                         
         
        cmd.Arguments[arg.Name] = arg;         
        argNr++;         
    }         
         
    return cmd;         
}         
         
         
// Works like the regular split command, except text in quotes is never split.                  
private static List<string> SuperSplit(string s, char separator = ';',bool removeQuotes=false)         
{         
         
    var res = new List<string>();         
         
         
    var inQuotes = false;         
    var lastCutPos = 0;         
         
    for (int charPos = 0; charPos < s.Length; charPos++)         
    {         
        var c = s[charPos];         
        if (c == '"') inQuotes = !inQuotes;         
        else if (c == separator && !inQuotes)         
        {         
            res.Add(s.Substring(lastCutPos, charPos - lastCutPos));         
            lastCutPos = charPos + 1;         
        }         
    }         
         
    res.Add(s.Substring(lastCutPos, s.Length - lastCutPos));         
         
       
	if(removeQuotes)       
	{       
	 for(int i = 0;i < res.Count;i++)       
	 {       
	   var val = res[i];       
 	  if (val.StartsWith("\"")) val = val.Substring(1);         
   if (val.EndsWith("\"")) val = val.Substring(0,val.Length-1);            
	   res[i] = val;       
	 }       
	}          
       
    return res;         
}         
         
         
                 
                      
                   
public struct CommandLine                   
{                   
    public string Name;                                   
    public Dictionary<string, Argument> Arguments;                   
}         
         
public struct Argument         
{         
    public string Name;         
         
    public string ValueAsString;         
         
	public string ValueForTextPanel         
	{         
	 get         
	 {         
  var text = ValueAsString.Replace("<br>","\n");      
  if (text.StartsWith("\"")) text = text.Substring(1);          
  if (text.EndsWith("\"")) text = text.Substring(0,text.Length-1);            
	  return text;         
	 }         
	}         
         
    // Value as float               
    public float Value         
    {         
        get         
        {         
            return Convert.ToSingle(ValueAsString);         
        }         
    }         
         
    public bool ValueAsBool         
    {         
        get         
        {         
            var val = ValueAsString.ToUpper();         
            return val == "1" || val == "YES" || val == "TRUE" || val == "ON" || val == "YEP" || val == "YEAH";         
        }         
    }         
         
    public Color ValueAsColor               
    {               
        get               
        {               
            var color = new Color(0,0,0);               
            var colorParts = ValueAsString.Split(',');               
            if (colorParts.Length < 3) return color;               
         
            color.R = Convert.ToByte(colorParts[0]);               
            color.G = Convert.ToByte(colorParts[1]);               
            color.B = Convert.ToByte(colorParts[2]);               
            return color;               
        }               
    }               
         
         
    public bool IsRelative         
    {         
        get         
        {            
            return ValueAsString.StartsWith("+") || ValueAsString.StartsWith("-");         
        }         
         
    }         
         
         
}   
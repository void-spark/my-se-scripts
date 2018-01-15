const string PREFIX = "PhinB";  

IMyTextPanel myLcd;

void Main(string argument) {
  if(myLcd == null) {
    myLcd = FindFirstWithPrefix<IMyTextPanel>();
  }

  if(String.IsNullOrEmpty(argument)){
    Print("No argument given!", false);
    return;
  }

  Print("Using group: " + argument, false); 

  List<IMyTerminalBlock> blocks = GetGroupBlocks(argument);

  Print("Group size: " + blocks.Count, true );  

  for(var i = 0;i < blocks.Count;i++) {
    blocks[i].SetCustomName(argument + " - " + (i + 1) );
  }    
  //myRotor.SetValueFloat( "Velocity", limitedVelocity );
}

IMyBlockGroup GetGroup(string name) {
  var allGroups = new List<IMyBlockGroup>();
  GridTerminalSystem.GetBlockGroups(allGroups);

  for(var i = 0;i<allGroups.Count;i++) {
    if(name == allGroups[i].Name) {
      return allGroups[i];
    }
  }
  return null;
}

List<IMyTerminalBlock> GetGroupBlocks(string name) {
  var group = GetGroup(name);
  return group != null ? group.Blocks : new List<IMyTerminalBlock>();
}

public void Print( string strIn , bool append) { 
  if(myLcd != null) { 
    myLcd.WritePublicText(strIn + "\r\n", append); 
  } 
}

public T FindFirstWithPrefix<T>() {
  var list = new List<IMyTerminalBlock>(); 
  GridTerminalSystem.GetBlocksOfType<T>(list, FilterByPrefix); 
  	if( list.Count == 0 ) {
    return default(T);
  }
  return (T)list[0];
}

bool FilterByPrefix(IMyTerminalBlock block) {  
    return block.CustomName.StartsWith(PREFIX); 
} 

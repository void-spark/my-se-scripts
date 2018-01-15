    const string PREFIX = "PhinA";

bool set;
String lcdBuffer;
IMyTextPanel myLcd;
IMyTimerBlock myTimer;

public struct StiltInfo {
  public IMyPistonBase piston;
  public IMyLandingGear landingGear;

  public StiltInfo(IMyPistonBase piston, IMyLandingGear landingGear) {
    this.piston = piston;
    this.landingGear = landingGear;
  }
}       

List<StiltInfo> stilts = new List<StiltInfo>();

void Main(string argument) {
  if( set == false ) {
    Setup();
  }

  Print("-- MINER CONTROL --", false);

  bool ready = true;

  for(int k = 0 ; k < stilts.Count; k++) {
    StiltInfo stilt = stilts[k];
    Print(stilt.piston.CustomName, true);
    Print(stilt.landingGear.CustomName + ": " + IsReadyToLock(stilt.landingGear)  + ", " + stilt.landingGear.IsLocked, true);
    Print(stilt.piston.CurrentPosition + " " + stilt.piston.Status, true);
    if(!IsReadyToLock(stilt.landingGear) && !stilt.landingGear.IsLocked) {
      stilt.piston.SetValueFloat( "Velocity", 0.05f );
      ready =  false;
    } else {
      stilt.piston.SetValueFloat( "Velocity", 0.0f );
      TerminalBlockExtentions.ApplyAction(stilt.landingGear,"Lock"); 
    }
  }
  Print("Ready: " + ready, true); 

  if(myLcd != null) {
    myLcd.WritePublicText(lcdBuffer, false);
  }

  TerminalBlockExtentions.ApplyAction(myTimer,"TriggerNow");
}

public void Setup() {
  myLcd = FindFirst<IMyTextPanel>();
  myLcd.ShowPublicTextOnScreen();

  myTimer = FindFirst<IMyTimerBlock>();

  var l=new List<IMyTerminalBlock>();
  GridTerminalSystem.GetBlocksOfType<IMyPistonBase>(l);
  List<IMyPistonBase>pistons = l.ConvertAll(x => (IMyPistonBase)x);
  for(int k = 0 ; k < pistons.Count; k++) {
    IMyPistonBase piston = pistons[k];
    int index = piston.CustomName.IndexOf("Up");
    if(index == -1) {
      continue;
    }
    IMyLandingGear landingGear = FindFirstWithPrefix<IMyLandingGear>("Landing Gear " + piston.CustomName.Substring(index + 3));
    stilts.Add(new StiltInfo(piston, landingGear));
  }

  set = true;
}

public bool IsReadyToLock2(IMyLandingGear gear) {
   return gear.DetailedInfo.Contains("Ready To Lock");
}

public bool IsReadyToLock(IMyTerminalBlock block) { 
    var builder = new StringBuilder(); 
    block.GetActionWithName("SwitchLock").WriteValue(block, builder); 
  
    return builder.ToString() == "Ready To Lock"; 
} 
  
public bool IsLocked(IMyTerminalBlock block) { 
    var builder = new StringBuilder();  
    block.GetActionWithName("SwitchLock").WriteValue(block, builder);  
  
    return builder.ToString() == "Locked"; 
}

public void Print( string str , bool append) {
  if(!append) {
    lcdBuffer = str + "\r\n";
  } else {
    lcdBuffer += str + "\r\n";
  }
}

public T FindFirst<T>() {
  var list = new List<IMyTerminalBlock>();
  GridTerminalSystem.GetBlocksOfType<T>(list, x => x.CubeGrid == Me.CubeGrid);
  if( list.Count > 0) {
    return (T)list[0];
  }
  return default(T);
}

public T FindFirstWithPrefix<T>(String prefix) {
  var list = new List<IMyTerminalBlock>();
  GridTerminalSystem.GetBlocksOfType<T>(list);
  for(int pos = 0; pos < list.Count; pos++) {
    if(list[pos].CustomName.StartsWith(prefix)) {
      return (T)list[pos];
    }
  }
  return default(T);
}

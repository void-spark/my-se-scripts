const string PREFIX = "Phin Projector";

bool set;
IMyTextPanel myLcd;
IMyProjector myProjector;

void Main(string argument) {
  if( set == false ) {
    Setup();
  }

  Print("-- Projector status --", false);
  Update();
}

public void Print( string strIn , bool append) {
  if(myLcd != null) {
    myLcd.WritePublicText(strIn + "\r\n", append);
  }
}

public void Setup() {
  myLcd = FindFirstWithPrefix<IMyTextPanel>();
  if(myLcd == null) {
    Echo("No LCD found");
    return;
  }
  myLcd.ShowPublicTextOnScreen();
  myLcd.SetValueFloat("FontSize", 0.6f);


  myProjector = FindFirstWithPrefix<IMyProjector>();
  if(myProjector == null) {
    Echo("No Projector found");
    return;
  }
  set = true;
}

public void Update() {
  Print(myProjector.DetailedInfo, true);
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

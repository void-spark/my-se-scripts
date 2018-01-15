const string PREFIX = "Phin";  

bool set;
IMyTextPanel myLcd;
IMyMotorStator myRotor;

void Main(string argument) {
  	if( set == false ) {
    		Setup();
    		set = true;
  	}

  Print("-- Running --", false);
  Update();
  Print("-- Done --", true);
}

public void Print( string strIn , bool append) {
  if(myLcd != null) {
    myLcd.WritePublicText(strIn + "\r\n", append);
  }
}

public void Setup() {
  myLcd = FindFirstWithPrefix<IMyTextPanel>();
  myRotor = FindFirstWithPrefix<IMyMotorStator>();
  //myRotor.SetValueFloat( "Velocity", 0 );
}

public void Update() { 
  	float tarDeg = getAngle( myRotor ); 
  Print( "TA: "+ tarDeg.ToString(), true ); 
} 

public float getAngle( IMyMotorStator motor ) { 
  string data = motor.DetailedInfo;
  	string[] dataSplit = data.Split( ':' );
  	return float.Parse( dataSplit[1].Substring( 0, dataSplit[1].Length-1 ) );
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

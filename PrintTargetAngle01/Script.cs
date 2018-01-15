const string PREFIX = "Phin";  

bool set;
float targetDeg = 150;
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
  	float angleDeg = getAngle( myRotor );
  Print( "Angle: "+ angleDeg.ToString(), true );
  float angleRad = MathHelper.ToRadians(angleDeg);
  float targetRad = MathHelper.ToRadians(targetDeg);
  float diffRad = MathHelper.WrapAngle(targetRad - angleRad);
  float diffDeg =  (float)Math.Round(MathHelper.ToDegrees(diffRad));
  Print( "Diff: "+ diffDeg.ToString(), true );
  Print( "Diag: "+ ((angleDeg + diffDeg) % 360).ToString(), true );
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

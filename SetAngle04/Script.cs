const string PREFIX = "PhinA";

const float GOOD_ENOUGH_RAD = (float)Math.PI / (180.0f * 100.0f);

bool set;
IMyTextPanel myLcd;
IMyTimerBlock myTimer;

public struct RotationInfo {
  public float targetRad;
  public IMyMotorStator rotor;

  public RotationInfo(float targetRad, IMyMotorStator rotor) {
    this.targetRad = targetRad;
    this.rotor = rotor;
  }
}

List<RotationInfo> work = new List<RotationInfo>();

void Main(string argument) {
  if( set == false ) {
    Setup();
  }

  Print("-- ROTOR CONTROL --", false);
  String[] args = argument.Split(' ');
  if(args.Length >= 2) {
    for(int i = 0 ; i < args.Length / 2; i++) {
      Print("Input: " + args[i*2] + " - " + args[i*2 +1], true);
      var l=new List<IMyTerminalBlock>();
      GridTerminalSystem.GetBlocksOfType<IMyMotorStator>(l);
      List<IMyMotorStator>rotors=l.ConvertAll(x => (IMyMotorStator)x);
      for(int k = 0 ; k < rotors.Count; k++) {
        IMyMotorStator rotor = rotors[k];
        if(!rotor.CustomName.StartsWith(args[i*2])) {
          continue;
        }
        float targetRad = MathHelper.ToRadians(float.Parse(args[i*2 +1]));
        for(int j = 0; j < work.Count; j++ ) {
          if(work[j].rotor == rotor) {
            work.RemoveAt(j);
            j--;
          }
        }
        work.Add(new RotationInfo(targetRad, rotor));
      }
    }
  }

  for(int i = 0; i < work.Count; i++ ) {
    bool rotorDone = Update(work[i]);
    if(rotorDone) {
      work.RemoveAt(i);
      i--;
    }
  }
  Print("Work left: " + work.Count,true);
  if (work.Count > 0) {
    ITerminalAction action = myTimer.GetActionWithName("TriggerNow");
    action.Apply(myTimer);
  }
}

public void Setup() {
  myLcd = FindFirstWithPrefix<IMyTextPanel>();
  myLcd.ShowPublicTextOnScreen();
  myTimer = FindFirstWithPrefix<IMyTimerBlock>();

  set = true;
}

public bool Update(RotationInfo info) {
  float angleRad = info.rotor.Angle;
  Print( "Rotor: "+ info.rotor.CustomName + "@" + MathHelper.ToDegrees(angleRad), true );
  float diffRad = MathHelper.WrapAngle(info.targetRad - angleRad);
  Print( "Diff: "+ MathHelper.ToDegrees(diffRad), true );

  bool done;

  float limitedVelocity;
  if ( Math.Abs(diffRad) > GOOD_ENOUGH_RAD) {
    // -1 if we're -180 deg offset, +1 if we're +180 deg offset.
    float normalizedDiff = diffRad / (float)Math.PI;
    float targetVelocity = normalizedDiff * 120.0f;
    limitedVelocity = MathHelper.Clamp( targetVelocity, -30.0f, 30.0f);
    done = false;
  } else {
    limitedVelocity = 0.0f;
    done = true;
  }
  Print( "Vel: " + limitedVelocity, true );
  info.rotor.SetValueFloat( "Velocity", limitedVelocity );
  // Print( "Done: " + done, true );
  return done;
}

public void Print( string strIn , bool append) {
  if(myLcd != null) {
    myLcd.WritePublicText(strIn + "\r\n", append);
  }
}

public T FindFirstWithPrefix<T>() {
  return FindFirstWithPrefix<T>(PREFIX);
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

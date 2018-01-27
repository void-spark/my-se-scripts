// Each X'th time we're called to actually do something.
int INTERVAL = 3;

// Counter for how many times we've been called.
long step;

// Time since start
double elapsedMs;

// Indicates if setup was (succesfully) completed.
// Can bet reset with "RESET"
bool isSetupDone;

Vector3D lastPos;
Vector3D  lastSpeed;
Vector3D  lastSpeed2; 

Dictionary<Vector3, List<IMyThrust>> _thrusters;

const string PREFIX = "AutoPilot";

List<IMyThrust> thrusters;
List<IMyBatteryBlock> batteries;
List<IMyReactor> reactors;

String lcdBuffer;
IMyTextPanel myLcd;
IMyRemoteControl myRemote;
IMyShipConnector myConnector;

// Directions in ship coords, each with length 1.
Vector3D up;
Vector3D down;
Vector3D left;
Vector3D right;
Vector3D forward;
Vector3D backward;

IMyCockpit cockpit;

Vector3D location;
Vector3D speed; 
MatrixD toWorldRot;
MatrixD toShipRot;
static Matrix shipOrient;


public Program () {
  
}

public void Main(string argument, UpdateType updateType) {
  elapsedMs += Runtime.TimeSinceLastRun.TotalMilliseconds;

  // User, timer, etc. triggered us
  if ((updateType & (UpdateType.Trigger | UpdateType.Terminal)) != 0) {
      if(!isSetupDone) {
        // Try to do inital setup, don't do this in the constructor, since we might want to redo it if the ship changed.
        // Which can be done using the 'RESET' command.
        isSetupDone = Setup();
        if(!isSetupDone) {
          // Setup failed, don't continue
          if(myLcd != null) {
            myLcd.WritePublicText(lcdBuffer, false);
          }
          Runtime.UpdateFrequency = UpdateFrequency.None;
          return;
        }
      }
  }

  // User, timer, etc. triggered us
  if ((updateType & (UpdateType.Trigger | UpdateType.Terminal)) != 0) {
      if(argument == "STOP") {
        Print("Emergency stop", false);
        elapsedMs = 0.0;
        Runtime.UpdateFrequency = UpdateFrequency.None;
      } else if(argument == "START") {
        Print("Starting", false);
        elapsedMs = 0.0;
        Runtime.UpdateFrequency = UpdateFrequency.Update1;
      } else if(argument == "RESET") {
        Print("Reset", false);
        elapsedMs = 0.0;
        isSetupDone = false;
        Runtime.UpdateFrequency = UpdateFrequency.None;
      }
  }

  // Self update
  if ((updateType & UpdateType.Update1) != 0) {
         if(step++ % INTERVAL != 0) {
           return;
         }
         Print("-- " + PREFIX + "(" + step + ") -- ", false);
         double elapsedNow = elapsedMs;
         elapsedMs = 0.0;
         if(!primaryLogic(argument, elapsedNow)) {
               Runtime.UpdateFrequency = UpdateFrequency.None;
         }
  }

  if(myLcd != null) {
    myLcd.WritePublicText(lcdBuffer, false);
  }
}

bool primaryLogic(string argument, double elapsedNow) {

  Print("Elapsed MS: " +  elapsedNow, true );
  Print("Remote: " + (myRemote != null), true );
  Print("Remote: " + myRemote.Position.ToString(), true );
  Print("Connector: " + myConnector.Position.ToString(), true );

  Print( 
    "Thrusters F: " + thrusterPow(forward) + 
    ", B: " + thrusterPow(backward) + 
    ", L: " + thrusterPow(left) + 
    ", R: " + thrusterPow(right) + 
    ", U: " + thrusterPow(up) + 
    ", D: " + thrusterPow(down), true); 

    MyShipMass mass = myRemote.CalculateShipMass();


	/// Gets the base mass of the ship.  
  Print("BaseMass: " + mass.BaseMass, true ); 
	/// Gets the total mass of the ship, including cargo.  
  Print("TotalMass: " + mass.TotalMass, true );  
	/// Gets the physical mass of the ship, which accounts for inventory multiplier.  
  Print("PhysicalMass: " + mass.PhysicalMass, true );  

   location = myRemote.GetPosition();
  Print("Position: " + location.ToString(), true);

  // Only do stuff if time has passed, otherwise we get NaN's and such (and our last position will still be 0,0,0)
  if(elapsedNow != 0.0) {
    toWorldRot = myRemote.WorldMatrix.GetOrientation();
    toShipRot = MatrixD.Transpose(toWorldRot);

    Vector3D grav=myRemote.GetNaturalGravity();

    speed = (location - lastPos) * (1000.0 / elapsedNow);
    Vector3D speedLocal = Vector3D.Transform(speed, toShipRot);

  Print("SpeedA: " + speed.Length(), true );    
  Print("SpeedB: "  + myRemote.GetShipSpeed(), true );    
 // Print("SpeedA: " + ToString(speed), true );    
 // Print("SpeedB: "  + ToString(myRemote.GetShipVelocities().LinearVelocity), true );     


    Vector3D acc = (speed - lastSpeed) * (1000.0 / elapsedNow); 
    Vector3D acc2 = (myRemote.GetShipVelocities().LinearVelocity - lastSpeed2) * (1000.0 / elapsedNow);  

  //Print("AccA: " + acc.Length(), true );     
//Print("AccB: " + acc.Length(), true );      

  Print("AccA: " + ToString(acc), true );      
Print("AccB: " + ToString(acc), true );      

  }

  lastPos = location;
lastSpeed = speed;
lastSpeed2 = myRemote.GetShipVelocities().LinearVelocity;

  return true;
}



// Should be a Vector with each component at most 1.0 (so length can be > 1.0).
public void setThrusters( ref Vector3D control ) {
// Instead one list of thruster classes, and each has a direction field
    float upControl = Math.Max(0.0f, (float)control.Y);
    float downControl = Math.Max(0.0f, (float)-control.Y);
    float rightControl = Math.Max(0.0f, (float)control.X);
    float leftControl = Math.Max(0.0f, (float)-control.X);
    float forwardControl = Math.Max(0.0f, (float)-control.Z);
    float backwardControl = Math.Max(0.0f, (float)control.Z);
    _thrusters[up].ForEach(thruster => thruster.ThrustOverridePercentage = upControl);
    _thrusters[down].ForEach(thruster => thruster.ThrustOverridePercentage = downControl);
    _thrusters[left].ForEach(thruster => thruster.ThrustOverridePercentage = leftControl);
    _thrusters[right].ForEach(thruster => thruster.ThrustOverridePercentage = rightControl);
    _thrusters[forward].ForEach(thruster => thruster.ThrustOverridePercentage = forwardControl);
    _thrusters[backward].ForEach(thruster => thruster.ThrustOverridePercentage = backwardControl);
}

public String thrusterCount(Vector3 direction) {
  if(_thrusters.ContainsKey(direction)) {
    return _thrusters[direction].Count.ToString();
  } else {
    return "None";
  }
}

public String thrusterPow(Vector3 direction) { 
  if(_thrusters.ContainsKey(direction)) {
    float totMaxEffectiveThrust = 0;
    _thrusters[direction].ForEach(t => totMaxEffectiveThrust += t.MaxEffectiveThrust); 
totMaxEffectiveThrust /= 1000.0f;
    return totMaxEffectiveThrust.ToString() + "kN";
  } else { 
    return "None"; 
  } 
} 



 

public bool Setup() {
  Print("-- Setup --", false);
  step = 0;

  myLcd = FindFirstWithPrefixOrAny<IMyTextPanel>();
  if(myLcd != null) {
    myLcd.ShowPublicTextOnScreen();
    myLcd.SetValueFloat("FontSize", 1.0f);
  }

  myRemote = FindFirstWithPrefixOrAny<IMyRemoteControl>();
  if(myRemote == null) {
    Print("Remote not found!", true);
    return false;
  }

  myConnector = FindFirstWithPrefixOrAny<IMyShipConnector>();
  if(myConnector == null) {
    Print("Connector not found!", true);
    return false;
  }

  cockpit = FindFirst<IMyCockpit>();
  if(cockpit == null) {
    Print("Cockpit not found!", true);
    return false;
  }

  myRemote.Orientation.GetMatrix(out shipOrient);

  Matrix fromGridToReference;
  myRemote.Orientation.GetMatrix(out fromGridToReference);
  Matrix.Transpose(ref fromGridToReference, out fromGridToReference);

  up = shipOrient.Up;
  down = shipOrient.Down;
  left = shipOrient.Left;
  right = shipOrient.Right;
  forward = shipOrient.Forward;
  backward = shipOrient.Backward;

  List<IMyTerminalBlock> list = new List<IMyTerminalBlock>();

  GridTerminalSystem.GetBlocksOfType<IMyReactor>(list, x => x.CubeGrid == Me.CubeGrid);
  reactors = list.ConvertAll(x => (IMyReactor)x);

  GridTerminalSystem.GetBlocksOfType<IMyBatteryBlock>(list, x => x.CubeGrid == Me.CubeGrid);
  batteries = list.ConvertAll(x => (IMyBatteryBlock)x);

  GridTerminalSystem.GetBlocksOfType<IMyThrust>(list, x => x.CubeGrid == Me.CubeGrid);
  thrusters = list.ConvertAll(x => (IMyThrust)x);

  _thrusters = new Dictionary<Vector3, List<IMyThrust>>();

  _thrusters[up] = new List<IMyThrust>();
  _thrusters[down] = new List<IMyThrust>();
  _thrusters[left] = new List<IMyThrust>();
  _thrusters[right] = new List<IMyThrust>();
  _thrusters[forward] = new List<IMyThrust>();
  _thrusters[backward] = new List<IMyThrust>();

  for (int i = 0; i < thrusters.Count; ++i) {
    IMyThrust thruster = thrusters[i];
    Matrix fromThrusterToGrid;
    thruster.Orientation.GetMatrix(out fromThrusterToGrid);
    Vector3 accelerationDirection = Vector3.Transform(fromThrusterToGrid.Backward, fromGridToReference);
    _thrusters[accelerationDirection].Add(thruster);
  }

  lastPos = myRemote.GetPosition();

  return true;
}


public void Print( string str , bool append) {
  if(!append) {
    lcdBuffer = str + "\r\n";
  } else {
    lcdBuffer += str + "\r\n";
  }
}

public T FindFirstWithPrefixOrAny<T>() where T: class {
  T result = FindFirstWithPrefix<T>(PREFIX);
  return result != null ? result : FindFirst<T>();
}

public T FindFirstWithPrefix<T>(String prefix) where T: class {
  var list = new List<IMyTerminalBlock>();
  GridTerminalSystem.GetBlocksOfType<T>(list, x => x.CubeGrid == Me.CubeGrid);
  for(int pos = 0; pos < list.Count; pos++) {
    if(list[pos].CustomName.StartsWith(prefix)) {
      return (T)list[pos];
    }
  }
  return default(T);
}

public T FindFirst<T>() where T: class {
  var list = new List<IMyTerminalBlock>();
  GridTerminalSystem.GetBlocksOfType<T>(list, x => x.CubeGrid == Me.CubeGrid);
  if( list.Count > 0) {
    return (T)list[0];
  }
  return default(T);
}


public string ToString(Vector3D value) {
  return String.Format("{0:F3}, {1:F3}, {2:F3}", value.X, value.Y, value.Z);
}




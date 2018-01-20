// Should we enable reactors on bootup
bool ENABLE_REACTORS = false;

// Each X'th time we're called to actually do something.
int INTERVAL = 3;

// Counter for how many times we've been called.
long step;

// Time since start
double elapsedMs;

// Indicates if setup was (succesfully) completed.
// Can bet reset with "RESET"
bool isSetupDone;

MyPid pid;

Vector3D lastPos;

Dictionary<Vector3, List<IMyThrust>> _thrusters;

const string PREFIX = "AutoPilot";

float CTRL_COEFF = 0.8f; //Set lower if overshooting, set higher to respond quicker
float ANGLE_OK = (float)MathHelper.ToRadians(0.01f);

List<GyroInfo> gyroInfos;
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
MatrixD toWorldRot;
MatrixD toShipRot;
static Matrix shipOrient;

List<String> targets;
bool dockingApproach;

public class Persistent {
  public bool running;
  public int tgtIndex;

  public string persist() {
    return "" + running + ":" + tgtIndex;
  }

  public void load(string input) {
    String[] store = input.Split(':');
    running = (input.Length > 0) && store.Length > 0 ? Boolean.Parse(store[0]) : false;
    tgtIndex = store.Length > 1 ? int.Parse(store[1]) : 0;
  }
}

public class Target {
  public String name;
  public Vector3D loc;
  public bool docking;

  public Target(String input) {
    String[] parts = input.Split(':');
    name = parts[1];
    loc = new Vector3D(float.Parse(parts[2]), float.Parse(parts[3]), float.Parse(parts[4]));
    docking = name.StartsWith("CONN");
  }
}

Persistent persistent;
Target target;

// Were we running when saved, in which case we want to setup on the first tick.
bool loadrunning;

public Program () {
    persistent = new Persistent();
    persistent.load(Storage);

    // Continue if we were running when saving
    if(persistent.running) {
        loadrunning = true;
        Runtime.UpdateFrequency = UpdateFrequency.Update1;
    }
}

public void Main(string argument, UpdateType updateType) {
  elapsedMs += Runtime.TimeSinceLastRun.TotalMilliseconds;

  // User, timer, etc. triggered us
  if (loadrunning || (updateType & (UpdateType.Trigger | UpdateType.Terminal)) != 0) {
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
        disengage();
        Print("Emergency stop", false);
        elapsedMs = 0.0;
        persistent.running = false;
        Runtime.UpdateFrequency = UpdateFrequency.None;
      } else if(argument == "START") {
        Print("Starting", false);
        if(!Bootup()) {
          // Bootup failed, don't continue
          Runtime.UpdateFrequency = UpdateFrequency.None;
          return;
        }
        elapsedMs = 0.0;
        persistent.running = true;
        Runtime.UpdateFrequency = UpdateFrequency.Update1;
      } else if(argument == "NEXT") {
        NextTarget();
        Print("Target: " + target.name + "(" + persistent.tgtIndex + ")", false);
      } else if(argument == "RESET") {
        Print("Reset", false);
        persistent.load("");
        elapsedMs = 0.0;
        isSetupDone = false;
        Runtime.UpdateFrequency = UpdateFrequency.None;
      }
  }

  // Self update
  if ((updateType & UpdateType.Update1) != 0) {
      if(!persistent.running) {
         Print("Not running", true);
         elapsedMs = 0.0;
       } else {
         if(step++ % INTERVAL != 0) {
           return;
         }
         Print("-- " + PREFIX + "(" + step + ") -- ", false);
         double elapsedNow = elapsedMs;
         elapsedMs = 0.0;
         if(!primaryLogic(argument, elapsedNow)) {
           disengage();
           // We're done, stop running
           persistent.running = false;
           Runtime.UpdateFrequency = UpdateFrequency.None;
         }
       }
  }

  if(myLcd != null) {
    myLcd.WritePublicText(lcdBuffer, false);
  }
}

public void Save() {
    Storage = persistent.persist();
}

bool primaryLogic(string argument, double elapsedNow) {

  Print("Elapsed MS: " +  elapsedNow, true );
  Print("Remote: " + (myRemote != null), true );
  Print("Gyros: " + gyroInfos.Count + ", Thrusters: " + thrusters.Count, true );
  Print("Remote: " + ToString(myRemote.Position), true );
  Print("Connector: " + ToString(myConnector.Position), true );

  Print(
    "Thrusters F: " + thrusterCount(forward) +
    ", B: " + thrusterCount(backward) +
    ", L: " + thrusterCount(left) +
    ", R: " + thrusterCount(right) +
    ", U: " + thrusterCount(up) +
    ", D: " + thrusterCount(down), true);

  //SetControlThrusters( false );
  SetDampeners( false );

  location = myRemote.GetPosition();
  Print("Position: " + ToString(location), true);

  // Only do stuff if time has passed, otherwise we get NaN's and such (and our last position will still be 0,0,0)
  if(elapsedNow != 0.0) {
    toWorldRot = myRemote.WorldMatrix.GetOrientation();
    toShipRot = MatrixD.Transpose(toWorldRot);

    Vector3D grav=myRemote.GetNaturalGravity();

    Vector3D speed = (location - lastPos) * (1000.0 / elapsedNow);
    Vector3D speedLocal = Vector3D.Transform(speed, toShipRot);

    Nullable<Vector3D> heading;
    Vector3D targetSpeedLocal;

    if(!logic(ref grav, ref speedLocal, out heading, out targetSpeedLocal)) {
      return false;
    }

    controlThrusters(ref speedLocal, ref targetSpeedLocal, elapsedNow);
    controlGyros(ref grav, ref heading);
  }

  lastPos = location;

  return true;
}

public bool logic(ref Vector3D grav, ref Vector3D speedLocal, out Nullable<Vector3D> heading, out Vector3D targetSpeedLocal) {
  heading = null;
  targetSpeedLocal = default(Vector3D);

  bool dockPart1 = target.docking && dockingApproach;
  bool dockPart2 = target.docking && !dockingApproach;

// Connected: green/Locked  IsLocked = true, IsConnected = true
// Connectable: yellow or cyan, ready to lock,  IsLocked = true, IsConnected = false
// Unconnected: red/white , IsLocked = false, IsConnected = false

  if(myConnector.Status == MyShipConnectorStatus.Connected) {
    TerminalBlockExtentions.ApplyAction(myConnector,"SwitchLock");
  }
  if(myConnector.Enabled != dockPart2) {
    TerminalBlockExtentions.ApplyAction(myConnector,"OnOff");
  }

  Print("TGT: ("+target.name+") " + ToString(target.loc), true );

  double margin;
  Vector3D diff;
  if(dockPart1) {
    // Approach a point above the connector, like a normal NAV point.
    Vector3D dockOffset = Vector3D.Normalize(grav) * +4.5;
    diff = target.loc - (myConnector.GetPosition() + dockOffset);
    margin = 1.0;
  } else if(dockPart2) {
    // Very slowly move towards the connector.
    Vector3D dockOffset = Vector3D.Normalize(grav) * -0.5;
    diff = target.loc - (myConnector.GetPosition() + dockOffset);
    margin = 0.0;
  } else {
    // Regular NAV.
    diff = target.loc - location;
    margin = 5.0;
  }

  Vector3D diffLocal = Vector3D.Transform(diff, toShipRot);
  Print("delta Local: " + ToString(diffLocal), true);
  double distance = diffLocal.Length();
  Print("Target distance: " + distance, true);
  if(!dockPart2 && distance < margin) {
    if(target.docking) {
      dockingApproach = false;
    } else {
      NextTarget();
    }
  } else if(dockPart2 && myConnector.Status == MyShipConnectorStatus.Connectable) {
      TerminalBlockExtentions.ApplyAction(myConnector,"SwitchLock");
      Print("Connector status: " + myConnector.Status, true);
      Print("Docked", true);

      NextTarget();

      for (int i = 0; i < thrusters.Count; ++i) {
        IMyThrust thruster = thrusters[i];
        if(thruster.Enabled) {
          TerminalBlockExtentions.ApplyAction(thruster,"OnOff_Off");
        }
      }

      for (int i = 0; i < gyroInfos.Count; ++i) {
        IMyGyro gyro = gyroInfos[i].gyro;
        if(gyro.Enabled) {
          TerminalBlockExtentions.ApplyAction(gyro,"OnOff_Off");
        }
      }

      for (int i = 0; i < batteries.Count; ++i) {
        IMyBatteryBlock battery = batteries[i];
        if(!BatteryIsCharging(battery)) {
          TerminalBlockExtentions.ApplyAction(battery,"Recharge");
        }
      }

      return false;
  }

  Vector3D targetDirectionLocal = Vector3D.Normalize(diffLocal);
  double targetAcceleration = Math.Pow(100.0, 2.0) / (2.0 * 2000.0);
  double limitedDistance = Math.Min(2000.0, distance);
  double targetSpeed = dockPart2 ? 0.3 : Math.Sqrt(2 * targetAcceleration * limitedDistance);
  targetSpeedLocal = targetDirectionLocal * targetSpeed;
  if(!dockPart2 && !(dockPart1 && distance < 10.0)) {
    heading = diff;
  }
  return true;
}

public void NextTarget() {
  persistent.tgtIndex++;
  if(persistent.tgtIndex == targets.Count) {
    persistent.tgtIndex = 0;
  }
  target = new Target(targets[persistent.tgtIndex]);
  dockingApproach = target.docking;
}

public void disengage() {
  thrusters.ForEach(thruster => thruster.SetValueFloat("Override", 0.0f));
  gyroInfos.ForEach(gyroInfo => gyroInfo.gyro.SetValueBool("Override", false));
  //SetControlThrusters( true );
  SetDampeners( true );
}

public void controlThrusters(ref Vector3D speedLocal, ref Vector3D targetSpeedLocal, double elapsedNow) {
  Print("M M/S: " + speedLocal.Length(), true);
  Print("T M/S: " + targetSpeedLocal.Length(), true);

  Print("M M/S: " + ToString(speedLocal), true);
  Print("T M/S: " + ToString(targetSpeedLocal), true);

  Vector3D control;

  pid.step(ref speedLocal, ref targetSpeedLocal, elapsedNow / 1000.0, out control);
  Vector3D error = pid.getPreviousError();

  Print("ERR: " + ToString(error), true);
  Print("I: " + ToString(pid.getIntegral()), true);
  Print("CTRL: " + ToString(control), true);

  Print("Up thrust: " + control.Y, true );

  setThrusters(ref control);
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

public void controlGyros(ref Vector3D targetDown, ref Nullable<Vector3D> targetForward) {
  Vector3 normalizedTargetDown = Vector3.Normalize(targetDown);
  Vector3 localTargetDown = Vector3.Transform(normalizedTargetDown, toShipRot);
  Vector3 localDown = Vector3.Transform(down, Matrix.Transpose(shipOrient));

  Vector3 rot;
  float ang;

  if(targetForward.HasValue) {
    Vector3 normalizedTargetForward = Vector3.Normalize(targetForward.Value);
    Vector3 localTargetForward = Vector3.Transform(normalizedTargetForward, toShipRot);
    Vector3 localForward = Vector3.Transform(forward, Matrix.Transpose(shipOrient));
    Vector3 closestForwardDirectionMatch = GetClosest90DegreeVector(localTargetDown, localTargetForward);
    GetChangeInPose(localForward, localDown,  closestForwardDirectionMatch, localTargetDown, out rot, out ang);
  } else {
    GetChangeInDirection(localDown, localTargetDown, out rot, out ang);
  }

  if(ang < ANGLE_OK) {
    Print("Level", true);
    ReleaseGyros();
  } else {
    Print(String.Format("Off level: {0:F3}", MathHelper.ToDegrees(ang)), true);
    // Control speed to be proportional to distance (angle) we have left
    float ctrl_vel = (ang/(float)Math.PI) * CTRL_COEFF;
    SetGyros(rot,ctrl_vel);
  }
}

public void SetGyros(Vector3 rotation, float velocity) {
  gyroInfos.ForEach(gyroInfo => {
    // Ship local to gyro local
    Vector3 rotLocal = Vector3.Transform(rotation, gyroInfo.shipToGyro);

    // Set strength to velocity (which should be 0.0 - 1.0)
    rotLocal *= MathHelper.Clamp(gyroInfo.max * velocity, 0.01f, gyroInfo.max);

    gyroInfo.gyro.SetValueFloat("Pitch",  (float)rotLocal.GetDim(0));
    gyroInfo.gyro.SetValueFloat("Yaw",   -(float)rotLocal.GetDim(1));
    gyroInfo.gyro.SetValueFloat("Roll",  -(float)rotLocal.GetDim(2));

    gyroInfo.gyro.SetValueFloat("Power", 1.0f);
    gyroInfo.gyro.SetValueBool("Override", true);
  });
}

public void ReleaseGyros() {
  gyroInfos.ForEach(gi => gi.gyro.SetValueBool("Override", false));
}

public String thrusterCount(Vector3 direction) {
  if(_thrusters.ContainsKey(direction)) {
    return _thrusters[direction].Count.ToString();
  } else {
    return "None";
  }
}

public void SetDampeners(bool value) {
    if(cockpit.DampenersOverride != value) {
      Print((value ? "Enable" : "Disable" ) + " dampeners", true);
      TerminalBlockExtentions.ApplyAction(cockpit,"DampenersOverride");
    }
}

public void SetControlThrusters(bool value) {
    if(cockpit.ControlThrusters != value) {
      Print((value ? "Enable" : "Disable" ) + " thruster control", true);
      TerminalBlockExtentions.ApplyAction(cockpit,"ControlThrusters");
    }
}

public bool Setup() {
  Print("-- Setup --", false);
  step = 0;

  myLcd = FindFirstWithPrefixOrAny<IMyTextPanel>();
  if(myLcd != null) {
    myLcd.ShowPublicTextOnScreen();
    myLcd.SetValueFloat("FontSize", 0.84f);
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

  GridTerminalSystem.GetBlocksOfType<IMyGyro>(list, x => x.CubeGrid == Me.CubeGrid);
  gyroInfos = list.ConvertAll(x => new GyroInfo((IMyGyro)x));

  targets = new List<String>();

  targets.Add("GPS:NAV1:13505.1:143686.8:-108313.36:");
  targets.Add("GPS:CONN_1:13534.84:143649.95:-108380.27:");

//  targets.Add("GPS:NAV1:-43345.54:-20007.78:37797.06:");
//  targets.Add("GPS:NAV2:-46849.63:-17410.13:38084.17:");
//  targets.Add("GPS:NAV3:-50178.06:-10798.46:34745.96:");
//  targets.Add("GPS:CONN_MESA:-50139.2:-10769.9:34651.28:");

//  targets.Add("GPS:NAV3:-50178.06:-10798.46:34745.96:");
//  targets.Add("GPS:NAV2:-46849.63:-17410.13:38084.17:");
//  targets.Add("GPS:NAV1:-43345.54:-20007.78:37797.06:");
//  targets.Add("GPS:CONN_BR:-43332.44:-20002.64:37760.94:");

//  targets.Add("GPS:TEST1:-43345.57:-20039.8:37776.12:");
//  targets.Add("GPS:TEST2:-42841.2:-22348.01:38199.66:");
//  targets.Add("GPS:TEST3:-42503.44:-20779.72:40073.23:");
//  targets.Add("GPS:TEST1:-43345.57:-20039.8:37776.12:");

//  targets.Add("GPS:TEST1:-43345.57:-20039.8:37776.12:");
//  targets.Add("GPS:CONN_BR:-43332.44:-20002.64:37760.94:");

  target = new Target(targets[persistent.tgtIndex]);
  dockingApproach = target.docking;

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

  pid = new MyPid(0.2, 0.1, 0.0, 1.0);

  return true;
}

bool Bootup() {

  for (int i = 0; i < reactors.Count; ++i) {
    IMyReactor reactor = reactors[i];
    if(ENABLE_REACTORS && !reactor.Enabled) {
      TerminalBlockExtentions.ApplyAction(reactor,"OnOff_On");
    }
  }

  for (int i = 0; i < batteries.Count; ++i) {
    IMyBatteryBlock battery = batteries[i];
    if(!battery.Enabled) {
      TerminalBlockExtentions.ApplyAction(battery,"OnOff_On");
    }
    if(BatteryIsCharging(battery)) {
      TerminalBlockExtentions.ApplyAction(battery,"Recharge");
    }
  }

  for (int i = 0; i < thrusters.Count; ++i) {
    IMyThrust thruster = thrusters[i];
    if(!thruster.Enabled) {
      TerminalBlockExtentions.ApplyAction(thruster,"OnOff_On");
    }
  }

  for (int i = 0; i < gyroInfos.Count; ++i) {
    IMyGyro gyro = gyroInfos[i].gyro;
    if(!gyro.Enabled) {
      TerminalBlockExtentions.ApplyAction(gyro,"OnOff_On");
    }
  }
  return true;
}

bool BatteryIsCharging(IMyBatteryBlock batt) {
    var builder = new StringBuilder();
    batt.GetActionWithName("Recharge").WriteValue(batt, builder);

    return builder.ToString() == "On";
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

public struct GyroInfo {
  public float max;
  public IMyGyro gyro;
  public Matrix shipToGyro;

  public GyroInfo(IMyGyro gyro) {
    this.gyro = gyro;

    Matrix idToGyro;
    gyro.Orientation.GetMatrix(out idToGyro);
    shipToGyro = shipOrient * Matrix.Transpose(idToGyro);

    // Yup, we assume max/min is same in each direction.
    this.max = gyro.GetMaximum<float>("Yaw");
  }
}

// Since our indicator vectors are at a 90 degree angle, e.g. down and forward, our target vectors should also be.
public static Vector3 GetClosest90DegreeVector(Vector3 mainTarget, Vector3 secondaryTarget) {
  var perp = Vector3.Cross(mainTarget, secondaryTarget);
  perp.Normalize();
  return Vector3.Cross(perp, mainTarget);
}

// Get the rotation vector and angle for the shortest rotation between the two given vectors.
public static void GetChangeInDirection(Vector3 indicator, Vector3 target, out Vector3 rotationAxis, out float angle) {
  rotationAxis = Vector3.Cross(indicator, target);
  double dot = Vector3.Dot(indicator, target);
  float magnitude = rotationAxis.Normalize();
  // Supposedly more numerically stable than (float)Math.Asin(magnitude);
  angle = (float)Math.Atan2((double)magnitude, Math.Sqrt(Math.Max(0.0, 1.0 - magnitude * magnitude)));
  if(dot < 0) {
    angle = 180.0f - (float)dot;
  }
}

// All HAIL JoeTheDestroyer!
public static void GetChangeInPose(Vector3 indicator1, Vector3 indicator2, Vector3 target1, Vector3 target2, out Vector3 rotationAxis, out float angle) {
  // Figure out the rotation needed to point indicator1 to target1.
  Vector3 rotationAxis1;
  float rotationAngle1;
  GetChangeInDirection(indicator1, target1, out rotationAxis1, out rotationAngle1);
  Quaternion rotation1 = Quaternion.CreateFromAxisAngle(rotationAxis1, rotationAngle1);

  // Figure out the rotation needed to point indicator2 (after the first rotation) to target2.
  Vector3 rotationAxis2;
  float rotationAngle2;
  GetChangeInDirection(Vector3.Transform(indicator2, rotation1), target2, out rotationAxis2, out rotationAngle2);
  Quaternion rotation2 = Quaternion.CreateFromAxisAngle(rotationAxis2, rotationAngle2);

  // Multiply the two rotations, and create the result rotation angle and axis.
  Quaternion rotation = rotation2 * rotation1;
  rotation.GetAxisAngle(out rotationAxis, out angle);
}

public string ToString(Vector3 value) {
  return String.Format("{0:F3}, {1:F3}, {2:F3}", value.X, value.Y, value.Z);
}

public string ToGpsString(string name, Vector3 value) {
  return String.Format("GPS:{0}:{1:F2}:{2:F2}:{3:F2}:", name, value.X, value.Y, value.Z);
}

public string ToString(Vector3I value) {
  return String.Format("{0:D}, {1:D}, {2:D}", value.X, value.Y, value.Z);
}

public string ToString(Vector3D value) {
  return String.Format("{0:F3}, {1:F3}, {2:F3}", value.X, value.Y, value.Z);
}

public string ToGpsString(string name, Vector3D value){
  return String.Format("GPS:{0}:{1:F2}:{2:F2}:{3:F2}:", name, value.X, value.Y, value.Z);
}

public class MyPid {

  private readonly double Kp;
  private readonly double Ki;
  private readonly double Kd;
  private Vector3D minOut;
  private Vector3D maxOut;
  private Vector3D minI;
  private Vector3D maxI;

  private Vector3D previousError;
  private Vector3D integral;

  public MyPid(double Kp, double Ki, double Kd, double outMax) {
    this.Kp = Kp;
    this.Ki = Ki;
    this.Kd = Kd;
    this.minOut = new Vector3D(-outMax);
    this.maxOut = new Vector3D(outMax);
    this.minI = new Vector3D(-outMax) / Ki;
    this.maxI = new Vector3D(outMax) / Ki;
    this.previousError = new Vector3D(0.0, 0.0, 0.0);
    this.integral = new Vector3D(0.0, 0.0, 0.0);
  }

  public void step(ref Vector3D processVariable, ref Vector3D setPoint, double deltaTime, out Vector3D controlVariable) {
    Vector3D error = setPoint - processVariable;
    integral = integral + error * deltaTime;
    Vector3D.Clamp(ref integral, ref minI, ref maxI, out integral);
    Vector3D derivative = (error - previousError) / deltaTime;
    controlVariable = Kp * error + Ki * integral + Kd * derivative;
    Vector3D.Clamp(ref controlVariable, ref minOut, ref maxOut, out controlVariable);
    previousError = error;
  }

  public Vector3D getPreviousError() {
    return previousError;
  }

  public Vector3D getIntegral() {
    return integral;
  }
}

// Use ILSpy to get API's
// https://gist.github.com/ZerothAngel/da177f8a02347ac252b9
// http://forum.keenswh.com/threads/aligning-ship-to-planet-gravity.7373513/
// http://forum.keenswh.com/threads/ingame-programming-missing-api-and-functions-and-known-issues.7358476/
// https://en.wikipedia.org/wiki/PID_controller
// http://forum.unity3d.com/threads/spaceship-control-using-pid-controllers.191755/
// http://brettbeauregard.com/blog/2011/04/improving-the-beginner%E2%80%99s-pid-reset-windup/
// http://forum.keenswh.com/threads/rotation-script.7376458/
// https://github.com/Sibz/YASEL/blob/master/YASEL/Extensions/GyroExtensions.cs
// http://forum.keenswh.com/threads/gravity-aware-rotation-solved.7376549/

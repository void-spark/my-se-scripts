const string PREFIX = "AutoGyro";

float CTRL_COEFF = 0.8f; //Set lower if overshooting, set higher to respond quicker
float ANGLE_OK = (float)MathHelper.ToRadians(0.01f);

List<GyroInfo> gyroInfos;

bool set;
IMyTextPanel myLcd;
IMyTimerBlock myTimer;
IMyRemoteControl myRemote;

// Directions in ship coords, each with length 1.
Vector3D up;
Vector3D down;
Vector3D left;
Vector3D right;
Vector3D forward;
Vector3D backward;

Dictionary<String, Vector3> angles;

Nullable<Vector3> currentAngle;
String currentAngleName;

MatrixD toWorldRot;
MatrixD toShipRot;
static Matrix shipOrient;

void Main(string argument) {
  if( set == false ) {
    angles = new Dictionary<String, Vector3>();
    angles.Add("TEST", toDirection("GPS:Cranphin #4:-43336.87:-20003.61:37755.51:",
      "GPS:Cranphin #5:-43316.64:-20015.97:37772.22:"));

    Setup();
    set = true;
  }

  if(argument == "RESET") {
    currentAngle = null;
	currentAngleName = "NONE";
  } else if(argument != null && angles.ContainsKey(argument) ) {
    currentAngle = angles[argument];
	currentAngleName = argument;
  }

  Print("-- " + PREFIX + " -- ", false);
  Print("Timer: " + (myTimer != null) + ", Remote: " + (myRemote != null), true );
  Print("Gyros: " + gyroInfos.Count, true );
  Print("Current target heading " + currentAngleName, true );

  toWorldRot = myRemote.WorldMatrix.GetOrientation();
  toShipRot = MatrixD.Transpose(toWorldRot);

  Vector3 grav=myRemote.GetNaturalGravity();
  controlGyros(grav, currentAngle);

  if(!myTimer.Enabled) {
    gyroInfos.ForEach(gyroInfo => gyroInfo.gyro.SetValueBool("Override", false));
    Print("Stopped", true);
  } else {
    TerminalBlockExtentions.ApplyAction(myTimer,"TriggerNow");
  }
}

public static Vector3 toDirection(String from, String to) {
   Vector3 result = toVector(to) - toVector(from);
   result.Normalize();
   return result;
}

public static Vector3 toVector(String gps) {
  String[] parts = gps.Split(':');
  return new Vector3(float.Parse(parts[2]), float.Parse(parts[3]), float.Parse(parts[4]));
}

public void controlGyros(Vector3 targetDown, Nullable<Vector3> targetForward) {

  Vector3 normalizedTargetDown = Vector3.Normalize(targetDown);
  Vector3 localTargetDown = Vector3.Transform(normalizedTargetDown, toShipRot);

  Vector3 localDown = Vector3.Transform(down, Matrix.Transpose(shipOrient));

  Vector3 rot;
  float ang;

  if(targetForward.HasValue) {
    Vector3 normalizedTargetForward = Vector3.Normalize(targetForward.Value);
    Vector3 localTargetForward = Vector3.Transform(normalizedTargetForward, toShipRot);

    Vector3 localForward = Vector3.Transform(forward, Matrix.Transpose(shipOrient));

    Vector3 closestDownDirectionMatch = GetClosest90DegreeVector(localTargetForward, localTargetDown);
    GetChangeInPose(localForward, localDown, localTargetForward, closestDownDirectionMatch, out rot, out ang);
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

public void Setup() {
  myLcd = FindFirstWithPrefix<IMyTextPanel>();
  if(myLcd != null) {
    myLcd.ShowPublicTextOnScreen();
  }
  myTimer = FindFirstWithPrefix<IMyTimerBlock>();
  myRemote = FindFirst<IMyRemoteControl>();

  myRemote.Orientation.GetMatrix(out shipOrient);

  up = shipOrient.Up;
  down = shipOrient.Down;
  left = shipOrient.Left;
  right = shipOrient.Right;
  forward = shipOrient.Forward;
  backward = shipOrient.Backward;

  var l=new List<IMyTerminalBlock>();
  GridTerminalSystem.GetBlocksOfType<IMyGyro>(l,x => x.CubeGrid==Me.CubeGrid);
  List<IMyGyro> gyros=l.ConvertAll(x => (IMyGyro)x);
  gyroInfos = new List<GyroInfo>();
  for (int i = 0; i < gyros.Count; ++i) {
    gyroInfos.Add(new GyroInfo(gyros[i]));
  }

  currentAngle = null;
  currentAngleName = "NONE";
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

public T FindFirst<T>() {
  var list = new List<IMyTerminalBlock>();
  GridTerminalSystem.GetBlocksOfType<T>(list);
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

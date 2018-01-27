﻿int ticks; 
Vector3D position_1; 
Vector3D position_2; 
double speed = 0; 
string history; 
String state; 
 
// Directions in ship coords, each with length 1.
Vector3D up;
Vector3D down;
Vector3D left;
Vector3D right;
Vector3D forward;
Vector3D backward;

List<IMyThrust> thrusters;
Dictionary<Vector3, List<IMyThrust>> _thrusters;

static Matrix shipOrient;

public Program() { 
    ticks = 0; 

state = "";
    Runtime.UpdateFrequency = UpdateFrequency.Update1;
} 

public void Main(string argument, UpdateType updateType) {
    var cockpit = FindFirst<IMyCockpit>(); 
    var Display =  FindFirst<IMyTextPanel>();  

    cockpit.Orientation.GetMatrix(out shipOrient);

    Matrix fromGridToReference;
    cockpit.Orientation.GetMatrix(out fromGridToReference);
    Matrix.Transpose(ref fromGridToReference, out fromGridToReference);

    up = shipOrient.Up;
    down = shipOrient.Down;
    left = shipOrient.Left;
    right = shipOrient.Right;
    forward = shipOrient.Forward;
    backward = shipOrient.Backward;

    List<IMyTerminalBlock> list = new List<IMyTerminalBlock>();
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

    float mass = cockpit.CalculateShipMass().PhysicalMass; 
    Echo("Mass: " + mass); 

    float max_thrust_fw = 0.0f; 
    _thrusters[forward].ForEach(thruster => max_thrust_fw += thruster.MaxEffectiveThrust); 
    Echo("Max fw thrust: " + max_thrust_fw);

    float max_thrust_bw = 0.0f;  
    _thrusters[backward].ForEach(thruster => max_thrust_bw += thruster.MaxEffectiveThrust);  
    Echo("Max bw thrust: " + max_thrust_bw); 


    float target_speed = 10;  

    float burn_time_fw = (int) ((60 * target_speed * mass) / max_thrust_fw) + 1;
    Echo("Time fw: " + burn_time_fw);

    float burn_time_bw = (int) ((60 * target_speed * mass) / max_thrust_bw) + 1; 
    Echo("Time bw: " + burn_time_bw); 

    // recalculate thrust to match exact timing
    float thrust_fw = ((60 * target_speed * mass) / burn_time_fw);  
    Echo("Thrust fw " + thrust_fw); 
    float thrust_percent_fw = 100f * thrust_fw / max_thrust_fw; 
    Echo("Override fw" + thrust_percent_fw); 

    // recalculate thrust to match exact timing 
    float thrust_bw = ((60 * target_speed * mass) / burn_time_bw);   
    Echo("Thrust bw " + thrust_bw);  
    float thrust_percent_bw = 100f * thrust_bw / max_thrust_bw;  
    Echo("Override bw" + thrust_percent_bw);  

    if(argument == "reset"){ 
state = "";
        ticks = 0; 
        _thrusters[forward].ForEach(thruster => thruster.ThrustOverridePercentage = 0.0f);
        _thrusters[backward].ForEach(thruster => thruster.ThrustOverridePercentage = 0.0f);
    }  else {
        ticks++; 
    if(ticks == 60){ 
        state = "Forward thrust";
        _thrusters[forward].ForEach(thruster => thruster.ThrustOverridePercentage = thrust_percent_fw); 
    } 
    if(ticks == (60 + burn_time_fw)){ 
        state = "FW pause"; 
        _thrusters[forward].ForEach(thruster => thruster.ThrustOverridePercentage = 0.0f); 
    }  
    if(ticks == (60 + burn_time_fw + 60)){  
        state = "FW start measure";  
        position_1 = Me.GetPosition(); 
    }  
    if(ticks == (60 + burn_time_fw + 360)){   
        state = "FW measure done";   
        position_2 = Me.GetPosition(); 
        speed = ((position_2-position_1)/5.0).Length();  
        history = "  " + speed + " m/s\n" + history; 
    } 
    if(ticks == (60 + burn_time_fw + 420)){ 
        state = "BREAK"; 
        _thrusters[backward].ForEach(thruster => thruster.ThrustOverridePercentage = thrust_percent_bw); 
    } 
    if(ticks == (60 + burn_time_fw + 420 + burn_time_bw)){ 
        state = "DONE"; 
        _thrusters[backward].ForEach(thruster => thruster.ThrustOverridePercentage = 0.0f); 
    } 
    float calculated_speed = ((thrust_percent_fw * burn_time_fw * max_thrust_fw ) / (mass * 60 * 100)); 
    Display.WritePublicText( state
                                             + "\n  mass: " + mass + " kg" 
                                            + "\n  thrust: " + thrust_fw + " N" 
                                            + "\n  thruster override: " + thrust_percent_fw + " %" 
                                        //    + "\n           thruster: " + thruster_back.GetValueFloat("Override") 
                                            + "\n  time: " + burn_time_fw + " ticks" 
                                            + "\n  measured speed: " + speed + " m/s" 
                                            + "\n  calculated speed:" + calculated_speed + " m/s" 
                                            + "\n  error: " + (100 * (calculated_speed - speed) / calculated_speed) + " %" 
                                            + "\n  history:\n" + history); 
    Display.ShowPublicTextOnScreen(); 
}
}


public T FindFirst<T>() where T: class {
  var list = new List<IMyTerminalBlock>();
  GridTerminalSystem.GetBlocksOfType<T>(list, x => x.CubeGrid == Me.CubeGrid);
  if( list.Count > 0) {
    return (T)list[0];
  }
  return default(T);
}
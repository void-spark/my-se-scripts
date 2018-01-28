// Based on:
// https://www.reddit.com/r/spaceengineers/comments/6glg5i/mass_force_and_acceleration_dont_match_up/

Vector3D position_1;
Vector3D position_2;
double speed = 0;
double speed2 = 0;
double speed3 = 0;
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

// The game ticks since we started, or the last restart.
long ticks;

// The game ticks per second.
long ticksPerSecond = 60;


public Program() {
    ticks = 0;
    state = "";
    Runtime.UpdateFrequency = UpdateFrequency.Update1;
}

public void Main(string argument, UpdateType updateType) {

    // Runtime.TimeSinceLastRun.Ticks returns 160000 for each game tick, which is 16ms according to TimeSpan.
    // But 1/60 of a second would be 16 2/3 ms.
    // Since in this script we only want game ticks, we just divide by 160000.
    ticks += Runtime.TimeSinceLastRun.Ticks / 160000;

    Echo("T:" + ticks.ToString());
    Echo("dT:" + Runtime.TimeSinceLastRun.Ticks);

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

    // Round up, we'll compensate with less thrust.
    int burn_time_fw = (int)Math.Ceiling((ticksPerSecond * target_speed * mass) / max_thrust_fw);
    Echo("Ticks fw: " + burn_time_fw);

    // Round up, we'll compensate with less thrust.
    int burn_time_bw = (int)Math.Ceiling((ticksPerSecond * target_speed * mass) / max_thrust_bw);
    Echo("Ticks bw: " + burn_time_bw);

    // recalculate thrust to match exact timing
    float thrust_fw = ((ticksPerSecond * target_speed * mass) / burn_time_fw);
    Echo("Thrust fw: " + thrust_fw);
    float thrust_percent_fw = thrust_fw / max_thrust_fw;
    Echo("Override fw: " + thrust_percent_fw * 100.0f);

    // recalculate thrust to match exact timing
    float thrust_bw = ((ticksPerSecond * target_speed * mass) / burn_time_bw);
    Echo("Thrust bw: " + thrust_bw);
    float thrust_percent_bw = thrust_bw / max_thrust_bw;
    Echo("Override bw: " + thrust_percent_bw * 100.0f);

    if(argument == "reset"){
        state = "";
        ticks = 0;
        _thrusters[forward].ForEach(thruster => thruster.ThrustOverridePercentage = 0.0f);
        _thrusters[backward].ForEach(thruster => thruster.ThrustOverridePercentage = 0.0f);
        return;
    }

long measureSeconds = 6;
long pausesteps = ticksPerSecond;
    long fwThrustStep = pausesteps;
    long fwThrustStopStep = fwThrustStep + burn_time_fw;
    long fwStartMeasureStep = fwThrustStopStep + pausesteps;
    long fwEndMeasureStep = fwStartMeasureStep + (measureSeconds * ticksPerSecond);
    long bwThrustStep = fwEndMeasureStep + pausesteps;
    long bwThrustStopStep = bwThrustStep + burn_time_bw;

    if(ticks == fwThrustStep){
        state = "Forward thrust";
        cockpit.DampenersOverride = false;
        _thrusters[forward].ForEach(thruster => thruster.ThrustOverridePercentage = thrust_percent_fw);
    } else if(ticks == fwThrustStopStep){
        state = "FW pause";
        _thrusters[forward].ForEach(thruster => thruster.ThrustOverridePercentage = 0.0f);
    } else if(ticks == fwStartMeasureStep){
        state = "FW start measure";
        speed2 = cockpit.GetShipSpeed();
        position_1 = cockpit.GetPosition();
    } else if(ticks == fwEndMeasureStep){
        state = "FW measure done";
        position_2 = cockpit.GetPosition();
        speed = ((position_2-position_1)/measureSeconds).Length();
        speed3 = cockpit.GetShipSpeed();
        history = "  " + speed + " m/s\n" + history;
    } else if(ticks == bwThrustStep){
        state = "BREAK";
        _thrusters[backward].ForEach(thruster => thruster.ThrustOverridePercentage = thrust_percent_bw);
    } else if(ticks == bwThrustStopStep){
        state = "DONE";
        _thrusters[backward].ForEach(thruster => thruster.ThrustOverridePercentage = 0.0f);
        cockpit.DampenersOverride = true;
    }

    float calculated_speed = ((thrust_percent_fw * burn_time_fw * max_thrust_fw ) / (mass * ticksPerSecond));
    Display.WritePublicText( state
        + "\n  mass: " + mass + " kg"
        + "\n  thrust: " + thrust_fw + " N"
        + "\n  thruster override: " + thrust_percent_fw + " %"
        + "\n  time: " + burn_time_fw + " ticks"
        + "\n  measured speed: " + speed + " m/s"
        + "\n  measured speed2: " + speed2 + " m/s"
        + "\n  measured speed3: " + speed3 + " m/s"
        + "\n  calculated speed:" + calculated_speed + " m/s"
        + "\n  error: " + (100.0f * (calculated_speed - speed) / calculated_speed) + " %"
        + "\n  history:\n" + history);
    Display.ShowPublicTextOnScreen();
}

public T FindFirst<T>() where T: class {
    var list = new List<IMyTerminalBlock>();
    GridTerminalSystem.GetBlocksOfType<T>(list, x => x.CubeGrid == Me.CubeGrid);
    if( list.Count > 0) {
        return (T)list[0];
    }
    return default(T);
}

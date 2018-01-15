const string PREFIX = "PhinB";
Color low = new Color(255,255,255);
Color high1 = new Color(255, 0, 0); 
Color high2 = new Color(255, 0, 0);  
float rotation = 0.0f;
IMyTextPanel myLcd;

public struct LightInfo { 
  public float angPos;
  public IMyInteriorLight light;

  public LightInfo(float angPos, IMyInteriorLight light) {
    this.angPos = angPos;
    this.light = light;
  }
}

List<LightInfo> lights;

void Main(string argument) {
  Print( "--start--", false);

  if(myLcd == null) {
    myLcd = FindFirstWithPrefix<IMyTextPanel>();
  }

  PreCalc(argument);

  rotation += 1.0f / 90;
  rotation -= (float)Math.Floor(rotation);

  Print( "Lights: " + lights.Count, true);

  for(var i = 0;i < lights.Count;i++) { 
    IMyInteriorLight light = lights[i].light; 

    float input = rotation + lights[i].angPos;
    input -= (float)Math.Floor(input); 
 
    float reverse = (1.0f -rotation) + lights[i].angPos; 
    reverse -= (float)Math.Floor(reverse);  

    float val = CalcVal(input);
    float val2 = CalcVal(reverse);

    light.SetValue("Color", Color.Lerp(Color.Lerp(low, high1, val), high2, val2));
    light.SetValueFloat( "Intensity",  Math.Min(1.4f + 3.6f * val + 3.6f * val2, 5.0f)); 
  }
  Print( "--end--", true); 
}

public float CalcVal(float input) {
  float val = 0.0f; 
  if(input > 0.25f && input < 0.5f) { 
    val =  (input - 0.25f) * 4.0f; 
  } else if(input > 0.5f && input < 0.75f) { 
    val =  (0.75f - input) * 4.0f;  
  }
  return val;
}

void PreCalc(String argument) {
  if(lights != null) {
    return;
  }
  if(String.IsNullOrEmpty(argument)){ 
    Print("No argument given!", false); 
    return; 
  } 
 
  List<IMyTerminalBlock> blocks; 

  Print("Using group: " + argument, false);  
 
  blocks = GetGroupBlocks(argument); 
 
  Print("Group size: " + blocks.Count, true );   

  float xAcc = 0.0f; 
  int xMin = int.MaxValue; 
  int xMax = int.MinValue; 
 
  float yAcc = 0.0f; 
  int yMin = int.MaxValue; 
  int yMax = int.MinValue; 
 
  float zAcc = 0.0f; 
  int zMin = int.MaxValue; 
  int zMax = int.MinValue; 
 
  lights = new List<LightInfo>();

  for(var i = 0;i < blocks.Count;i++) { 
    Vector3I pos = blocks[i].Position; 
    int x = GetX(pos); 
    int y = GetY(pos);  
    int z = GetZ(pos); 
   
    xAcc += x; 
    yAcc += y; 
    zAcc += z; 
    xMin = Math.Min(xMin,x);
    yMin = Math.Min(yMin,y);
    zMin = Math.Min(zMin,z);
    xMax = Math.Max(xMax,x);
    yMax = Math.Max(yMax,y);
    zMax = Math.Max(zMax,z);
  } 
 
  float xAvg = xAcc / blocks.Count; 
  float yAvg = yAcc / blocks.Count;  
  float zAvg = zAcc / blocks.Count;  
 
  int xSize = Math.Abs(xMax - xMin); 
  int ySize = Math.Abs(yMax - yMin); 
  int zSize = Math.Abs(zMax - zMin);

  Print( "Avg: " + xAvg + ", " + yAvg + ", " + zAvg, true);
  Print( "Size: " + xSize + ", " + ySize + ", " + zSize, true);

  for(var i = 0;i < blocks.Count;i++) {  
    IMyInteriorLight light = (IMyInteriorLight)  blocks[i];  
    Vector3I pos = light.Position;  
    int x = GetX(pos);  
    int y = GetY(pos);   
    int z = GetZ(pos);  
    double angle = Math.Atan2(z - zAvg,x - xAvg); 
    float angpos = (float)((angle + Math.PI) / (2 * Math.PI));
    lights.Add(new LightInfo(angpos,light));
  }
}

public int GetX(Vector3I vInt) { 
  Vector3D vDouble = new Vector3D(vInt); 
  return (int)  Math.Round(vDouble.GetDim(0)); 
} 
 
public int GetY(Vector3I vInt) {  
  Vector3D vDouble = new Vector3D(vInt);  
  return (int)  Math.Round(vDouble.GetDim(1));  
}  
 
public int GetZ(Vector3I vInt) {  
  Vector3D vDouble = new Vector3D(vInt);  
  return (int)  Math.Round(vDouble.GetDim(2));  
} 

IMyBlockGroup GetGroup(string name) {
  var allGroups = new List<IMyBlockGroup>();
  GridTerminalSystem.GetBlockGroups(allGroups);

  for(var i = 0;i<allGroups.Count;i++) {
    if(name == allGroups[i].Name) {
      return allGroups[i];
    }
  }
  return null;
}

List<IMyTerminalBlock> GetGroupBlocks(string name) {
  var group = GetGroup(name);
  return group != null ? group.Blocks : new List<IMyTerminalBlock>();
}

public void Print( string strIn , bool append) { 
  if(myLcd != null) { 
    myLcd.WritePublicText(strIn + "\r\n", append); 
  } 
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

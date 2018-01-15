    const string PREFIX = "Locator";
 
bool set; 
IMyTextPanel myLcd;

void Main(string argument) { 
  	if( set == false ) {  
    		Setup();  
    		set = true;  
  	}
  Print("--GPS Coords--", false); 

  List<IMyTerminalBlock> list = new List<IMyTerminalBlock>(); 
  GridTerminalSystem.GetBlocksOfType<IMyShipConnector>(list);    
  for(int pos = 0; pos < list.Count; pos++) {    
    IMyTerminalBlock block = list[pos];
    Print(GetGpsPos(block).ToGpsString(block.CustomName), true );     
  }      
}

public void Setup() { 
  myLcd = FindFirstWithPrefix<IMyTextPanel>(); 
  myLcd.ShowPublicTextOnScreen(); 
} 

public void Print( string strIn , bool append) {  
  if(myLcd != null) {  
    myLcd.WritePublicText(strIn + "\r\n", append);  
  }  
} 
 
public T FindFirstWithPrefix<T>() where T: class {  
  return FindFirstWithPrefix<T>(PREFIX); 
} 

public T FindFirstWithPrefix<T>(String prefix) where T: class {  
  var list = new List<IMyTerminalBlock>();   
  GridTerminalSystem.GetBlocksOfType<T>(list);  
  for(int pos = 0; pos < list.Count; pos++) {  
    if(list[pos].CustomName.StartsWith(prefix)) {  
      return (T)list[pos];  
    }  
  }  
  return default(T);  
}

public GridPos GetGridPos(IMyTerminalBlock block) {  
  Vector3D vDouble = new Vector3D(block.Position);    
  int x = (int)  Math.Round(vDouble.GetDim(0)); 
  int y = (int)  Math.Round(vDouble.GetDim(1)); 
  int z = (int)  Math.Round(vDouble.GetDim(2)); 
  return new GridPos(x,y,z); 
}  
 
public GpsPos GetGpsPos(IMyTerminalBlock block) {  
  Vector3D vector3D = block.CubeGrid.GridIntegerToWorld(block.Position + new Vector3I(0 ,1 ,0));  
  double x = vector3D.GetDim(0);  
  double y = vector3D.GetDim(1);  
  double z = vector3D.GetDim(2); 
  return new GpsPos(x,y,z); 
} 

public struct GridPos {     
  int x;       
  int y;       
  int z;       
       
  public GridPos(int x, int y, int z) {      
    this.x = x;      
    this.y = y;      
    this.z = z;      
  }      
     
  public override string ToString(){      
    return String.Format("{0:D}, {1:D}, {2:D}", x, y, z);      
  }     
}   
   
public struct GpsPos {     
  double x;      
  double y;      
  double z;      
      
  public GpsPos(double x, double y, double z) {     
    this.x = x;     
    this.y = y;     
    this.z = z;     
  }     
  public override string ToString(){     
    return String.Format("{0:F2}, {1:F2}, {2:F2}", x, y, z);     
  }     

  public String ToGpsString(String name){
    return String.Format("GPS:{0}:{1:F2}:{2:F2}:{3:F2}:", name, x, y, z);      
  }      
}  


void Main(string argument)
{
  // Sadly, no armor blocks are mentioned.
  IMyCubeGrid grid = Me.CubeGrid; 
  GridPos min = new GridPos(grid.Min);  
  GridPos max = new GridPos(grid.Max); 
  int count = 0; 
  float mass = 0.0f;
  // HashSet on block works just as well..
  Dictionary<Vector3I, IMySlimBlock> blocks = new  Dictionary<Vector3I, IMySlimBlock> ();
  for(int x = min.x; x <= max.x; x++) { 
    for(int y = min.y; y <= max.y; y++) {  
      for(int z = min.z; z <= max.z; z++) { 
        IMySlimBlock slim = grid.GetCubeBlock(new Vector3I(x,y,z)); 
        if(slim != null) {
          if(blocks.ContainsKey(slim.Position)) {
            continue;
          }
          blocks.Add(slim.Position, slim);
          count++;  
          mass += slim.Mass; 
        } 
      }   
    }  
  }
  Echo("Min: " + min + ", Max: " + max); 
  Echo("CNT: " + count +"="+ blocks.Count + ", MASS: " + mass);
}

public struct GridPos { 
  public int x; 
  public int y; 
  public int z; 
 
  public GridPos(Vector3I v) { 
    Vector3D vDouble = new Vector3D(v); 
    x = (int)  Math.Round(vDouble.GetDim(0)); 
    y = (int)  Math.Round(vDouble.GetDim(1)); 
    z = (int)  Math.Round(vDouble.GetDim(2)); 
  } 
 
  public GridPos(int x, int y, int z) { 
    this.x = x; 
    this.y = y; 
    this.z = z; 
  } 
 
  public override string ToString(){ 
    return String.Format("{0:D}, {1:D}, {2:D}", x, y, z); 
  } 
}
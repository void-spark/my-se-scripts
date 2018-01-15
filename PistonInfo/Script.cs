const string PANEL_NAME = "LCD Control Status Left";    
const string CONTAINER_NAME = "weldPiston_L_A";   

IMyTextPanel myLcd;

// find and remove prefix?
// weldPiston_L_A
// weldPiston_L_A

const int PANEL_LINES = 22;   
int lineOffset = 0;  

void Main()   
{   
    try {  
        myLcd = FindFirstWithPrefix<IMyTextPanel>(PANEL_NAME);
        Print("Status", false);
        Print("------------------------------------------------------", true); 

        Dictionary<String,float> consolidated = new Dictionary<String,float>();
        List<IMyTerminalBlock> pistonList = new List<IMyTerminalBlock>(); 
        GridTerminalSystem.GetBlocksOfType<IMyPistonBase>(pistonList);    
        for (int i = 0; i < pistonList.Count; i++) {     
        if (pistonList[i] is IMyPistonBase) {  
            IMyPistonBase  piston= (IMyPistonBase)pistonList[i];
// float MinLimit  
// float MaxLimit
            Print(piston.CustomName + " - Att: " + getAttached(piston) +
                     ", Pos: " + getPosition(piston) +
                     ", Vel: " + Math.Round(piston.Velocity,2).ToString("###,###,##0.00")
                     , true); 

            float amount = piston.Velocity;  
            consolidated.Add(piston.CustomName, amount);  
        }  
    }  
    List<String> list = new List<String>();  
    var enumerator = consolidated.GetEnumerator();  
    while (enumerator.MoveNext())  
    	{
        var pair = enumerator.Current;      
        String itemKey = pair.Key;  
        float itemValue = pair.Value;  
          
        list.Add(itemKey + " - " + itemValue);  
    }       
    list.Sort();  
    list.Insert(0,"------------------------------------------------------");  
    list.Insert(0,"Status");  
    for (int o=0; o < lineOffset; o++) {  
        String shiftedItem = list[0];  
        list.RemoveAt(0);  
        list.Add(shiftedItem);  
    }  
    //myLcd.WritePublicText(String.Join("\n",list.ToArray()), false);  
   
    myLcd.ShowTextureOnScreen();    
    myLcd.ShowPublicTextOnScreen();   
    if (list.Count > PANEL_LINES) {  
        lineOffset++;  
        if (list.Count - lineOffset < PANEL_LINES) {  
           lineOffset = 0;  
        }  
    }  
    } catch (Exception ex) {  
        throw new Exception(ex.StackTrace);  
    }  
}   

public bool getAttached( IMyPistonBase piston ) {
  String line = piston.DetailedInfo.Split(new string[] { "\r\n" }, StringSplitOptions.None  )[0];
  return line == "Attached";
} 

public float getPosition(  IMyPistonBase piston ) {  
  String line = piston.DetailedInfo.Split(new string[] { "\r\n" }, StringSplitOptions.None  )[1]; 
  	string[] dataSplit = line.Split( ':' );
  	return float.Parse( dataSplit[1].Substring( 0, dataSplit[1].Length-1 ) );  
}  
 
public void Print( string strIn , bool append) {  
  if(myLcd != null) {  
    myLcd.WritePublicText(strIn + "\r\n", append);  
  }  
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


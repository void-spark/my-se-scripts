﻿const string PANEL_NAME = "LCD Control Status Right";   
const string CONTAINER_NAME = "LargeCargo_Welder";  
const int PANEL_LINES = 22;  
int lineOffset = 0; 
 
void Main()  
{  
    try { 
    List<IMyTerminalBlock> work = new List<IMyTerminalBlock>();  
    Dictionary<String,float> consolidated = new Dictionary<String,float>(); 
    GridTerminalSystem.SearchBlocksOfName(PANEL_NAME, work);       
    IMyTextPanel panel = null; 
    for (int i = 0; i < work.Count; i++)    
    {    
        if (work[i] is IMyTextPanel) { 
           panel = (IMyTextPanel)work[i];   
           break; 
        } 
    } 
    List<IMyTerminalBlock> containerList = new List<IMyTerminalBlock>();  
    GridTerminalSystem.SearchBlocksOfName(CONTAINER_NAME, containerList);       
 
    for (int i = 0; i < containerList.Count; i++)    
    {    
        if (containerList[i] is IMyCargoContainer) { 
            var containerInvOwner = containerList[i] as IMyInventoryOwner; 
            var containerItems = containerInvOwner.GetInventory(0).GetItems();    
            for(int j = containerItems.Count - 1; j >= 0; j--)     
            {     
                String itemName = decodeItemName(containerItems[j].Content.SubtypeName,  
                                  containerItems[j].Content.TypeId.ToString()) + "|" +  
                                  containerItems[j].Content.TypeId.ToString(); 
                float amount = (float)containerItems[j].Amount; 
                if (!consolidated.ContainsKey(itemName)) { 
                   consolidated.Add(itemName, amount); 
                } else { 
                   consolidated[itemName] += amount; 
                } 
            }   
        } 
    } 
    List<String> list = new List<String>(); 
    var enumerator = consolidated.GetEnumerator(); 
    while (enumerator.MoveNext()) 
	{       
        var pair = enumerator.Current; 
        String itemKey = pair.Key; 
        float itemValue = pair.Value; 
         
        String txt = itemKey.Split('|')[0] + "  -  ";  
        String amt = amountFormatter(itemValue,itemKey.Split('|')[1]);  
        txt += amt;  
        list.Add(txt); 
    }      
    list.Sort(); 
    list.Insert(0,"------------------------------------------------------"); 
    list.Insert(0,CONTAINER_NAME + " Inventory"); 
    for (int o=0; o < lineOffset; o++) { 
        String shiftedItem = list[0]; 
        list.RemoveAt(0); 
        list.Add(shiftedItem); 
    } 
    panel.WritePublicText(String.Join("\n",list.ToArray()), false); 
  
    panel.ShowTextureOnScreen();   
    panel.ShowPublicTextOnScreen();  
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
  
String amountFormatter(float amt, String typeId) {  
    if (typeId.EndsWith("_Ore") || typeId.EndsWith("_Ingot")) { 
        if (amt > 1000.0f) { 
          return "" + Math.Round((float)amt/1000,2).ToString("###,###,##0.00") + "K";  
        } else { 
          return "" + Math.Round((float)amt,2).ToString("###,###,##0.00");  
        } 
    } 
    return "" + Math.Round((float)amt,0).ToString("###,###,##0");  
}  
  
String decodeItemName(String name, String typeId)   
{  
    if (name.Equals("Construction")) { return "Construction Component"; }  
    if (name.Equals("MetalGrid")) { return "Metal Grid"; }  
    if (name.Equals("InteriorPlate")) { return "Interior Plate"; }  
    if (name.Equals("SteelPlate")) { return "Steel Plate"; }  
    if (name.Equals("SmallTube")) { return "Small Steel Tube"; }  
    if (name.Equals("LargeTube")) { return "Large Steel Tube"; }  
    if (name.Equals("BulletproofGlass")) { return "Bulletproof Glass"; }  
    if (name.Equals("Reactor")) { return "Reactor Component"; }  
    if (name.Equals("Thrust")) { return "Thruster Component"; }  
    if (name.Equals("GravityGenerator")) { return "GravGen Component"; }  
    if (name.Equals("Medical")) { return "Medical Component"; }  
    if (name.Equals("RadioCommunication")) { return "Radio Component"; }  
    if (name.Equals("Detector")) { return "Detector Component"; }  
    if (name.Equals("SolarCell")) { return "Solar Cell"; }  
    if (name.Equals("PowerCell")) { return "Power Cell"; }  
    if (name.Equals("AutomaticRifleItem")) { return "Rifle"; }  
    if (name.Equals("AutomaticRocketLauncher")) { return "Rocket Launcher"; }  
    if (name.Equals("WelderItem")) { return "Welder"; }  
    if (name.Equals("AngleGrinderItem")) { return "Grinder"; }  
    if (name.Equals("HandDrillItem")) { return "Hand Drill"; }  
    if (typeId.EndsWith("_Ore")) { 
        if (name.Equals("Stone")) { 
            return name; 
        } 
        return name + " Ore"; 
    } 
    if (typeId.EndsWith("_Ingot")) { 
        if (name.Equals("Stone")) { 
            return "Gravel"; 
        } 
        if (name.Equals("Magnesium")) { 
            return name + " Powder"; 
        } 
        if (name.Equals("Silicon")) { 
            return name + " Wafer"; 
        } 
       return name + " Ingot"; 
    } 
    return name;  
}
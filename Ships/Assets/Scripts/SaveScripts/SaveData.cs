using Newtonsoft.Json;
using System;

public class SaveData
{
    public string uniqueID;
    public string username;

    public SaveData()
    {
        AssignUniqueId();
        username = null;
    }

    [JsonConstructor]
    public SaveData(string uniqueID, string username)
    {
        this.uniqueID = uniqueID;
        this.username = username;
    }

    public void AssignUniqueId()
    {
        uniqueID = Guid.NewGuid().ToString();
    }

    public void UpdateUsername(string newUsername)
    {
        username = newUsername;
        Save.SaveMyData();
    }

    public override string ToString()
    {
        string returnString = "Username: " + username + "\n" + "Unique ID: " + uniqueID + "\n";
        return returnString;
    }
}

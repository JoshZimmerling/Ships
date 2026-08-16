using Newtonsoft.Json;
using System;

public class SaveData
{
    public string uniqueID;
    public string username;

    public SaveData()
    {
        uniqueID = Guid.NewGuid().ToString();
        username = null;
    }

    [JsonConstructor]
    public SaveData(string uniqueID, string username)
    {
        this.uniqueID = uniqueID;
        this.username = username;
    }

    public override string ToString()
    {
        string returnString = "Username: " + username + "\n";
        return returnString;
    }
}

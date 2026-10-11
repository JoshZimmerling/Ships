using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class Factions : Singleton<Factions>
{
    [SerializeField] public List<Faction> factionList;
}

[System.Serializable]
public class Faction
{
    public string factionName;
    public string factionDescription;
    public NetworkPrefabsList factionShipList;
}

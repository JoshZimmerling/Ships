using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class GameSettingsManager : NetworkBehaviour
{
    private NetworkVariable<int> mapIndex = new NetworkVariable<int>(0, writePerm: NetworkVariableWritePermission.Server);
    public NetworkPrefabsList mapsList;
    private List<int> mapMaxPlayers = new List<int>();
    private List<Sprite> mapSprites = new List<Sprite>();

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public override void OnNetworkSpawn()
    {
        foreach (NetworkPrefab map in mapsList.PrefabList)
        {
            mapSprites.Add(map.Prefab.GetComponent<Image>().sprite);

            //This will loop through all objects in the map that are spawn zones to count how many max players there are for each map
            int numSpawnZones = 0;
            foreach (Transform mothershipSpawn in map.Prefab.transform.Find("Mothership Spawn Positions").GetComponentsInChildren<Transform>())
                if (mothershipSpawn.name.Contains("Spawn Zone"))
                    numSpawnZones++;
            mapMaxPlayers.Add(numSpawnZones);
        }
    }

    public void NextMap()
    {
        mapIndex.Value++;
        if (mapIndex.Value >= mapsList.PrefabList.Count) mapIndex.Value = 0;
    }

    public void LastMap()
    {
        mapIndex.Value--;
        if (mapIndex.Value < 0) mapIndex.Value = mapsList.PrefabList.Count - 1;
    }

    public GameObject GetSelectedMapPrefab()
    {
        return mapsList.PrefabList[mapIndex.Value].Prefab;
    }

    public int GetSelectedMapMaxPlayers()
    {
        return mapMaxPlayers[mapIndex.Value];
    }

    public Sprite GetSelectedMapImage()
    {
        return mapSprites[mapIndex.Value];
    }
}

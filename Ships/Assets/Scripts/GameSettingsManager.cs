using NUnit.Framework;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class GameSettingsManager : NetworkBehaviour
{
    public NetworkVariable<int> mapIndex = new NetworkVariable<int>(0, writePerm: NetworkVariableWritePermission.Server);
    public NetworkPrefabsList mapsList;
    public List<Sprite> mapSprites = new List<Sprite>();

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public override void OnNetworkSpawn()
    {
        foreach (NetworkPrefab map in mapsList.PrefabList)
        {
            mapSprites.Add(map.Prefab.GetComponent<Image>().sprite);
        }
    }

    public void NextMap()
    {
        mapIndex.Value = mapIndex.Value + 1;
        if (mapIndex.Value >= mapsList.PrefabList.Count) mapIndex.Value = 0;
    }

    public void LastMap()
    {
        mapIndex.Value = mapIndex.Value - 1;
        if (mapIndex.Value <= 0) mapIndex.Value = mapsList.PrefabList.Count;
    }
}

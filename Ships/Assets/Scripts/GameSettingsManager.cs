using Unity.Netcode;
using UnityEngine;

public class GameSettingsManager : NetworkBehaviour
{
    public NetworkVariable<int> mapIndex = new NetworkVariable<int>(0, writePerm: NetworkVariableWritePermission.Server);
    public NetworkPrefabsList mapsList;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public override void OnNetworkSpawn()
    {
        if (!IsHost) return;
        foreach (NetworkPrefab map in mapsList.PrefabList)
        {
            GameObject obj = Instantiate(map.Prefab);
            obj.GetComponent<NetworkObject>().Spawn();
            obj.transform.parent = transform;
            obj.SetActive(false);
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

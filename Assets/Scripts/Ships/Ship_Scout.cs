using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Ship_Scout : Ship
{
    [SerializeField] GameObject scoutRadar;
    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        if (IsOwner)
        {
            scoutRadar.SetActive(true);
        }
    }
}

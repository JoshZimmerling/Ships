using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Ship_Goliath : Ship
{
    [SerializeField] float hps;
    public void FixedUpdate()
    {
        currentShipHP.Value += hps * Time.deltaTime;
        if (currentShipHP.Value > maxShipHP)
            currentShipHP.Value = maxShipHP;
    }
}

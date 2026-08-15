using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Shooting_Drone : Shooting
{
    int bulletsShotCounter_Drone = 0;
    protected override void ShootBullet()
    {
        bulletsShotCounter_Drone++;

        if (bulletsShotCounter_Drone == 4)
        {
            bulletsShotCounter_Drone = 0;
        }

        CreateBullet(bulletSpawnerList[bulletsShotCounter_Drone]);
    }
}

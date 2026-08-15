using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Shooting_Challenger : Shooting
{
    private float maxBulletLifetime;
    protected override void ShootBullet()
    {
        foreach (BulletSpawner bulletSpawner in bulletSpawnerList)
        {
            maxBulletLifetime = bulletLifetime;
            for (bulletLifetime = maxBulletLifetime; bulletLifetime > 0.1f; bulletLifetime -= maxBulletLifetime / 6)
                CreateBullet(bulletSpawner);
            bulletLifetime = maxBulletLifetime;
        }
    }
}

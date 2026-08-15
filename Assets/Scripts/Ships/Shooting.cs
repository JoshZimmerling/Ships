using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class Shooting : NetworkBehaviour
{
    // Bullet variables
    [SerializeField] protected float bulletLifetime;
    [SerializeField] private float bulletSpeed;
    [SerializeField] private float bulletsPerSecond;
    [SerializeField] private float bulletDamage;

    [SerializeField] private GameObject BulletPrefab;

    [SerializeField] protected List<BulletSpawner> bulletSpawnerList;

    float counter = 0;
    public void FixedUpdate()
    {
        if (!IsHost) return;

        counter += Time.deltaTime;
        if (counter >= 1 / bulletsPerSecond)
        {
            ShootBullet();
            counter = 0;
        }
    }

    protected virtual void ShootBullet()
    {
        foreach (BulletSpawner bulletSpawner in bulletSpawnerList)
        {
            CreateBullet(bulletSpawner);
        }
    }

    public void CreateBullet(BulletSpawner bulletSpawner)
    {
        GameObject bullet = Instantiate(BulletPrefab, transform.rotation * bulletSpawner.spawnPoint + transform.position, Quaternion.LookRotation(new Vector3(0, 0, 1), transform.rotation * bulletSpawner.shootDirection));
        bullet.GetComponent<NetworkObject>().SpawnWithOwnership(OwnerClientId);
        bullet.GetComponent<Bullet>().SetupBullet(bulletLifetime, bulletDamage, bulletSpeed);
        bullet.transform.parent = GameManager.Singleton.bulletContainer;
    }
}

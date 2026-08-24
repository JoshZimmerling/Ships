using System.Collections.Generic;
using Unity.Netcode;
using Unity.VisualScripting;
using UnityEngine;

public class Turret : NetworkBehaviour
{
    [SerializeField] private GameObject bulletPrefab;

    private enum TurretType
    {
        HeavyTurret,
        MediumTurret,
        LightTurret,
        MissilePods,
        LightningGun,
        ChallengerGun,
        HawkGun
    }

    [SerializeField] private TurretType turretType;
    [SerializeField] private int damage;
    [SerializeField] private float fireRate;
    [SerializeField] private int range;
    [SerializeField] private int aimPoint;
    [SerializeField] private int arcSpread;
    [SerializeField] private int projectileSpeed;
    [SerializeField] private float counter = 0;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public override void OnNetworkSpawn()
    {

    }

    private void FixedUpdate()
    {
        //Shoot the Turrets
        if (!IsHost) return;

        counter -= Time.deltaTime;
        if (counter > 0) return;

        foreach (GameObject ship in GameSceneManager.Singleton.shipsInScene)
        {
            float dSqrToTarget = (ship.transform.position - transform.position).sqrMagnitude;
        }

        /*
        // Find closest
        Transform bestTarget = null;
        float closestDistanceSqr = Mathf.Infinity;

        float maxRadians = (aimPoint + transform.rotation.eulerAngles.z) * Mathf.Deg2Rad;
        Vector2 perpFiringAngle = Vector2.Perpendicular(new Vector2(Mathf.Cos(maxRadians), Mathf.Sin(maxRadians)));

        foreach (GameObject enemyShip in GameSceneManager.Singleton.shipsInScene)
        {
            if (enemyShip.GetComponent<Ship>().OwnerClientId == OwnerClientId) continue; // Is owned by me
            int corrVal = enemyShip.GetComponent<Ship>().correctionFactor;

            Vector2 delta = enemyShip.transform.position - transform.position;
            float dSqrToTarget = delta.sqrMagnitude;
            if (dSqrToTarget > (range + corrVal) * (range + corrVal)) continue; // Is out of range

            float angle = (Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg + 360) % 360;

            Vector2 delta1 = delta + perpFiringAngle * corrVal;
            float angle1 = (Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg + 360) % 360;
            Vector2 delta2 = delta - perpFiringAngle * corrVal;
            float angle2 = (Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg + 360) % 360;

            float shipRotationZ = gameObject.transform.rotation.eulerAngles.z;
            float max = (aimPoint + arcSpread / 2 + shipRotationZ + 360) % 360;
            float min = (aimPoint - arcSpread / 2 + shipRotationZ + 360) % 360;

            if ((min < max ? min > angle || angle > max : angle > max && angle < min) &&
                (min < max ? min > angle1 || angle1 > max : angle1 > max && angle1 < min) &&
                (min < max ? min > angle2 || angle2 > max : angle2 > max && angle2 < min))
                continue; //Out of angle

            // Finds the closest
        }

        if (bestTarget != null)
        {
            // Determine future position
            float timeToTarget = Mathf.Sqrt(closestDistanceSqr) / projectileSpeed;
            Vector2 targetedPos = new Vector2(bestTarget.transform.position.x, bestTarget.transform.position.y);// + bestTarget.GetComponent<Movement>().GetFuturePosition(timeToTarget); //TODO: FIX
            }
        }

        // Determine future position
            GameObject bullet = Instantiate(bulletPrefab, transform.position, Quaternion.LookRotation(new Vector3(0, 0, 1), shootDirection));
            bullet.GetComponent<NetworkObject>().SpawnWithOwnership(OwnerClientId);
            bullet.GetComponent<Bullet>().SetupBullet(range / projectileSpeed, damage, projectileSpeed);
            bullet.transform.parent = GameSceneManager.Singleton.bulletContainer;
            Vector2 shootDirection = targetedPos - new Vector2(transform.position.x, transform.position.y);
            counter = 1 / fireRate;
        }
    }

    private void OnDrawGizmos()
    {
        //if (!IsLocalPlayer) return;

        float shipRotationZ = gameObject.transform.rotation.eulerAngles.z;

        Gizmos.color = Color.whiteSmoke;
        float aim = aimPoint + shipRotationZ;
        float maxRadians = aim * Mathf.Deg2Rad;
        //Gizmos.DrawLine(transform.position, transform.position + new Vector3(Mathf.Cos(maxRadians), Mathf.Sin(maxRadians)) * range);

        switch (turretType)
        {
            case TurretType.HeavyTurret:
                Gizmos.color = Color.red;
                break;
            case TurretType.MediumTurret:
                Gizmos.color = Color.yellow;
                break;
            case TurretType.LightTurret:
                Gizmos.color = Color.green;
                break;
        }

        float max = aimPoint + arcSpread / 2 + shipRotationZ;
        maxRadians = max * Mathf.Deg2Rad;
        Gizmos.DrawLine(transform.position, transform.position + new Vector3(Mathf.Cos(maxRadians), Mathf.Sin(maxRadians)) * range);

        float min = aimPoint - arcSpread / 2 + shipRotationZ;
        float minRadians = min * Mathf.Deg2Rad;
        Gizmos.DrawLine(transform.position, transform.position + new Vector3(Mathf.Cos(minRadians), Mathf.Sin(minRadians)) * range);

        for (float a = min; a < max; a += 5)
        {
            minRadians = a * Mathf.Deg2Rad;
            maxRadians = (a + 5) * Mathf.Deg2Rad;
            Gizmos.DrawLine(transform.position + new Vector3(Mathf.Cos(minRadians), Mathf.Sin(minRadians)) * range, transform.position + new Vector3(Mathf.Cos(maxRadians), Mathf.Sin(maxRadians)) * range);
        }
        bullet.GetComponent<Bullet>().SetupBullet(range / projectileSpeed, damage, projectileSpeed);
        bullet.transform.parent = GameSceneManager.Singleton.bulletContainer;

        counter = 1 / fireRate;
        
        */
    }
}

using System.Collections.Generic;
using Unity.Netcode;
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
    [SerializeField] private int projectileSpeed;
    [SerializeField] private float counter = 0;

    private Collider2D targettingCollider;
    //private List<Ship> targets = new List<Ship>();
    private List<Transform> targets = new List<Transform>();

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public override void OnNetworkSpawn()
    {
        targettingCollider = GetComponent<Collider2D>();
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!IsHost) return;
        
        Ship s = collision.gameObject.GetComponent<Ship>();
        if (s != null && s.OwnerClientId != OwnerClientId)
                targets.Add(collision.transform);
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (!IsHost) return;

        Ship s = collision.gameObject.GetComponent<Ship>();
        if (s != null && s.OwnerClientId != OwnerClientId)
           targets.Remove(collision.transform);
    }

    private void FixedUpdate()
    {
        //Shoot the Turrets
        if (!IsHost) return;

        counter -= Time.deltaTime;
        if (counter > 0 || targets.Count == 0) return;

        // Find closest
        Transform bestTarget = null;
        float closestDistanceSqr = Mathf.Infinity;
        Vector3 currentPosition = transform.position;
        foreach (Transform potentialTarget in targets)
        {
            Vector3 directionToTarget = potentialTarget.transform.position - currentPosition;
            float dSqrToTarget = directionToTarget.sqrMagnitude;
            if (dSqrToTarget < closestDistanceSqr)
            {
                closestDistanceSqr = dSqrToTarget;
                bestTarget = potentialTarget;
            }
        }
        // Determine future position
        float timeToTarget = Mathf.Sqrt(closestDistanceSqr) / projectileSpeed;
        Vector2 targetedPos = new Vector2(bestTarget.transform.position.x, bestTarget.transform.position.y);// + bestTarget.GetComponent<Movement>().GetFuturePosition(timeToTarget); //TODO: FIX

        // calculate shootDirection
        Vector2 shootDirection = targetedPos - new Vector2(transform.position.x, transform.position.y);

        GameObject bullet = Instantiate(bulletPrefab, transform.position, Quaternion.LookRotation(new Vector3(0, 0, 1), shootDirection));
        bullet.GetComponent<NetworkObject>().SpawnWithOwnership(OwnerClientId);
        bullet.GetComponent<Bullet>().SetupBullet(range / projectileSpeed, damage, projectileSpeed);
        bullet.transform.parent = GameManager.Singleton.bulletContainer;

        counter = 1 / fireRate;
    }
}

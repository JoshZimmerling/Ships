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


    // Find closest
    Transform bestTarget;
    float closestShipDistance;


    float maxRadians;
    Vector2 fireAngle;

    float distanceToTarget;

    private void FixedUpdate()
    {
        //Shoot the Turrets
        if (!IsHost) return;

        counter -= Time.deltaTime;
        if (counter > 0) return;


        bestTarget = null;
        closestShipDistance = Mathf.Infinity;

        maxRadians = (aimPoint + transform.rotation.eulerAngles.z) * Mathf.Deg2Rad;
        fireAngle = new Vector2(Mathf.Cos(maxRadians), Mathf.Sin(maxRadians));

        foreach (GameObject enemyShip in GameSceneManager.Singleton.shipsInScene)
        {
            // Finds the closest
            if (IsValidTarget(enemyShip) && closestShipDistance > distanceToTarget)
            {
                closestShipDistance = distanceToTarget;
                bestTarget = enemyShip.transform;
            }
        }

        if (bestTarget != null)
        {
            // Determine future position
            float timeToTarget = closestShipDistance / projectileSpeed;
            Vector2 targetedPos = new Vector2(bestTarget.transform.position.x, bestTarget.transform.position.y);// + bestTarget.GetComponent<Movement>().GetFuturePosition(timeToTarget); //TODO: FIX

            // Determine the shot direction
            Vector2 shootDirection = (targetedPos - new Vector2(transform.position.x, transform.position.y)).normalized;

            //Clamping shot angle to inside the bounds of our spread
            if (arcSpread != 360)
            {
                Vector2 leftEdgeVector = RotateVector(fireAngle, arcSpread * 0.5f).normalized;
                Vector2 rightEdgeVector = RotateVector(fireAngle, -arcSpread * 0.5f).normalized;

                if (leftEdgeVector.x < rightEdgeVector.x)
                    shootDirection.x = Mathf.Clamp(shootDirection.x, leftEdgeVector.x, rightEdgeVector.x);
                else
                    shootDirection.x = Mathf.Clamp(shootDirection.x, rightEdgeVector.x, leftEdgeVector.x);

                if (leftEdgeVector.y < rightEdgeVector.y)
                    shootDirection.y = Mathf.Clamp(shootDirection.y, leftEdgeVector.y, rightEdgeVector.y);
                else
                    shootDirection.y = Mathf.Clamp(shootDirection.y, rightEdgeVector.y, leftEdgeVector.y);
            }

            //Fire the bullet at the angle calculated
            GameObject bullet = Instantiate(bulletPrefab, transform.position, Quaternion.LookRotation(new Vector3(0, 0, 1), shootDirection));
            bullet.GetComponent<NetworkObject>().SpawnWithOwnership(OwnerClientId);
            bullet.GetComponent<Bullet>().SetupBullet(range / projectileSpeed, damage, projectileSpeed);
            bullet.transform.parent = GameSceneManager.Singleton.bulletContainer;
            counter = 1 / fireRate;
        }
    }

    private bool IsValidTarget(GameObject enemyShip)
    {
        if (enemyShip.GetComponent<Ship>().OwnerClientId == OwnerClientId) return false; // Is owned by me

        int corrVal = enemyShip.GetComponent<Ship>().correctionFactor;
        Vector2 delta = enemyShip.transform.position - transform.position;
        distanceToTarget = delta.magnitude - corrVal;
        if (distanceToTarget > range) return false; // Is out of range     d^2 > (r+c)^2     = d > r+c = (d-c) > r = (d-c)^2 > r^2

        if (arcSpread == 360) return true;

        float dot = Vector2.Dot(delta.normalized, fireAngle);
        float halfAngleRad = (arcSpread * 0.5f) * Mathf.Deg2Rad;
        float cosHalfAngle = Mathf.Cos(halfAngleRad);
        if (dot >= cosHalfAngle && distanceToTarget <= range) return true; // Center of ship in sector (extended)

        Vector2 leftEdgeDir = RotateVector(fireAngle, arcSpread * 0.5f).normalized;
        Vector2 rightEdgeDir = RotateVector(fireAngle, -arcSpread * 0.5f).normalized;

        if (LineSegmentIntersectsCircle(transform.position, (Vector2)transform.position + leftEdgeDir * range, enemyShip.transform.position, corrVal)) return true;
        if (LineSegmentIntersectsCircle(transform.position, (Vector2)transform.position + rightEdgeDir * range, enemyShip.transform.position, corrVal)) return true;

        return false;
    }

    private Vector2 RotateVector(Vector2 vector, float degrees)
    {
        float sin = Mathf.Sin(degrees * Mathf.Deg2Rad);
        float cos = Mathf.Cos(degrees * Mathf.Deg2Rad);
        return new Vector2(
            vector.x * cos - vector.y * sin,
            vector.x * sin + vector.y * cos
        );
    }

    private static bool LineSegmentIntersectsCircle(Vector2 start, Vector2 end, Vector2 circleCenter, float radius)
    {
        Vector2 segment = end - start;
        Vector2 toCircle = circleCenter - start;

        // Project toCircle onto segment to find the closest point parameter 't'
        float segLengthSq = segment.sqrMagnitude;
        if (segLengthSq == 0) return toCircle.sqrMagnitude <= radius * radius;

        float t = Vector2.Dot(toCircle, segment) / segLengthSq;
        t = Mathf.Clamp01(t); // Clamp to restrict to the finite line segment length

        // Find the closest point on the segment to the circle center
        Vector2 closestPoint = start + t * segment;

        // If distance to closest point is less than radius, it intersects
        return (circleCenter - closestPoint).sqrMagnitude <= radius * radius;
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
    }
}

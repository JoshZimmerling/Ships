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
    float closestDistanceSqr;


    float maxRadians;
    Vector2 fireAngle;

    float dSqrToTarget;

    private void FixedUpdate()
    {
        //Shoot the Turrets
        if (!IsHost) return;

        counter -= Time.deltaTime;
        if (counter > 0) return;


        bestTarget = null;
        closestDistanceSqr = Mathf.Infinity;

        maxRadians = (aimPoint + transform.rotation.eulerAngles.z) * Mathf.Deg2Rad;
        fireAngle = new Vector2(Mathf.Cos(maxRadians), Mathf.Sin(maxRadians));

        foreach (GameObject enemyShip in GameSceneManager.Singleton.shipsInScene)
        {
            // Finds the closest
            if (IsValidTarget(enemyShip) && closestDistanceSqr > dSqrToTarget)
            {
                closestDistanceSqr = dSqrToTarget;
                bestTarget = enemyShip.transform;
            }
        }

        if (bestTarget != null)
        {
            // Determine future position
            float timeToTarget = Mathf.Sqrt(closestDistanceSqr) / projectileSpeed;
            Vector2 targetedPos = new Vector2(bestTarget.transform.position.x, bestTarget.transform.position.y);// + bestTarget.GetComponent<Movement>().GetFuturePosition(timeToTarget); //TODO: FIX

            // Determine future position
            Vector2 shootDirection = targetedPos - new Vector2(transform.position.x, transform.position.y);



            /*
            // Clamp to angles
            float angle = Mathf.Atan2(shootDirection.y, shootDirection.x) * Mathf.Rad2Deg;
            float minAngle = aimPoint - arcSpread * 0.5f;
            float maxAngle = aimPoint + arcSpread * 0.5f;
            float angleDiff = Mathf.DeltaAngle(minAngle, angle);
            float maxDiff = Mathf.DeltaAngle(minAngle, maxAngle);

            // Check if the angle falls within the allowed span
            if (angleDiff < 0 || angleDiff > maxDiff)
                if (Mathf.Abs(angleDiff) < Mathf.Abs(Mathf.DeltaAngle(maxAngle, angle)))
                    angle = minAngle;
                else
                    angle = minAngle;


            angle = angle * Mathf.Deg2Rad;
            shootDirection = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
            */

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
        dSqrToTarget = delta.sqrMagnitude;
        if (dSqrToTarget > (range + corrVal) * (range + corrVal)) return false; // Is out of range

        float dot = Vector2.Dot(delta.normalized, fireAngle);
        float halfAngleRad = (arcSpread * 0.5f) * Mathf.Deg2Rad;
        float cosHalfAngle = Mathf.Cos(halfAngleRad);
        if (dot >= cosHalfAngle && dSqrToTarget <= (range + corrVal) * (range + corrVal)) return true; // Center of ship in sector (extended)

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

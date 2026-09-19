using Unity.Netcode;
using Unity.VisualScripting;
using UnityEngine;
using static UnityEngine.GraphicsBuffer;

public class Turret : NetworkBehaviour
{
    [SerializeField] private GameObject bulletPrefab;
    [SerializeField] private GameObject missilePrefab;

    public enum TurretType
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

    [SerializeField] private int aimDirection;
    [SerializeField] private int firingArc;
    [SerializeField] private int firingSpread;

    [SerializeField] private int projectileSpeed;
    [SerializeField] private float missileTurningSpeed = 60f;
    [SerializeField] private float missileLifetimeMax = 8f;
    [SerializeField] private float counter = 0;

    private int rangeMod = 1;

    private Movement mv;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public override void OnNetworkSpawn()
    {
        mv = GetComponentInParent<Movement>();
    }

    // Find closest
    Transform bestTarget;
    float bestTargetDistance;

    float maxRadians;
    Vector2 fireVector;

    float distanceToTarget;

    private void FixedUpdate()
    {
        //Shoot the Turrets
        if (!IsHost) return;

        counter -= Time.deltaTime;
        if (counter > 0) return;


        bestTarget = null;
        bestTargetDistance = Mathf.Infinity;

        maxRadians = (aimDirection + transform.rotation.eulerAngles.z) * Mathf.Deg2Rad;
        fireVector = new Vector2(Mathf.Cos(maxRadians), Mathf.Sin(maxRadians));

        rangeMod = (turretType == TurretType.HawkGun && !mv.moving ? 2 : 1);

        foreach (GameObject enemyShip in GameSceneManager.Singleton.shipsInScene)
        {
            // Finds the closest ship
            if (IsValidTarget(enemyShip) && bestTargetDistance > distanceToTarget)
            {
                bestTargetDistance = distanceToTarget;
                bestTarget = enemyShip.transform;
            }
        }

        // If it is not a missile turret and there isnt a ship targeted or if it is a light turret look to target missiles
        if (turretType != TurretType.MissilePods && (bestTarget == null || turretType == TurretType.LightTurret))
        {
            foreach (GameObject enemyMissiles in GameSceneManager.Singleton.missilesInScene)
            {
                // Look to see if there are any missiles in range and targets them if they are the closest (or if there isnt anything targeted yet)
                if (IsValidTarget(enemyMissiles) && (bestTarget == null || bestTargetDistance > distanceToTarget))
                {
                    bestTargetDistance = distanceToTarget;
                    bestTarget = enemyMissiles.transform;
                }
            }
        }

        if (bestTarget != null)
        {
            if (turretType == TurretType.MissilePods)
            {
                // Fire the missile
                GameObject missile = Instantiate(missilePrefab, transform.position, Quaternion.Euler(0, 0, aimDirection - 90 + transform.rotation.eulerAngles.z));
                GameSceneManager.Singleton.missilesInScene.Add(missile);
                missile.GetComponent<NetworkObject>().SpawnWithOwnership(OwnerClientId);
                missile.GetComponent<Missile>().SetupMissile(damage, projectileSpeed, missileTurningSpeed, missileLifetimeMax, bestTarget, transform.parent.parent.GetComponent<NeutralShip>() != null);
                missile.transform.parent = GameSceneManager.Singleton.bulletContainer;
            }
            else
            {
                // Determine future position
                float timeToTarget = bestTargetDistance / projectileSpeed;
                Vector2 targetedPos = Vector2.zero;
                if (bestTarget.GetComponent<Fighter>() != null)
                {
                    targetedPos = Vector2.Lerp(new Vector2(bestTarget.transform.position.x, bestTarget.transform.position.y), bestTarget.GetComponent<Fighter>().GetFuturePosition(timeToTarget), .8f);
                }
                else if (bestTarget.GetComponent<Ship>() != null)
                {
                    targetedPos = Vector2.Lerp(new Vector2(bestTarget.transform.position.x, bestTarget.transform.position.y), bestTarget.GetComponent<Movement>().GetFuturePosition(timeToTarget), .8f); //This somewhat leads the ship, but not fully
                }
                else if (bestTarget.GetComponent<NeutralShip>() != null)
                {
                    targetedPos = Vector2.Lerp(new Vector2(bestTarget.transform.position.x, bestTarget.transform.position.y), bestTarget.GetComponent<Movement>().GetFuturePosition(timeToTarget), .8f); //This somewhat leads the ship, but not fully
                }
                else if (bestTarget.GetComponent<Missile>() != null)
                {
                    targetedPos = bestTarget.position;
                }

                // Determine the shot direction
                Vector2 shootDirection = (targetedPos - new Vector2(transform.position.x, transform.position.y));
                float angleDiff = Vector2.SignedAngle(shootDirection, fireVector);

                // Clamping shot angle to inside the bounds of our spread
                if (firingArc != 360 && Mathf.Abs(angleDiff) > firingArc / 2)
                    angleDiff = firingArc / 2 * Mathf.Sign(angleDiff);

                // Calculate final fiire angle
                float fireAngle = (aimDirection + transform.rotation.eulerAngles.z - angleDiff + Random.Range(-firingSpread / 2, firingSpread / 2)) * Mathf.Deg2Rad;
                fireVector = new Vector2(Mathf.Cos(fireAngle), Mathf.Sin(fireAngle));

                // Fire the bullet at the angle calculated
                GameObject bullet = Instantiate(bulletPrefab, transform.position, Quaternion.LookRotation(new Vector3(0, 0, 1), fireVector));
                bullet.GetComponent<NetworkObject>().SpawnWithOwnership(OwnerClientId);
                bullet.GetComponent<Bullet>().SetupBullet(range * rangeMod, damage, projectileSpeed * rangeMod, turretType, transform.parent.parent.GetComponent<NeutralShip>() != null);
                bullet.transform.parent = GameSceneManager.Singleton.bulletContainer;

                // Play sound for firing bullet
                PlayShootingAudioRPC();
            }
            counter = Random.Range(0.95f, 1.05f) / fireRate;
        }
    }

    private bool IsValidTarget(GameObject target)
    {
        int corrVal = 0;

        if (target.GetComponent<Ship>() != null)
        {
            if (target.GetComponent<Ship>().OwnerClientId == OwnerClientId && !transform.parent.parent.GetComponent<NeutralShip>()) return false; // Is owned by me
            corrVal = target.GetComponent<Ship>().correctionFactor;
        }
        else if (target.GetComponent<NeutralShip>() != null)
        {
            if (transform.parent.parent.GetComponent<NeutralShip>()) return false; // Neutral ships should not shoot other neutral ships
            corrVal = target.GetComponent<NeutralShip>().correctionFactor;
        }
        else if (target.GetComponent<Missile>() != null)
        {
            if ((target.GetComponent<Missile>().OwnerClientId == OwnerClientId && !target.GetComponent<Missile>().isFromNeutralShip && !transform.parent.parent.GetComponent<NeutralShip>()) || (transform.parent.parent.GetComponent<NeutralShip>() && target.GetComponent<Missile>().isFromNeutralShip)) return false; // Is owned by me
        }

        Vector2 delta = target.transform.position - transform.position;
        distanceToTarget = delta.magnitude - corrVal;

        if (distanceToTarget > range * rangeMod) return false; // Is out of range

        if (firingArc == 360) return true;

        if (turretType == TurretType.HawkGun && !mv.moving) // Checks if in vision for hawk gun
        {
            bool inRange = false;
            foreach (GameObject ship in GameSceneManager.Singleton.shipsInScene)
            {
                Ship alliedShip = ship.GetComponent<Ship>();
                if (alliedShip != null && alliedShip.OwnerClientId == OwnerClientId && (alliedShip.transform.position - target.transform.position).magnitude < alliedShip.visionRange)
                {
                    inRange = true;
                    break;
                }
            }
            if (!inRange) return false;
        }

        float dot = Vector2.Dot(delta.normalized, fireVector);
        float halfAngleRad = (firingArc * 0.5f) * Mathf.Deg2Rad;
        float cosHalfAngle = Mathf.Cos(halfAngleRad);
        if (dot >= cosHalfAngle && distanceToTarget <= range * rangeMod) return true; // Center of target in sector (extended)

        Vector2 leftEdgeDir = RotateVector(fireVector, firingArc * 0.5f).normalized;
        Vector2 rightEdgeDir = RotateVector(fireVector, -firingArc * 0.5f).normalized;

        if (LineSegmentIntersectsCircle(transform.position, (Vector2)transform.position + leftEdgeDir * range * rangeMod, target.transform.position, corrVal)) return true;
        if (LineSegmentIntersectsCircle(transform.position, (Vector2)transform.position + rightEdgeDir * range * rangeMod, target.transform.position, corrVal)) return true;

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

    [Rpc(SendTo.ClientsAndHost)]
    public void PlayShootingAudioRPC()
    {
        Transform thisTurretsShip = transform.parent.parent;
        if (Camera_Control.Singleton.IsOnScreen(thisTurretsShip))
        {
            bool isSeenByMyShips = false;
            foreach (Transform ship in PlayerDataList.Singleton.GetLocalPlayer().transform)
            {
                Ship myShip = ship.GetComponent<Ship>();
                if ((myShip.transform.position - thisTurretsShip.position).magnitude < myShip.visionRange)
                {
                    isSeenByMyShips = true;
                    break;
                }
            }
            if (isSeenByMyShips)
                if (GetComponent<AudioSource>() != null)
                    GetComponent<AudioSource>().Play();
        }
    }

    private void OnDrawGizmos()
    {
        float shipRotationZ = gameObject.transform.rotation.eulerAngles.z;

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
            case TurretType.MissilePods:
                Gizmos.color = Color.blue;
                float aimDirectionRadians = (aimDirection + shipRotationZ) * Mathf.Deg2Rad;
                Gizmos.DrawLine(transform.position, transform.position + new Vector3(Mathf.Cos(aimDirectionRadians), Mathf.Sin(aimDirectionRadians)) * range * 0.2f);
                break;
        }

        float max = aimDirection + firingArc / 2 + shipRotationZ;
        float maxRadians = max * Mathf.Deg2Rad;
        float min = aimDirection - firingArc / 2 + shipRotationZ;
        float minRadians = min * Mathf.Deg2Rad;
        if (firingArc < 360)
        {
            Gizmos.DrawLine(transform.position + new Vector3(Mathf.Cos(maxRadians), Mathf.Sin(maxRadians)) * range * rangeMod * 0.9f, transform.position + new Vector3(Mathf.Cos(maxRadians), Mathf.Sin(maxRadians)) * range * rangeMod);
            Gizmos.DrawLine(transform.position + new Vector3(Mathf.Cos(minRadians), Mathf.Sin(minRadians)) * range * rangeMod * 0.9f, transform.position + new Vector3(Mathf.Cos(minRadians), Mathf.Sin(minRadians)) * range * rangeMod);
        }

        for (float a = min; a < max - 1; a += 5)
        {
            minRadians = a * Mathf.Deg2Rad;
            maxRadians = (a + 5) * Mathf.Deg2Rad;
            Gizmos.DrawLine(transform.position + new Vector3(Mathf.Cos(minRadians), Mathf.Sin(minRadians)) * range * rangeMod, transform.position + new Vector3(Mathf.Cos(maxRadians), Mathf.Sin(maxRadians)) * range * rangeMod);
        }

        // Draw spread
        /*
        Gizmos.color = Color.white;

        max = aimDirection + firingSpread / 2 + shipRotationZ;
        maxRadians = max * Mathf.Deg2Rad;
        Gizmos.DrawLine(transform.position, transform.position + new Vector3(Mathf.Cos(maxRadians), Mathf.Sin(maxRadians)) * 10);
        min = aimDirection - firingSpread / 2 + shipRotationZ;
        minRadians = min * Mathf.Deg2Rad;
        Gizmos.DrawLine(transform.position, transform.position + new Vector3(Mathf.Cos(minRadians), Mathf.Sin(minRadians)) * 10);

        for (float a = min; a < max; a += 1)
        {
            minRadians = a * Mathf.Deg2Rad;
            maxRadians = (a + 1) * Mathf.Deg2Rad;
            Gizmos.DrawLine(transform.position + new Vector3(Mathf.Cos(minRadians), Mathf.Sin(minRadians)) * 10, transform.position + new Vector3(Mathf.Cos(maxRadians), Mathf.Sin(maxRadians)) * 10);
        }
        */
    }
}

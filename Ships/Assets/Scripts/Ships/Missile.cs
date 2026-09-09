using Unity.Netcode;
using UnityEngine;

public class Missile : NetworkBehaviour
{
    float dmg;
    float bulletSpeed;
    float missileTurnRate;
    [SerializeField] float missileLifetimeMax;
    float missileLifetime;

    Transform missileTarget;

    public override void OnNetworkSpawn()
    {
        GetComponent<SpriteRenderer>().color = PlayerDataList.Singleton.players[OwnerClientId].playerColor;
        missileLifetime = missileLifetimeMax;
    }

    void FixedUpdate()
    {
        if (!IsHost) return;

        missileLifetime -= Time.deltaTime;
        if (missileLifetime <= 0 )
        {
            DestroyMissile();
            return;
        }

        if (missileTarget != null)
        {
            Vector2 direction = missileTarget.position - transform.position;
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            Quaternion targetRotation = Quaternion.Euler(0, 0, angle - 90);
            transform.rotation = Quaternion.RotateTowards(
                transform.rotation,
                targetRotation,
                missileTurnRate * Time.deltaTime
            );
        }

        transform.Translate(Vector2.up * bulletSpeed * Mathf.Pow(missileLifetime / missileLifetimeMax, 1/2) * Time.deltaTime);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!IsHost) return;
        
        if (collision.GetComponent<Ship>() != null)
        { 
            if (collision.GetComponent<Ship>().OwnerClientId == this.OwnerClientId)
                return;
            else
                collision.GetComponent<Ship>().DoDamage(dmg);
        }

        if (collision.GetComponent<NeutralShip>() != null)
        {
            collision.GetComponent<NeutralShip>().DoDamage(dmg, this.OwnerClientId);
        }

        if (collision.GetComponent<Missile>() != null)
        {
            if (collision.GetComponent<Missile>().OwnerClientId == this.OwnerClientId)
                return;
        }

        if (collision.GetComponent<Bullet>() != null)
        {
            if (collision.GetComponent<Bullet>().OwnerClientId == this.OwnerClientId && !collision.GetComponent<Bullet>().isFromNeutralShip)
                return;
        }

        DestroyMissile();
    }

    /*
    public Vector2 GetFuturePosition(float seconds)
    {
        //TODO: MIGHT NEED FIX
        return transform.position + transform.rotation * Vector2.up * bulletSpeed * seconds;
    }
    */

    public void SetupMissile(float damage, float speed, float turnRate, Transform target)
    {
        dmg = damage;
        bulletSpeed = speed;
        missileTurnRate = turnRate;
        missileTarget = target;
    }

    public void DestroyMissile()
    {
        if (!IsHost) return;

        GameSceneManager.Singleton.missilesInScene.Remove(gameObject);
        GetComponent<NetworkObject>().Despawn();
        Destroy(this);
    }
}

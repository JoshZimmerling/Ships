using Unity.Netcode;
using UnityEngine;

public class Missile : NetworkBehaviour
{
    float dmg;
    float bulletSpeed;
    float missileTurnRate;

    Transform missileTarget;

    public override void OnNetworkSpawn()
    {
        GetComponent<SpriteRenderer>().color = PlayerDataList.Singleton.players[OwnerClientId].playerColor;
    }

    void FixedUpdate()
    {
        if (!IsHost) return;
        
        if (missileTarget == null) DestroyMissile();

        Vector2 direction = missileTarget.position - transform.position;
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        Quaternion targetRotation = Quaternion.Euler(0, 0, angle - 90);

        float degreesOff = Quaternion.Angle(transform.rotation, targetRotation);
        if (degreesOff > 70) //Rotate faster if we are far off of our target
        {
            transform.rotation = Quaternion.RotateTowards(
                transform.rotation,
                targetRotation,
                missileTurnRate * 5 * Time.deltaTime
            );
            transform.Translate(Vector2.up * bulletSpeed * .5f * Time.deltaTime);
        }
        else
        {
            transform.rotation = Quaternion.RotateTowards(
                transform.rotation,
                targetRotation,
                missileTurnRate * Time.deltaTime
            );
            transform.Translate(Vector2.up * bulletSpeed * Time.deltaTime);
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!IsHost) return;
        
        if (collision.gameObject.layer == LayerMask.NameToLayer("Ship"))
        {
            if (collision.GetComponent<Ship>().OwnerClientId == this.OwnerClientId)
                return;
            else
                collision.GetComponent<Ship>().DoDamage(dmg);
        }

        if (collision.gameObject.layer == LayerMask.NameToLayer("Missile"))
        {
            if (collision.GetComponent<Missile>().OwnerClientId == this.OwnerClientId)
                return;
        }

        if (collision.gameObject.layer == LayerMask.NameToLayer("Bullet"))
        {
            if (collision.GetComponent<Bullet>().OwnerClientId == this.OwnerClientId)
                return;
        }

        DestroyMissile();
    }

    public Vector2 GetFuturePosition(float seconds)
    {
        //TODO: MIGHT NEED FIX
        return transform.position + transform.rotation * Vector2.up * bulletSpeed * seconds;
    }

    public void SetupMissile(float damage, float speed, float turnRate, Transform target)
    {
        dmg = damage;
        bulletSpeed = speed;
        missileTurnRate = turnRate;
        missileTarget = target;
        transform.localScale = new Vector3(.75f, .75f, .75f);

        //Set initial rotation
        Vector2 direction = missileTarget.position - transform.position;
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0, 0, angle + (Random.Range(0,2) * 180)); //Will randomly start either directly left or directly right of where we are aiming
    }

    public void DestroyMissile()
    {
        if (!IsHost) return;

        GameSceneManager.Singleton.missilesInScene.Remove(gameObject);
        GetComponent<NetworkObject>().Despawn();
        Destroy(this);
    }
}

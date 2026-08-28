using Unity.Netcode;
using UnityEngine;

public class Missile : NetworkBehaviour
{
    float dmg;
    float bulletSpeed;

    Transform missileTarget;

    public override void OnNetworkSpawn()
    {
        GetComponent<SpriteRenderer>().color = PlayerDataList.Singleton.players[OwnerClientId].playerColor;
    }

    void FixedUpdate()
    {
        if (!IsHost || missileTarget == null) return;
        
        Vector2 direction = missileTarget.position - transform.position;
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0, 0, angle - 90);

        transform.Translate(new Vector2(0, 1) * bulletSpeed * Time.deltaTime);
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
            else
                collision.GetComponent<Missile>().DestroyMissile();
        }

        if (collision.gameObject.layer == LayerMask.NameToLayer("Bullet"))
        {
            if (collision.GetComponent<Bullet>().OwnerClientId == this.OwnerClientId)
                return;
        }

        DestroyMissile();
    }

    public void SetupMissile(float damage, float speed, Transform target)
    {
        dmg = damage;
        bulletSpeed = speed;
        missileTarget = target;
        transform.localScale = new Vector3(.75f, .75f, .75f);
    }

    public void DestroyMissile()
    {
        GetComponent<NetworkObject>().Despawn();
        Destroy(gameObject);
    }
}

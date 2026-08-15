using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class Bullet : NetworkBehaviour
{
    float dmg;
    float maxbulletLifetime;
    float bulletSpeed;

    public override void OnNetworkSpawn()
    {
        GetComponent<SpriteRenderer>().color = GameManager.Singleton.playerColors[OwnerClientId];
    }

    float bulletLifetime = 0;
    void FixedUpdate()
    {
        if (!IsHost) return;

        transform.Translate(new Vector2(0, 1) * bulletSpeed * Time.deltaTime);
        bulletLifetime += Time.deltaTime;

        if (bulletLifetime > maxbulletLifetime)
        {
            GetComponent<NetworkObject>().Despawn();
            Destroy(this);
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!IsHost) return;

        if (collision.gameObject.layer == LayerMask.NameToLayer("Ship"))
            if (collision.GetComponent<Ship>().OwnerClientId == this.OwnerClientId)
                return;
            else
                collision.GetComponent<Ship>().DoDamage(dmg);

        GetComponent<NetworkObject>().Despawn();
        Destroy(this.gameObject);
    }

    public void SetupBullet(float lifetime, float damage, float speed)
    {
        maxbulletLifetime = lifetime;
        dmg = damage;
        bulletSpeed = speed;
    }
    /*
    [ClientRpc]
    public void SetupBulletClientRPC(float lifetime, float damage, float speed)
    {
        maxbulletLifetime = lifetime;
        dmg = damage;
        bulletSpeed = speed;
    }
    */
}

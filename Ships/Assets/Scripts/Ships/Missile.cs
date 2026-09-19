using Unity.Netcode;
using UnityEngine;

public class Missile : NetworkBehaviour
{
    float dmg;
    float bulletSpeed;
    float missileTurnRate;
    float missileLifetime = 1f; //Temporary to cause it to not despawn while getting setup
    float missileLifetimeMax = 1f; //Temporary to cause it to not despawn while getting setup

    Transform missileTarget;

    public bool isFromNeutralShip = false;
    private Color neutralShipColor = new Color(212 / 255f, 175 / 255f, 55 / 255f);

    public override void OnNetworkSpawn()
    {
        GetComponent<SpriteRenderer>().color = PlayerDataList.Singleton.players[OwnerClientId].playerColor;
        if (!IsOwner) transform.Find("Fog Remover").gameObject.SetActive(false);
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

        if (collision.gameObject.name == "Challenger Shield")
        {
            if (collision.transform.parent.GetComponent<Ship>().OwnerClientId == this.OwnerClientId && !isFromNeutralShip)
                return;
        }
        else if (collision.GetComponent<Ship>() != null)
        { 
            if (collision.GetComponent<Ship>().OwnerClientId == this.OwnerClientId && !isFromNeutralShip)
                return;
            else
                collision.GetComponent<Ship>().DoDamage(dmg);
        }
        else if (collision.GetComponent<NeutralShip>() != null)
        {
            if (isFromNeutralShip)
                return;
            else
                collision.GetComponent<NeutralShip>().DoDamage(dmg, this.OwnerClientId);
        }
        else if (collision.GetComponent<Missile>() != null)
        {
            if ((collision.GetComponent<Missile>().OwnerClientId == this.OwnerClientId && !collision.GetComponent<Missile>().isFromNeutralShip && !isFromNeutralShip) || (isFromNeutralShip && collision.GetComponent<Missile>().isFromNeutralShip))
                return;
        }
        else if (collision.GetComponent<Bullet>() != null)
        {
            if ((collision.GetComponent<Bullet>().OwnerClientId == this.OwnerClientId && !collision.GetComponent<Bullet>().isFromNeutralShip && !isFromNeutralShip) || (isFromNeutralShip && collision.GetComponent<Bullet>().isFromNeutralShip))
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

    public void SetupMissile(float damage, float speed, float turnRate, float lifetime, Transform target, bool neutralShip)
    {
        dmg = damage;
        bulletSpeed = speed;
        missileTurnRate = turnRate;
        missileLifetime = lifetime;
        missileLifetimeMax = lifetime;
        missileTarget = target;
        isFromNeutralShip = neutralShip;

        if (isFromNeutralShip)
        {
            SetMissileColorRPC(neutralShipColor);
            transform.Find("Fog Remover").gameObject.SetActive(false);
        }
        else
            SetMissileColorRPC(PlayerDataList.Singleton.players[OwnerClientId].playerColor);
    }

    [Rpc(SendTo.ClientsAndHost)]
    public void SetMissileColorRPC(Color bulletColor)
    {
        GetComponent<SpriteRenderer>().color = bulletColor;
    }

    public void DestroyMissile()
    {
        if (!IsHost) return;

        GameSceneManager.Singleton.missilesInScene.Remove(gameObject);
        GetComponent<NetworkObject>().Despawn();
        Destroy(this);
    }
}

using Unity.Netcode;
using UnityEngine;

public class Bullet : NetworkBehaviour
{
    float dmg;
    float bulletSpeed;

    Vector2 spawnPos;
    float sqrRange;
    Turret.TurretType turretType;
    public GameObject parentShip;

    public bool isFromNeutralShip = false;
    private Color neutralShipColor = new Color(212/255f, 175/255f, 55/255f);

    void FixedUpdate()
    {
        if (!IsHost || spawnPos == null) return;
        
        transform.Translate(new Vector2(0, 1) * bulletSpeed * Time.deltaTime);        
        if (Vector2.SqrMagnitude(spawnPos - (Vector2)transform.position) > sqrRange)
        {
            GetComponent<NetworkObject>().Despawn();
            Destroy(this);
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!IsHost) return;

        if (collision.gameObject.name == "Challenger Shield")
        {
            if(collision.transform.parent.GetComponent<Ship>().OwnerClientId == this.OwnerClientId && !isFromNeutralShip)
                return;
        }
        else if (collision.GetComponent<Ship>() != null)
        {
            if (collision.GetComponent<Ship>().OwnerClientId == this.OwnerClientId && !isFromNeutralShip)
                return;
            else
                collision.GetComponent<Ship>().DoDamage(dmg, parentShip);
        }
        else if (collision.GetComponent<NeutralShip>() != null)
        {
            if (isFromNeutralShip)
                return;
            collision.GetComponent<NeutralShip>().DoDamage(dmg, parentShip);
        }
        else if (collision.GetComponent<Missile>() != null)
        {
            if ((collision.GetComponent<Missile>().OwnerClientId == this.OwnerClientId && !collision.GetComponent<Missile>().isFromNeutralShip && !isFromNeutralShip) || (isFromNeutralShip && collision.GetComponent<Missile>().isFromNeutralShip))
                return;
        }

        GetComponent<NetworkObject>().Despawn();
        Destroy(this);
    }

    public void SetupBullet(float range, float damage, float speed, Turret.TurretType type, GameObject shipWhoShot)
    {
        spawnPos = transform.position;
        dmg = damage;
        range = range + 5;
        sqrRange = range * range;
        bulletSpeed = speed;
        turretType = type;
        parentShip = shipWhoShot;
        isFromNeutralShip = shipWhoShot.GetComponent<NeutralShip>() != null;
        switch (turretType)
        {
            case Turret.TurretType.HeavyTurret:
            case Turret.TurretType.HawkGun:
                transform.localScale = new Vector3(0.6f, 0.6f, 1);
                break;
            case Turret.TurretType.MediumTurret:
                transform.localScale = new Vector3(0.4f, 0.4f, 1);
                break;
            case Turret.TurretType.LightTurret:
                transform.localScale = new Vector3(0.2f, 0.2f, 1);
                break;
        }

        if (isFromNeutralShip)
            SetBulletColorRPC(neutralShipColor);
        else
            SetBulletColorRPC(PlayerDataList.Singleton.players[OwnerClientId].playerColor);
    }

    [Rpc(SendTo.ClientsAndHost)]
    public void SetBulletColorRPC(Color bulletColor)
    {
        GetComponent<SpriteRenderer>().color = bulletColor;
    }
}

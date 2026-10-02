using Unity.Netcode;
using UnityEngine;
using static Ship;

public class Missile : NetworkBehaviour
{
    float dmg;
    float bulletSpeed;
    float missileTurnRate;
    float missileLifetime = 1f; //Temporary to cause it to not despawn while getting setup
    float missileLifetimeMax = 1f; //Temporary to cause it to not despawn while getting setup

    public int visionRange = 7;

    Transform missileTarget;

    public GameObject parentShip;
    public bool isFromNeutralShip = false;
    private Color neutralShipColor = new Color(212 / 255f, 175 / 255f, 55 / 255f);

    [SerializeField] private GameObject explosionPrefab;
    [SerializeField] private GameObject visionPrefab;

    [SerializeField] AudioClip deathSound;
    private AudioSource inFlightAudio;
    private bool audioPlaying = false;

    public override void OnNetworkSpawn()
    {
        //GetComponent<SpriteRenderer>().color = NetworkManager.Singleton.LocalClient.PlayerObject.GetComponent<PlayerData>().playerColor;
        inFlightAudio = GetComponent<AudioSource>();
    }


    void FixedUpdate()
    {
        if (!audioPlaying)
        {
            if (Camera_Control.Singleton.IsOnScreen(transform) && (Camera_Control.Singleton.IsSeenByMyShips(transform) || (IsOwner && !isFromNeutralShip)))
            {
                audioPlaying = true;
                inFlightAudio.UnPause();
            }
        }
        else
        {
            if (!Camera_Control.Singleton.IsOnScreen(transform) || (!Camera_Control.Singleton.IsSeenByMyShips(transform) && (!IsOwner || isFromNeutralShip)))
            {
                audioPlaying = false;
                inFlightAudio.Pause();
            }
        }

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
        else if (collision.GetComponent<NeutralShip>() != null)
        {
            if (isFromNeutralShip)
                return;
            else
                collision.GetComponent<NeutralShip>().DoDamage(dmg, parentShip);
        }
        else if (collision.GetComponent<Ship>() != null)
        { 
            if (collision.GetComponent<Ship>().OwnerClientId == this.OwnerClientId && !isFromNeutralShip)
                return;
            else
                collision.GetComponent<Ship>().DoDamage(dmg, parentShip);
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

    public void SetupMissile(float damage, float speed, float turnRate, float lifetime, Transform target, GameObject shipWhoShot)
    {
        dmg = damage;
        bulletSpeed = speed;
        missileTurnRate = turnRate;
        missileLifetime = lifetime;
        missileLifetimeMax = lifetime;
        missileTarget = target;
        parentShip = shipWhoShot;
        isFromNeutralShip = shipWhoShot.GetComponent<NeutralShip>() != null;

        if (isFromNeutralShip)
        {
            SetMissileColorRPC(neutralShipColor);
        }
        else
            SetMissileColorRPC(NetworkManager.Singleton.LocalClient.PlayerObject.GetComponent<PlayerData>().playerColor);

        inFlightAudio.Play();
        inFlightAudio.Pause();
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

    public override void OnNetworkDespawn()
    {
        GameObject explosion = Instantiate(explosionPrefab, transform.position, Quaternion.identity);
        Destroy(explosion, 3f);

        // Play sound
        if (Camera_Control.Singleton.IsOnScreen(transform) && (Camera_Control.Singleton.IsSeenByMyShips(transform) || (IsOwner && !isFromNeutralShip)) && deathSound != null)
        {
            AudioSource.PlayClipAtPoint(deathSound, Camera.main.transform.position, 0.2f);
        }

        if (IsOwner && !isFromNeutralShip)
        {
            GameObject vision = Instantiate(visionPrefab, transform.position, Quaternion.identity);
            vision.GetComponent<VisionCone>().visionRange = visionRange;
            Destroy(vision, vision.GetComponent<VisionCone>().decayDuration);
        }
        
    }
}

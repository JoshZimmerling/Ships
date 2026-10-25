using System.Collections;
using Unity.Netcode;
using UnityEngine;

public class RaptorAbility : NetworkBehaviour
{
    [SerializeField] private float secondsUntilDestroyAfterHit = 2f;

    private bool hasBeenHit = false;
    private float counter = 0;
    private bool flashSprites = true;

    private void FixedUpdate()
    {
        if (hasBeenHit)
        {
            counter += Time.deltaTime;
            if (counter > 0.25f)
            {
                SetShieldOpacity(flashSprites);
                flashSprites = !flashSprites;
                counter = 0;
            }
        }
    }

    private void SetShieldOpacity(bool flash)
    {
        foreach (SpriteRenderer shieldSprite in GetComponentsInChildren<SpriteRenderer>())
        {
            if (flash)
                shieldSprite.color = new Color(1, 1, 1, 100f / 255f);
            else
                shieldSprite.color = new Color(1, 1, 1, 255f / 255f);
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!IsHost || hasBeenHit) return;

        //If the collision is a bullet or missile and is not from our own ships
        if ((collision.GetComponent<Missile>() != null && (collision.GetComponent<Missile>().OwnerClientId != this.OwnerClientId || collision.GetComponent<Missile>().isFromNeutralShip)) 
            || collision.GetComponent<Bullet>() != null && (collision.GetComponent<Bullet>().OwnerClientId != this.OwnerClientId || collision.GetComponent<Bullet>().isFromNeutralShip))
        {
            hasBeenHit = true;
            StartCoroutine(TurnOffShield());
        }
    }

    private IEnumerator TurnOffShield()
    {
        yield return new WaitForSeconds(secondsUntilDestroyAfterHit);
        gameObject.SetActive(false);
    }
}

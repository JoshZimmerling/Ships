using System.Collections;
using Unity.Netcode;
using UnityEngine;

public class RaptorAbility : NetworkBehaviour
{
    [SerializeField] private float secondsUntilDestroyAfterHit = 2.5f;

    private bool hasBeenHit = false;
    private SpriteRenderer sprite;
    private float counter = 0;
    private bool decrementColor = true;

    private void Start()
    {
        sprite = GetComponent<SpriteRenderer>();
        sprite.color = new Color(1, 1, 1, 100f / 255f);
    }

    private void FixedUpdate()
    {
        if (hasBeenHit)
        {
            counter += Time.deltaTime;
            if (counter > 0.25f)
            {
                if (decrementColor)
                    sprite.color = new Color(1, 1, 1, 50f / 255f);
                else
                    sprite.color = new Color(1, 1, 1, 100f / 255f);

                counter = 0;
                decrementColor = !decrementColor;
            }
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

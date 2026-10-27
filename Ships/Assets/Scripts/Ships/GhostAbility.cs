using Unity.Netcode;
using UnityEngine;
using static UnityEngine.GraphicsBuffer;

public class GhostAbility : NetworkBehaviour
{
    [SerializeField] private float timeToInvis = 1f;
    private float counter = 0f;
    private Ship shipScript;
    private Movement movementScript;
    private GameObject invisRangeIndicator;
    private bool lastRevealed;

    private SpriteRenderer[] spriteRenderers;
    private ParticleSystem[] particleSystems;

    void Start()
    {
        shipScript = gameObject.GetComponent<Ship>();
        movementScript = gameObject.GetComponent<Movement>();

        invisRangeIndicator = transform.Find("InvisRangeIndicator").gameObject;
        invisRangeIndicator.SetActive(false);

        spriteRenderers = GetComponentsInChildren<SpriteRenderer>();
        particleSystems = GetComponentsInChildren<ParticleSystem>();
    }

    void FixedUpdate()
    {
        if (OwnerClientId != NetworkManager.Singleton.LocalClientId)
        {
            bool revealed = false;
            //loop through ships to see if revealed
            if (shipScript.isInvis.Value)
            {
                foreach (Transform t in NetworkManager.Singleton.LocalClient.PlayerObject.transform)
                {
                    Ship ship = t.GetComponent<Ship>();
                    if (shipScript.invisDetectionRange > (transform.position - t.position).magnitude - ship.correctionFactor)
                    {
                        revealed = true;
                        break;
                    }
                }
            }
            else
            {
                revealed = true;
            }

            if (revealed != lastRevealed)
            {
                if (!revealed)
                {
                    foreach (SpriteRenderer sr in spriteRenderers)
                    {
                        sr.enabled = false;
                    }
                    foreach (ParticleSystem ps in particleSystems)
                    {
                        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                    }
                }
                else
                {
                    foreach (SpriteRenderer sr in spriteRenderers)
                    {
                        sr.enabled = true;
                    }
                    foreach (ParticleSystem ps in particleSystems)
                    {
                        ps.Play();
                    }
                }
                lastRevealed = revealed;
            }
        }

        if (!IsHost) return;

        //While we are moving, keep the counter at 0 seconds and if we are invis become visible
        if (movementScript.moving)
        {
            counter = 0;
            if (shipScript.isInvis.Value)
                GoVisible();
        }
        //If we are not moving and not yet invis, count up until we are invis
        else if (!movementScript.moving && !shipScript.isInvis.Value)
        {
            counter += Time.deltaTime;
            if (counter > timeToInvis)
            {
                GoInvisibile();
            }
        }
    }

    private void GoInvisibile()
    {
        shipScript.isInvis.Value = true;
        ShowInvisEffectsRPC();
    }

    private void GoVisible() 
    {
        shipScript.isInvis.Value = false;
        RemoveInvisEffectsRPC();
    }

    [Rpc(SendTo.Owner)]
    public void ShowInvisEffectsRPC()
    {
        shipScript.SetRadarActive(false);
        invisRangeIndicator.SetActive(true);
    }

    [Rpc(SendTo.Owner)]
    public void RemoveInvisEffectsRPC()
    {
        shipScript.SetRadarActive(true);
        invisRangeIndicator.SetActive(false);
    }
}

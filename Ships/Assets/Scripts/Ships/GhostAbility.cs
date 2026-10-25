using Unity.Netcode;
using UnityEngine;

public class GhostAbility : NetworkBehaviour
{
    [SerializeField] private float timeToInvis = 1f;
    private float counter = 0f;
    private Ship shipScript;
    private Movement movementScript;
    private GameObject invisRangeIndicator;

    void Start()
    {
        shipScript = gameObject.GetComponent<Ship>();
        movementScript = gameObject.GetComponent<Movement>();

        invisRangeIndicator = transform.Find("InvisRangeIndicator").gameObject;
        invisRangeIndicator.SetActive(false);
    }

    void FixedUpdate()
    {
        if (!IsHost) return;

        //While we are moving, keep the counter at 0 seconds and if we are invis become visible
        if (movementScript.moving)
        {
            counter = 0;
            if (shipScript.isInvis)
                GoVisible();
        }
        //If we are not moving and not yet invis, count up until we are invis
        else if (!movementScript.moving && !shipScript.isInvis)
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
        shipScript.isInvis = true;
        ShowInvisEffectsRPC();
    }

    private void GoVisible() 
    {
        shipScript.isInvis = false;
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

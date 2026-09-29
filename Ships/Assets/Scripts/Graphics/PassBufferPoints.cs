using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class PassBufferPoints : NetworkBehaviour
{
    private Vector4[] bufferData = new Vector4[128];
    private GameObject bulletContainer;
    public bool revealMap = false;
    

    void OnEnable()
    {
        bulletContainer = GameObject.Find("Bullet Container");
    }

    void Update()
    {
        if (!IsLocalPlayer) return;

        if (revealMap)
        {
            Shader.SetGlobalInt("_GlobalPointsBufferCount", 0);
        }
        else
        {
            Ship[] ships = GetComponentsInChildren<Ship>();

            if (bulletContainer == null)
                bulletContainer = GameObject.Find("Bullet Container");

            List<Missile> missiles = new List<Missile>();
            if (bulletContainer != null)
            {
                Missile[] unfilteredMissiles = bulletContainer.GetComponentsInChildren<Missile>();
                foreach (Missile missile in unfilteredMissiles)
                {
                    if (missile.OwnerClientId == this.OwnerClientId && !missile.isFromNeutralShip)
                    {
                        missiles.Add(missile);
                    }
                }
            }

            VisionCone[] visionCones = Object.FindObjectsByType<VisionCone>(FindObjectsSortMode.None);

            // Go through ships
            for (int i = 0; i < ships.Length; i++)
            {
                bufferData[i] = (Vector2)ships[i].transform.position;
                bufferData[i].z = 1;
                bufferData[i].w = ships[i].visionRange;
            }

            // Go through Missiles
            for (int i = 0; i < missiles.Count; i++)
            {
                bufferData[ships.Length + i] = (Vector2)missiles[i].transform.position;
                bufferData[ships.Length + i].z = 1;
                bufferData[ships.Length + i].w = missiles[i].visionRange;
            }

            // Go through vision cones
            for (int i = 0; i < visionCones.Length; i++)
            {
                bufferData[ships.Length + missiles.Count + i] = (Vector2)visionCones[i].transform.position;
                bufferData[ships.Length + missiles.Count + i].z = visionCones[i].alphaValue;
                bufferData[ships.Length + missiles.Count + i].w = visionCones[i].visionRange;
            }

            Shader.SetGlobalInt("_GlobalPointsBufferCount", ships.Length + missiles.Count + visionCones.Length);
            Shader.SetGlobalVectorArray("_GlobalPointsBuffer", bufferData);
        }
    }

    private void OnDisable()
    {
        Shader.SetGlobalInt("_GlobalPointsBufferCount", 0);
    }
}

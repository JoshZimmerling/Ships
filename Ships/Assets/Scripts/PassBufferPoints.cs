using NUnit.Framework;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Unity.Netcode;
using UnityEngine;


//[ExecuteInEditMode]
public class PassBufferPoints : NetworkBehaviour
{
    private VisionCone[] bufferData;
    private ComputeBuffer pointsBuffer;
    private GameObject bulletContainer;

    [StructLayout(LayoutKind.Sequential)]
    public struct VisionCone
    {
        public Vector2 position;        // 2 floats = 8 bytes
        public float visionRadius;      // 1 float  = 4 bytes
    }                                   // Total stride = 12 bytes

    void OnEnable()
    {
        // Initialize buffer: (Number of elements, Size of float4 in bytes [4 floats * 4 bytes])
        pointsBuffer = new ComputeBuffer(32, Marshal.SizeOf(typeof(VisionCone)));
        bulletContainer = GameObject.Find("Bullet Container");
    }

    void Update()
    {
        if (!IsLocalPlayer) return;

        //if (bulletContainer == null) bulletContainer = GameObject.Find("Bullet Container");

        Ship[] ships = GetComponentsInChildren<Ship>();
        Missile[] unfilteredMissiles = bulletContainer.GetComponentsInChildren<Missile>();

        List<Missile> missiles = new List<Missile>();

        foreach (Missile missile in unfilteredMissiles)
        {
            if (missile.OwnerClientId == this.OwnerClientId && !missile.isFromNeutralShip)
            {
                missiles.Add(missile);
            }
        }

        bufferData = new VisionCone[ships.Length + missiles.Count];

        // Go through ships
        for (int i = 0; i < ships.Length; i++)
        {
            bufferData[i].position = (Vector2)ships[i].transform.position;
            bufferData[i].visionRadius = ships[i].visionRange;
        }

        // Go through Missiles
        for (int i = 0; i < missiles.Count; i++)
        {
            bufferData[ships.Length + i].position = (Vector2)missiles[i].transform.position;
            bufferData[ships.Length + i].visionRadius = missiles[i].visionRange;
        }

        // Dynamically increases buffer as needed
        if (Shader.GetGlobalInt("_PointsBufferCount") < bufferData.Length)
            pointsBuffer = new ComputeBuffer(bufferData.Length, Marshal.SizeOf(typeof(VisionCone)));

        // Upload the array to the GPU buffer
        pointsBuffer.SetData(bufferData);

        // Bind the buffer and the count globally so any material can read it
        Shader.SetGlobalBuffer("_PointsBuffer", pointsBuffer);
        Shader.SetGlobalInt("_PointsBufferCount", bufferData.Length);
    }

    void OnDisable()
    {
        // ComputeBuffers must be disposed to avoid severe memory leaks on the GPU
        if (pointsBuffer != null)
        {
            pointsBuffer.Release();
            pointsBuffer = null;
        }
    }
}
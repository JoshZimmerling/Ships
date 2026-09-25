using NUnit.Framework;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Unity.Netcode;
using UnityEngine;

public class PassBufferPoints : NetworkBehaviour
{
    private VisionCone[] bufferData;
    private ComputeBuffer pointsBuffer;
    private GameObject bulletContainer;
    public bool revealMap = false;

    // Updated Struct: Padded out to match the 32-byte HLSL stride requirements (multiples of 16)
    [StructLayout(LayoutKind.Sequential)]
    public struct VisionCone
    {
        public Vector4 position;        // 4 floats = 16 bytes (Uses .x and .y for your 2D space)
        public float visionRadius;      // 1 float  = 4 bytes
        public Vector3 padding;         // 3 floats = 12 bytes
    }                                   // Total stride = 32 bytes

    void OnEnable()
    {
        // Start with a reasonable baseline capacity (32 slots)
        pointsBuffer = new ComputeBuffer(32, Marshal.SizeOf(typeof(VisionCone)));
        bulletContainer = GameObject.Find("Bullet Container");
    }

    void Update()
    {
        if (!IsLocalPlayer) return;

        if (revealMap)
        {
            bufferData = new VisionCone[0];
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

            bufferData = new VisionCone[ships.Length + missiles.Count];

            // Go through ships
            for (int i = 0; i < ships.Length; i++)
            {
                // Vector4 automatically handles implicit conversion from Vector3/Vector2
                bufferData[i].position = ships[i].transform.position;
                bufferData[i].visionRadius = ships[i].visionRange;
            }

            // Go through Missiles
            for (int i = 0; i < missiles.Count; i++)
            {
                bufferData[ships.Length + i].position = missiles[i].transform.position;
                bufferData[ships.Length + i].visionRadius = missiles[i].visionRange;
            }

            // FIXED: Only resize the buffer if our current pool size is too small.
            // This prevents allocating a brand new GPU buffer every single frame.
            if (pointsBuffer == null || pointsBuffer.count < bufferData.Length)
            {
                if (pointsBuffer != null) pointsBuffer.Release();

                // Allocate a little extra headroom (e.g., length + 8) to avoid resizing constantly
                int newCapacity = bufferData.Length + 8;
                pointsBuffer = new ComputeBuffer(newCapacity, Marshal.SizeOf(typeof(VisionCone)));
            }
        }

        // Upload array to GPU buffer (if empty, it safely uploads 0 elements)
        if (bufferData.Length > 0)
        {
            pointsBuffer.SetData(bufferData);
        }

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

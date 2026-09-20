using UnityEngine;

public class PassBufferPoints : MonoBehaviour
{
    public Vector3[] worldPoints;
    private ComputeBuffer pointsBuffer;

    void OnEnable()
    {
        if (worldPoints == null || worldPoints.Length == 0) return;

        // Initialize buffer: (Number of elements, Size of float4 in bytes [4 floats * 4 bytes])
        pointsBuffer = new ComputeBuffer(worldPoints.Length, sizeof(float) * 4);
    }

    void Update()
    {
        if (pointsBuffer == null || worldPoints == null) return;

        // Cover Vector3[] into a temporary Vector4[] array
        Vector4[] bufferData = new Vector4[worldPoints.Length];
        for (int i = 0; i < worldPoints.Length; i++)
        {
            bufferData[i] = new Vector4(worldPoints[i].x, worldPoints[i].y, worldPoints[i].z, 1.0f);
        }

        // Upload the array to the GPU buffer
        pointsBuffer.SetData(bufferData);

        // Bind the buffer and the count globally so any material can read it
        Shader.SetGlobalBuffer("_PointsBuffer", pointsBuffer);
        Shader.SetGlobalInt("_PointsBufferCount", worldPoints.Length);
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
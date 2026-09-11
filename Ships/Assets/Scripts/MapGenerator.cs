using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

[ExecuteInEditMode]
public class MapGenerator : MonoBehaviour
{
    [Header("Spawner Settings")]
    public List<GameObject> prefabsToSpawn;

    public float radiusMin;
    public float radiusMax;
    public float densityMin;
    public float densityMax;
    public float colliderRadius;

    [Tooltip("Updates the circle automatically when values change in the Inspector.")]
    public bool updateInEditor = true;

    private float lastRadiusMin;
    private float lastRadiusMax;
    private float lastdensityMin;
    private float lastdensityMax;
    private float lastColliderRadius;
    private List<GameObject> lastPrefabs;

    void Update()
    {
        if (!updateInEditor) return;

        // Check that parameters are good
        if (radiusMin <= 0 || radiusMax <= 0 || densityMin <= 0 || densityMax <= 0) return;
        if (prefabsToSpawn.Count < 1) return;
        foreach (var prefab in prefabsToSpawn)
            if (prefab == null) return;

        // Check if any values changed to avoid rebuilding every single frame
        if (radiusMin != lastRadiusMin || radiusMax != lastRadiusMax || densityMin != lastdensityMin || densityMax != lastdensityMax || colliderRadius != lastColliderRadius || prefabsToSpawn != lastPrefabs)
        {
            Debug.Log("Updating Map");
            GenerateCircle();

            lastRadiusMin = radiusMin;
            lastRadiusMax = radiusMax;
            lastdensityMin = densityMin;
            lastdensityMax = densityMax;
            lastPrefabs = prefabsToSpawn;
            lastColliderRadius = colliderRadius;
        }
    }

    public void GenerateCircle()
    {
        // Clear existing children first
        ClearCircle();
        float density;
        for (float rad = radiusMin; rad < radiusMax; rad += 1)
        {
            density = ((rad - radiusMin) / (radiusMax - radiusMin)) * (densityMax - densityMin) + densityMin;
            for (float i = 0; i < 360 * density; i++)
            {
                GameObject newObj = (GameObject)PrefabUtility.InstantiatePrefab(prefabsToSpawn[Random.Range(0, prefabsToSpawn.Count)], transform);
                newObj.transform.position = Random.onUnitCircle * rad;
                newObj.transform.rotation = Quaternion.Euler(0f, 0f, Random.Range(0f, 360f));
                if (rad > colliderRadius)
                {
                    DestroyImmediate(newObj.GetComponent<PolygonCollider2D>());
                    DestroyImmediate(newObj.GetComponent<Rigidbody2D>());
                }
            }
        }
        GetComponent<CircleCollider2D>().radius = colliderRadius;
    }

    public void ClearCircle()
    {
        while (transform.childCount > 0)
        {
            GameObject.DestroyImmediate(transform.GetChild(0).gameObject);
        }
    }
}

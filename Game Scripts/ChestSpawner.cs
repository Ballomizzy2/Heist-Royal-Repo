using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class ChestSpawner : MonoBehaviour
{
    [SerializeField]
    public GameObject chestPrefab;  // The chest prefab to spawn
    [SerializeField]
    public Terrain terrain;  // The terrain object
    public int maxChests = 20;  // Maximum number of chests to spawn
    public float offsetY = -5f;  // Vertical offset from terrain
    [SerializeField]
    public GameObject boundingSphere;  // Sphere bounding volume to avoid
    public float sphereRadius = 10.0f;  // Radius of the bounding sphere

    private TerrainData terrainData;
    private float terrainWidth;
    private float terrainLength;
    [SerializeField]
    private SphereCollider sphereCollider;

    private PhotonView view;

    void Start()
    {
        terrainData = terrain.terrainData;
        terrainWidth = terrainData.size.x;
        terrainLength = terrainData.size.z;
        sphereRadius = boundingSphere.GetComponent<SphereCollider>().radius;
        view = GetComponent<PhotonView>();


        SpawnChests();
    }

    void SpawnChests()
    {
        int chestsSpawned = 0;

        while (chestsSpawned < maxChests)
        {
            // Randomly select a position on the terrain
            float randomX = Random.Range(sphereCollider.bounds.min.x, sphereCollider.bounds.max.x);
            float randomZ = Random.Range(sphereCollider.bounds.min.z, sphereCollider.bounds.max.z);



            // Calculate the height of the terrain at the random position
            float terrainHeight = terrain.SampleHeight(new Vector3(randomX, 0, randomZ));

            // Calculate the spawn position with the offset
            Vector3 spawnPosition = new Vector3(randomX, terrainHeight + offsetY, randomZ);
            spawnPosition.y = spawnPosition.y-6f;

            //Instantiate(chestPrefab, spawnPosition, Quaternion.identity);
            PhotonNetwork.Instantiate("Chest Prototype", spawnPosition, Quaternion.identity);
            chestsSpawned++;

            // Check if the spawn position is outside the bounding sphere
            /*if (!IsWithinBoundingSphere(spawnPosition))
            {
                // Instantiate the chest at the spawn position
                Instantiate(chestPrefab, spawnPosition, Quaternion.identity);
                chestsSpawned++;
            }*/
        }
    }

    bool IsWithinBoundingSphere(Vector3 position)
    {
        // Calculate the distance between the position and the sphere's center
        float distance = Vector3.Distance(position, boundingSphere.transform.position);
        //sphereRadius = boundingSphere.GetComponent<SphereCollider>().radius;
        // Check if the position is within the sphere's radius
        return distance < sphereRadius;
    }
}

// Test Script for generating a chunk with the marching cubes compute shader

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class ChunkGenerator : MonoBehaviour {
    public GameObject terrain;
    public Vector3Int testCoords;
    public Material terrainMaterial;
    public ComputeShader marchingCubes;

    void Start() {
        terrain = this.gameObject;
    }

    void Update() {
        // This part updates when a chunk is deleted in scene view
        if(terrain.transform.childCount == 0) {
            GenerateChunk();
            Debug.Log("Compute Shaders supported?" + SystemInfo.supportsComputeShaders);
        }
    }

    void GenerateChunk() {
        GameObject chunk = new GameObject($"FirstChunk");
        chunk.transform.parent = terrain.transform;
        Chunk testChunk = chunk.AddComponent<Chunk>();
        testChunk.coords = testCoords;
        testChunk.material = terrainMaterial;
    }
}

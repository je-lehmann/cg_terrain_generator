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
    public DensityFunction densityFunction;

    [Header ("Voxel Params")]
    
    // Changed Ranges for better visability 
    [Range (0f, 10f)]
    public float isoLevel = 0.0f; 

    [Range (0f, 200f)]
    public float scale = 1f; // we need that later on
    // Chunk Helpers
    public Chunk chunk;
    public Vector3Int numChunks = Vector3Int.one; // for know we only have one chunk
    
    // Buffers
    ComputeBuffer triBuffer;
    ComputeBuffer vertBuffer;
    ComputeBuffer numBuffer;
    const int threadGroupSize = 8;
    bool updatedParameters;

    public Mesh generatedMesh;

    // updated parameters in Editor lead to new mesh generation
    void OnValidate() {
        generatedMesh = new Mesh();
        terrain = this.gameObject;
        updatedParameters = true;
    }
    void Update() {
        if (updatedParameters) {
            updatedParameters = false; 
            UpdateTerrain();
        }
    }
    public void UpdateTerrain(){
          // Debug.Log("Compute Shaders supported? " + SystemInfo.supportsComputeShaders);
            
        if (triBuffer != null || vertBuffer != null || numBuffer != null){
            triBuffer.Release ();
            vertBuffer.Release ();
            numBuffer.Release ();
        }

        CreateBuffers();

        if(GameObject.Find("FirstChunk") == null){
            GenerateChunk();
        }
        
        UpdateChunkMesh(chunk);
    }
    void CreateBuffers () {
        int numPoints = chunk.resolution * chunk.resolution * chunk.resolution;
        int numVoxelsPerAxis = chunk.resolution - 1; 
        int numVoxels = numVoxelsPerAxis * numVoxelsPerAxis * numVoxelsPerAxis;
        int maxTriangleCount = numVoxels * 5; //max of 5 polygons per cube

        // Buffers are initialized with count: number of elements in the buffer, stride: size of one element in the buffer, type
        // ComputeBufferType.Append: Allows a buffer to be treated like a stack in compute shaders 
        // ComputeBufferType.Raw: byte address buffer
        triBuffer = new ComputeBuffer (maxTriangleCount, sizeof (float) * 3 * 3, ComputeBufferType.Append);
        vertBuffer = new ComputeBuffer (numPoints, sizeof (float) * 4);
        numBuffer = new ComputeBuffer (1, sizeof (int), ComputeBufferType.Raw);
    }
     void GenerateChunk() {
        if(GameObject.Find("FirstChunk") == null) {
            GameObject chunkObject = new GameObject($"FirstChunk");
            chunkObject.transform.parent = terrain.transform;
            chunk = chunkObject.AddComponent<Chunk>();
            chunk.InitializeChunk(terrainMaterial, testCoords, generatedMesh);
        }
    }
     public void UpdateChunkMesh (Chunk chunk) {
        
        int numVoxelsPerAxis = chunk.resolution - 1;
        // int numThreadsPerAxis = Mathf.CeilToInt (numVoxelsPerAxis / (float) threadGroupSize);

        // placeholder for position of chunk
        Vector3Int coord = Vector3Int.zero;
        
        // generate noisy density values
        densityFunction.Generate (vertBuffer, chunk.resolution, scale, chunk.resolution);
       
        triBuffer.SetCounterValue (0); // resets number of elements in the buffer to 0
        int kernelHandle = marchingCubes.FindKernel("MarchingCubes");
        marchingCubes.SetBuffer (kernelHandle, "vertices", vertBuffer);
        marchingCubes.SetBuffer (kernelHandle, "triangles", triBuffer);
        marchingCubes.SetInt ("vertsPerAxis", chunk.resolution);
        marchingCubes.SetFloat ("isoLevel", isoLevel);

        marchingCubes.Dispatch (0, 8, 8, 8); //num Threads.... how many are needed???

        // Get number of triangles in the triangle buffer
        ComputeBuffer.CopyCount (triBuffer, numBuffer, 0);
        int[] triCountArray = new int[numBuffer.count];
        numBuffer.GetData (triCountArray);
        int numTriangles = triCountArray[0];

        Debug.Log(triCountArray[0] + " triangles were generated");

        // Get triangle data from shader
        Triangle[] triangles = new Triangle[numTriangles];
        triBuffer.GetData (triangles, 0, 0, numTriangles);

        generatedMesh.Clear();

        // write queried data into new mesh and assign it
        var vertices = new Vector3[numTriangles * 3];
        var meshTriangles = new int[numTriangles * 3];

        for (int i = 0; i < numTriangles; i++) {
            for (int j = 0; j < 3; j++) {
                meshTriangles[i * 3 + j] = i * 3 + j;
                vertices[i * 3 + j] = triangles[i][j];
            }
        }
        generatedMesh.vertices = vertices;
        generatedMesh.triangles = meshTriangles;

        generatedMesh.RecalculateNormals(); //replace by smooth normals later?
        
        chunk.UpdateMesh(generatedMesh);
    }
    
    // draw red boundaries for debugging reasons
    void OnDrawGizmos () {
        Gizmos.color = Color.red;
        float offset = scale;
        Gizmos.DrawWireCube (Vector3.zero + new Vector3(offset/2, offset/2, offset/2), Vector3.one * offset);
    }
    struct Triangle {
            public Vector3 a;
            public Vector3 b;
            public Vector3 c;

            public Vector3 this [int i] {
                get {
                    switch (i) {
                        case 0:
                            return a;
                        case 1:
                            return b;
                        default:
                            return c;
                    }
                }
            }
    }
}

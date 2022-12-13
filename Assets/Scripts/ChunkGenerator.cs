// Test Script for generating a chunk with the marching cubes compute shader

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class ChunkGenerator : MonoBehaviour {
    public GameObject terrain;
    public Material terrainMaterial;
    public ComputeShader marchingCubes;
    public DensityFunction densityFunction;

    [Header ("Voxel Params")]
    
    // Changed Ranges for better visability 
    [Range (-1f, 10f)]
    public float isoLevel = 0.0f; 
    public int seed;

    public int scale = 1; // we need that later on
    // Chunk Helpers
    public List<Chunk> chunkList= new List<Chunk>();
   // Dictionary<Vector2Int, Chunk> activeChunks = new Dictionary<Vector2Int, Chunk>();
    
    [Range (2, 64)] //depends on number of threads per chunk
    public int resolution = 2; // this will be our resolution or LOD later on    
    public Vector2Int chunkXZ = Vector2Int.one; // for know we only have one chunk
    
    // Buffers
    ComputeBuffer triBuffer;
    ComputeBuffer vertBuffer;
    ComputeBuffer numBuffer;
    const int threadGroupSize = 8;
    bool updatedParameters;
    private float y_Offset = 0;
// 
    // updated parameters in Editor lead to new mesh generation
    void OnValidate() {
        terrain = this.gameObject;
        updatedParameters = true;
    }
    void Update() {
        if (updatedParameters) {
            updatedParameters = false; 
            foreach(Chunk c in chunkList){
                // Debug.Log("destroyed" +  c.transform.gameObject.name);
                DestroyImmediate(c.transform.gameObject);
            }
            chunkList.Clear();
            UpdateTerrain();

        }
    }
    public void UpdateTerrain(bool forceUpdate = false){
          // Debug.Log("Compute Shaders supported? " + SystemInfo.supportsComputeShaders);
            
        if (triBuffer != null || vertBuffer != null || numBuffer != null){
            triBuffer.Release ();
            vertBuffer.Release ();
            numBuffer.Release ();
        }

        CreateBuffers();
        // activeChunks.Clear();
        //chunk creation
        for(int i = 0; i < chunkXZ.x; i++) {
            for(int j = 0; j < chunkXZ.y; j++) {
                 if(GameObject.Find($"Chunk ({i}, {j})") == null){
                  //  Debug.Log("Create new Chunk");
                 GenerateChunk(new Vector2Int(i,j));
                }
            }
        }
    }
    void CreateBuffers () {
        int numPoints = resolution * resolution * resolution;
        int numVoxelsPerAxis = resolution - 1; 
        int numVoxels = numVoxelsPerAxis * numVoxelsPerAxis * numVoxelsPerAxis;
        int maxTriangleCount = numVoxels * 5; //max of 5 polygons per cube

        // Buffers are initialized with count: number of elements in the buffer, stride: size of one element in the buffer, type
        // ComputeBufferType.Append: Allows a buffer to be treated like a stack in compute shaders 
        // ComputeBufferType.Raw: byte address buffer
        triBuffer = new ComputeBuffer (maxTriangleCount, sizeof (float) * 3 * 3, ComputeBufferType.Append);
        vertBuffer = new ComputeBuffer (numPoints, sizeof (float) * 4);
        numBuffer = new ComputeBuffer (1, sizeof (int), ComputeBufferType.Raw);
    }
     void GenerateChunk(Vector2Int position) {
        // Debug.Log("pos1" + position);
        Chunk newChunk = new Chunk();
        GameObject chunkObject = new GameObject($"Chunk ({position.x}, {position.y})");
        chunkObject.transform.parent = terrain.transform;
        newChunk = chunkObject.AddComponent<Chunk>();
        newChunk.InitializeChunk(new Vector3(position.x, y_Offset ,position.y), terrainMaterial);
        chunkList.Add(newChunk);
        UpdateMesh(newChunk);
    }
    void DeleteChunk(Vector2Int position) {
        GameObject doomedChunk = (GameObject.Find($"Chunk ({position.x}, {position.y})"));
        Destroy(doomedChunk);
    }
     public void UpdateMesh (Chunk chunk) {
        
        //estimate center so we can propagate noise along multiple chunks
        Vector3 bounds = new Vector3(chunkXZ.x, y_Offset, chunkXZ.y);
        Vector3 center =  -bounds / 2 + (Vector3) chunk.coords * scale + Vector3.one * scale / 2;
        // generate noisy density values
        center.y = 0; // whyyyy we need this... It should 0 despite center scaling
        Debug.Log("CENTER" + center);

        densityFunction.Generate (vertBuffer, resolution, scale, center);
       
        triBuffer.SetCounterValue (0); // resets number of elements in the buffer to 0
        int kernelHandle = marchingCubes.FindKernel("MarchingCubes");
        marchingCubes.SetBuffer (kernelHandle, "vertices", vertBuffer);
        marchingCubes.SetBuffer (kernelHandle, "triangles", triBuffer);
        marchingCubes.SetInt ("vertsPerAxis", resolution);
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

        Mesh generatedMesh = chunk.mesh;
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
        
        //chunk.UpdateMesh(generatedMesh);
        //Debug.Log(generatedMesh);
    }
    
    // draw red boundaries for debugging reasons
    void OnDrawGizmos () {
        Gizmos.color = Color.red;
        float offset = scale;
      //  Gizmos.DrawWireCube (Vector3.zero + new Vector3(offset/2 - 0.5f, offset/2 - 0.5f, offset/2 - 0.5f), Vector3.one * offset);
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

// Test Script for generating a chunk with the marching cubes compute shader

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

[ExecuteInEditMode]
public class ChunkGenerator : MonoBehaviour {
    public GameObject terrain;
    public Material terrainMaterial;
    public ComputeShader marchingCubes;
    public DensityFunction densityFunction;
    public Vector2Int cameraXZ;

    [Header ("Voxel Params")]
    
    // Changed Ranges for better visability 
    [Range (-100f, 100f)]
    public float isoLevel = 0.0f; 
    public int seed;

    [Range (0f, 200f)]
    public float scale = 1f; // we need that later on
    // Chunk Helpers
    public List<Chunk> chunkList= new List<Chunk>();
    Dictionary<Vector2Int, Chunk> activeChunks = new Dictionary<Vector2Int, Chunk>();
    public Vector2Int chunkXZ;
    
    [Range (2, 64)] //depends on number of threads per chunk
    public int resolution = 2; // this will be our resolution or LOD later on    
    
    // Buffers
    ComputeBuffer triBuffer;
    ComputeBuffer vertBuffer;
    ComputeBuffer numBuffer;
    const int threadGroupSize = 8;
    bool updatedParameters;
    bool calculatedDensity = false;
    private float y_Offset = 0;

    // update helpers
    private float nextActionTime = 0.0f;
    public float period = 0.1f;
    // updated parameters in Editor lead to new mesh generation
    void OnValidate() {
        terrain = this.gameObject;
        updatedParameters = true;
    }
    void Update() {

        if (updatedParameters) {
            updatedParameters = false; 
            UpdateTerrain();
            UpdateChunkVisibility();
        }
        // on variable change: XZ camera position
        if (Camera.main.transform.hasChanged) {
            // Camera has moved, do something here
            UpdateChunkVisibility();
            Camera.main.transform.hasChanged = false;
        }
    }
    public void UpdateTerrain(){
          // Debug.Log("Compute Shaders supported? " + SystemInfo.supportsComputeShaders);
        for (int i = 0; i < chunkList.Count; i++)
        {
            Chunk c = chunkList[i];
            DestroyImmediate(c.transform.gameObject);
        }
        chunkList.Clear();

        if (triBuffer != null || vertBuffer != null || numBuffer != null){
            triBuffer.Release ();
            vertBuffer.Release ();
            numBuffer.Release ();
        }

        CreateBuffers();
      
        // Implement Lvl of Detail here, chunks further away from camera dont need to be rendered in high res.
        
        int totalChunks = chunkXZ.x * chunkXZ.y;

        int roundedChunk = (int)Mathf.Round(chunkXZ.x) / 2;
        
        for (int i = 0; i < totalChunks; i++) {
            int x = i % chunkXZ.x;
            int y = i / chunkXZ.x;
            GenerateChunk(new Vector2Int(cameraXZ.x - roundedChunk + x, cameraXZ.y - roundedChunk + y));
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
        Chunk newChunk;
        GameObject chunkObject = new GameObject($"Chunk ({position.x}, {position.y})");
        chunkObject.transform.parent = terrain.transform;
        newChunk = chunkObject.AddComponent<Chunk>();
        newChunk.InitializeChunk(new Vector3(position.x, y_Offset, position.y), terrainMaterial);
        chunkList.Add(newChunk);
        
        UpdateMesh(newChunk);
        // Debug.Log("NEW CHUNK" + newChunk.name);
        // activeChunks.Add(position, newChunk);
        // Debug current Dictionary Entries
        // activeChunks.ToList().ForEach(x => Debug.Log(x));
    }
    void UpdateChunkVisibility() {
        
        // calculate relevant vector grid
        // convert the camera position to the xz grid position (compare to center)
        Vector3 bounds = new Vector3(chunkXZ.x, y_Offset, chunkXZ.y);
        Vector3 cameraPlanePosition  =  -bounds / 2 + (Vector3) Camera.main.transform.localPosition / scale + Vector3.one / scale / 2;
        cameraPlanePosition.y = 0;
        Vector2Int currentCameraXZ = cameraXZ;
        // translate to coord origin and round to int
        Vector2Int newCameraXZ = new Vector2Int((int)Mathf.Round(cameraPlanePosition.x) + (int)(Mathf.Round(chunkXZ.x)/2), (int)Mathf.Round(cameraPlanePosition.z) + (int)(Mathf.Round(chunkXZ.x)/2));
        if(newCameraXZ != currentCameraXZ){
            cameraXZ = newCameraXZ;
            Debug.Log("CHUNK CAMERA UPDATE");
            // UpdateTerrain();
        }
            
        // Debug.Log("cameraXZ" + cameraXZ);        

        // disable unneeded chunks

        // generate new chunks according to Chunk Buffer? 
    }
    void DeleteChunk(Vector2Int position) {
        GameObject doomedChunk = (GameObject.Find($"Chunk ({position.x}, {position.y})"));
        DestroyImmediate(doomedChunk);
    }
    public void UpdateMesh (Chunk chunk) {
        
        //estimate center so we can propagate noise along multiple chunks
        Vector3 bounds = new Vector3(chunkXZ.x, y_Offset, chunkXZ.y);
        Vector3 center =  -bounds / 2 + (Vector3) chunk.localCoords * scale + Vector3.one * scale / 2;
        Debug.Log("CENTER" + center);

        // generate noisy density values
        center.y = 0; // whyyyy we need this... It should 0 despite center scaling

        densityFunction.Generate (vertBuffer, resolution, scale, center);
            // calculatedDensity = true;
        
       
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


        for (int i = 0; i < numTriangles * 3; i += 3)
        {
            meshTriangles[i] = i;
            meshTriangles[i + 1] = i + 1;
            meshTriangles[i + 2] = i + 2;
            vertices[i] = triangles[i / 3][0];
            vertices[i + 1] = triangles[i / 3][1];
            vertices[i + 2] = triangles[i / 3][2];
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

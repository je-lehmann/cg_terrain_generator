//TODO: Add dispatch operations!

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
    public bool camCentered = true;
    public int maxRenderedDistance = 20;
    public int maxVisible = 5;


    [Header ("Voxel Params")]
    
    // Changed Ranges for better visability
    [Range (-100f, 100f)]
    public float isoLevel = 0.0f; 
    public int seed;

    [Range (0f, 200f)]
    public float scale = 1f; // we need that later on
    // Chunk Helpers
    // Chunk dictionary should replace the old chunk list
    Dictionary<string, Chunk> chunkDict = new Dictionary<string, Chunk>();
   
    [Header("at least 4x4 for LOD")]
    public Vector2Int chunkXZ;
    
    [Range (32, 64)] //depends on number of threads per chunk
    public int maxResolution = 60; // this will be our resolution or LOD later on    
    
    // Buffers
    ComputeBuffer triBuffer;
    ComputeBuffer vertBuffer;
    ComputeBuffer numBuffer;
    const int threadGroupSize = 8;
    bool updatedParameters;
    bool calculatedDensity = false;
    private float y_Offset = 0;
    public int[] lodModifiers;
    public bool printChunks = false;


    // update helpers
    // updated parameters in Editor lead to new mesh generation.

    void OnValidate() {
        terrain = this.gameObject;
        updatedParameters = true;
        // Debug.Log("Compute Shaders supported? " + SystemInfo.supportsComputeShaders);
    }
    
    void Update() {
        
        if(printChunks){
            printChunks = false;
            foreach (string key in chunkDict.Keys.ToList()) {
                Chunk c = chunkDict[key];
                Debug.Log("Chunk Print: " + c.transform.gameObject.name);    
            }            
        }

        if (updatedParameters) {
            updatedParameters = false; 
            ClearAllChunks();
            UpdateTerrain(); // UNCOMMENT ME
        }
        // on variable change: XZ camera position
        if (Camera.main.transform.hasChanged) {
            // Camera has moved, do something here
            CameraChunkUpdate();
            Camera.main.transform.hasChanged = false;
        }
    }
    public void UpdateTerrain(){

        if (triBuffer != null || vertBuffer != null || numBuffer != null){
            triBuffer.Release();
            vertBuffer.Release();
            numBuffer.Release();
        }

        CreateBuffers();
      
        // Implement Lvl of Detail here, chunks further away from camera dont need to be rendered in high res.
        
        int totalChunks = chunkXZ.x * chunkXZ.y;

        int roundedChunk = (int)Mathf.Round(chunkXZ.x) / 2;
        
        for (int i = 0; i < totalChunks; i++) {
            int x = i % chunkXZ.x;
            int y = i / chunkXZ.x;
            if (camCentered) {
                GenerateChunk(new Vector2Int(cameraXZ.x - roundedChunk + x, cameraXZ.y - roundedChunk + y));
            } else {
                GenerateChunk(new Vector2Int(cameraXZ.x - roundedChunk + x, cameraXZ.y + y));
            }
        }

    }
    void CreateBuffers () {
        int numPoints = maxResolution * maxResolution * maxResolution;
        int numVoxelsPerAxis = maxResolution - 1; 
        int numVoxels = numVoxelsPerAxis * numVoxelsPerAxis * numVoxelsPerAxis;
        int maxTriangleCount = numVoxels * 5; //max of 5 polygons per cube

        // Buffers are initialized with count: number of elements in the buffer, stride: size of one element in the buffer, type
        // ComputeBufferType.Append: Allows a buffer to be treated like a stack in compute shaders 
        // ComputeBufferType.Raw: byte address buffer
        triBuffer = new ComputeBuffer (maxTriangleCount, sizeof (float) * 3 * 3, ComputeBufferType.Append);
        vertBuffer = new ComputeBuffer (numPoints, sizeof (float) * 4);
        numBuffer = new ComputeBuffer (1, sizeof (int), ComputeBufferType.Raw);
    }
        private int CalculateLOD(Vector2Int chunkPosition) {
            int resolution;
            int distance = Mathf.FloorToInt(Vector2Int.Distance(cameraXZ, chunkPosition));
            if(distance <= 1) {
                resolution =  maxResolution; 
            } else if (distance == 2){
                resolution =  maxResolution / lodModifiers[0]; 
            } else if (distance == 3){
                resolution =  maxResolution / lodModifiers[1]; 
            } else {
                resolution =  maxResolution / lodModifiers[2]; 
            }
                       
            return resolution;
        }
     void GenerateChunk(Vector2Int position) {
        int LOD = CalculateLOD(position);
        string chunkKey = "Chunk (" + position.x + "," + position.y + ")" + " LOD: " + LOD;
        // dont generate chunk if it already exists
        if (!chunkDict.ContainsKey(chunkKey)) {
            Chunk newChunk;
            GameObject chunkObject = new GameObject(chunkKey);
            chunkObject.transform.parent = terrain.transform;
            newChunk = chunkObject.AddComponent<Chunk>();
            newChunk.InitializeChunk(new Vector3(position.x, y_Offset, position.y), terrainMaterial, LOD);
            chunkDict.Add(chunkKey, newChunk);
            // this is expensive
            UpdateMesh(newChunk);
        }
    }
    void CameraChunkUpdate() {
        // this could be more performant: We iterate over thousands of chunks to compare the keys
        // but choosing the right pos and lod is a little tricky...
        Vector3 bounds = new Vector3(chunkXZ.x, y_Offset, chunkXZ.y);
        Vector3 cameraPlanePosition  =  -bounds / 2 + (Vector3) Camera.main.transform.localPosition / scale + Vector3.one / scale / 2;
        cameraPlanePosition.y = 0;
        Vector2Int currentCameraXZ = cameraXZ;
        // translate to coord origin and round to int
        Vector2Int newCameraXZ = new Vector2Int((int)Mathf.Round(cameraPlanePosition.x) + (int)(Mathf.Round(chunkXZ.x)/2), (int)Mathf.Round(cameraPlanePosition.z) + (int)(Mathf.Round(chunkXZ.x)/2));
        if(newCameraXZ != currentCameraXZ){
            cameraXZ = newCameraXZ;
            UpdateTerrain();
            Dictionary<string, Chunk> newDict = new Dictionary<string, Chunk>();            
            foreach (string key in chunkDict.Keys) {
                Chunk c = chunkDict[key];
                int LOD = CalculateLOD(c.localXZ);
                int distance = Mathf.FloorToInt(Vector2Int.Distance(cameraXZ, c.localXZ));
                if(distance >= maxRenderedDistance){
                    c.Destroy();
                } else if(distance >= (chunkXZ.x / 2) + maxVisible || c.chunkResolution != LOD){
                    // we get corners because the distance is calculated different here than in the chunk creation, we could change this when theres time left
                    //Debug.Log( c.name + " has DIST: "+ Mathf.Floor(Vector2Int.Distance(cameraXZ, c.localXZ)).ToString());
                    c.Hide();
                    newDict.Add(c.name,c);
                } else {
                    c.Show();
                    newDict.Add(c.name,c);
                }
            }
            chunkDict = newDict;
           // Debug.Log("We have this many chunks: " + chunkDict.Count);
        }
    }

    public void ClearAllChunks() {
        int childCount = terrain.transform.childCount;
        for (int i = childCount - 1; i >= 0; i--) {
            Transform t = terrain.transform.GetChild(i);
            DestroyImmediate(t.gameObject);
        }
        chunkDict.Clear();
    }

    public void UpdateMesh (Chunk chunk) {
        //estimate center so we can propagate noise along multiple chunks
        Vector3 bounds = new Vector3(chunkXZ.x, y_Offset, chunkXZ.y);
        Vector3 center =  -bounds / 2 + (Vector3) chunk.localCoords * scale + Vector3.one * scale / 2;
        center.y = 0; // whyyyy we need this... It should 0 despite center scaling
        int numThreads = threadGroupSize;
        chunk.GenerateLODMesh(vertBuffer, triBuffer, cameraXZ, densityFunction, scale, center, isoLevel, numThreads, marchingCubes);
       
        // Get number of triangles in the triangle buffer
        ComputeBuffer.CopyCount (triBuffer, numBuffer, 0);
        int[] triCountArray = new int[numBuffer.count];
        numBuffer.GetData (triCountArray);
        int numTriangles = triCountArray[0];
        // Debug.Log(triCountArray[0] + " triangles were generated");
        // Get triangle data from shader
        Triangle[] triangles = new Triangle[numTriangles];
        triBuffer.GetData (triangles, 0, 0, numTriangles);

        Mesh generatedMesh = chunk.mesh;
        generatedMesh.Clear();
        // write queried data into new mesh and assign it
        var vertices = new Vector3[numTriangles * 3];
        var meshTriangles = new int[numTriangles * 3];

        Vector2[] uv = new Vector2[vertices.Length];

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

        // Vector2[] generatedUVs = UnityEditor.Unwrapping.GeneratePerTriangleUV(generatedMesh);
        // generatedMesh.uv = generatedUVs;
        
        //chunk.UpdateMesh(generatedMesh);
        //Debug.Log(generatedMesh);
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

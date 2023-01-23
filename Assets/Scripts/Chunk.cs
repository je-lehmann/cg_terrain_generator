// Chunk Component that holds generated Mesh, Colliders etc.

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Chunk : MonoBehaviour {
    public Vector3 localCoords;
    public Material material;
    public MeshFilter meshFilter;
    public MeshRenderer meshRenderer;
    private bool updatedParameters;
    public Mesh mesh;
    public string name;
    public Vector3 worldCoords;
    
    [Range (2, 64)] //depends on number of threads per chunk
    private int[] LODLevels = new int[4]; // this will be our resolution or LOD later on
  
    public void InitializeChunk(Vector3 position, Material mat){
       name = "Chunk" + position;
       int maxLOD = gameObject.GetComponentInParent<ChunkGenerator>().maxResolution;
       int[] lodModifiers = gameObject.GetComponentInParent<ChunkGenerator>().lodModifiers;

       LODLevels[3] = maxLOD;
       LODLevels[2] = maxLOD / lodModifiers[0];
       LODLevels[1] = maxLOD / lodModifiers[1];
       LODLevels[0] = maxLOD / lodModifiers[2];
       if (meshFilter == null) {
            meshFilter = gameObject.AddComponent<MeshFilter> ();
        }
        if (meshRenderer == null) {
            meshRenderer = gameObject.AddComponent<MeshRenderer> ();
        }
        if (mesh == null) {
            mesh = new Mesh ();
            mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        }
        // Debug.Log("pos2" + coords);
        localCoords = position;
        meshRenderer.material = mat;        
        meshFilter.sharedMesh = mesh;
        // m_filter.sharedMesh = Resources.GetBuiltinResource<Mesh>("Cube.fbx");
    }
    public void UpdateMesh(Mesh mesh) {
        meshFilter.sharedMesh = mesh;
        worldCoords = gameObject.transform.position;
    }

    public void GenerateLODMesh(ComputeBuffer vertBuffer, ComputeBuffer triBuffer, Vector2Int cameraXZ, DensityFunction func, float scale, Vector3 center, float isoLevel, int numThreads, ComputeShader shader) {
        int chunkResolution;
        Vector2Int localXZ = new Vector2Int((int)localCoords.x, (int)localCoords.z);
       // Debug.Log("DIST " + Mathf.Floor(Vector2Int.Distance(cameraXZ,localXZ)) + " for " + name);
        switch (Mathf.Floor(Vector2Int.Distance(cameraXZ,localXZ)))
        {
            case 0:
                chunkResolution =  LODLevels[3]; 
                break;
            case 1:
                chunkResolution =  LODLevels[2]; 
                break;
            case 2:
                chunkResolution =  LODLevels[1]; 
                break;
            default:
                chunkResolution =  LODLevels[0];
                break;
        }

        func.Generate (vertBuffer, chunkResolution, scale, center);
        triBuffer.SetCounterValue (0); // resets number of elements in the buffer to 0
        int kernelHandle = shader.FindKernel("MarchingCubes");
        shader.SetBuffer (kernelHandle, "vertices", vertBuffer);
        shader.SetBuffer (kernelHandle, "triangles", triBuffer);
        shader.SetInt ("vertsPerAxis", chunkResolution);
        shader.SetFloat ("isoLevel", isoLevel);
        shader.Dispatch (0, 8, 8, 8); //num Threads.... how many are needed???
    }
}

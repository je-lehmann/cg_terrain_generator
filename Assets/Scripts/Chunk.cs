// Chunk Component that holds generated Mesh, Colliders etc.

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Chunk : MonoBehaviour {
    public Vector3 localCoords;
    public Vector2Int localXZ;
    public Material material;
    public MeshFilter meshFilter;
    public MeshRenderer meshRenderer;
    private bool updatedParameters;
    public Mesh mesh;
    public Vector3 worldCoords;    
    public int chunkResolution; // this will be our resolution or LOD later on
    public void Destroy () {
       // Debug.Log("Bye" + gameObject.name);
        DestroyImmediate (gameObject, false);    
    }
    public void Hide () {
        meshRenderer.enabled = false;
    }
    public void Show () {
        meshRenderer.enabled = true;
    }
    public void InitializeChunk(Vector3 position, Material mat, int LOD){
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

        localCoords = position;
        localXZ = new Vector2Int((int)localCoords.x, (int)localCoords.z);
        meshRenderer.material = mat;        
        meshFilter.sharedMesh = mesh;
        chunkResolution = LOD;
    }
    public void UpdateMesh(Mesh mesh) {
        meshFilter.sharedMesh = mesh;
        worldCoords = gameObject.transform.position;
    }
    public void GenerateLODMesh(ComputeBuffer vertBuffer, ComputeBuffer triBuffer, Vector2Int cameraXZ, DensityFunction func, float scale, Vector3 center, float isoLevel, int numThreads, ComputeShader shader) {
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

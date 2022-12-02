// Chunk Component that holds generated Mesh, Colliders etc.

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class Chunk : MonoBehaviour {
    public Vector3Int coords;
    public Material material;
    public MeshFilter meshFilter;
    public MeshRenderer meshRenderer;
    private bool updatedParameters;
    
    [Range (2, 64)] //depends on number of threads per chunk
    public int resolution = 2; // this will be our resolution or LOD later on
    void OnValidate() {
        updatedParameters = true;
    }
    void Update() {
        if (updatedParameters) {
            updatedParameters = false; 
            this.gameObject.GetComponentInParent<ChunkGenerator>().UpdateTerrain();
        }
    }
    public void InitializeChunk(Material mat, Vector3Int coords, Mesh mesh){

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
        meshRenderer.material = mat;        
        meshFilter.sharedMesh = mesh;
        // m_filter.sharedMesh = Resources.GetBuiltinResource<Mesh>("Cube.fbx");
    }
    public void UpdateMesh(Mesh mesh) {
        meshFilter.sharedMesh = mesh;
    }
}

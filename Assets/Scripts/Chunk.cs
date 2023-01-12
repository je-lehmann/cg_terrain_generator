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
    public int resolution = 2; // this will be our resolution or LOD later on
  
    public void InitializeChunk(Vector3 position, Material mat){
        name = "Chunk" + position;
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
}

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

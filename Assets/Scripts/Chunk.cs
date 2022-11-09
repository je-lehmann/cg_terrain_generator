// Chunk Component that holds generated Mesh, Colliders etc.

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class Chunk : MonoBehaviour {
    public Vector3Int coords;
    public Material material;


    void Start() {
        MeshFilter m_filter = gameObject.AddComponent<MeshFilter>();
        MeshRenderer m_renderer = gameObject.AddComponent<MeshRenderer>();
        m_renderer.material = material;
        Mesh m = new Mesh();
        m.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        m_filter.sharedMesh = Resources.GetBuiltinResource<Mesh>("Cube.fbx");
    }

    void Update() {
        
    }
}

// set input Parameters for density computation, here we can implement constraints later :)
// we need this here: https://docs.unity3d.com/ScriptReference/ComputeBuffer.html
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class DensityFunction : MonoBehaviour
{
    const int threadGroupSize = 8;
    public ComputeShader densityFunction;
    public bool noiseEnabled = true; // does not update the mesh yet
    bool updatedParameters;
    private ChunkGenerator generator;

    void OnValidate() {
        updatedParameters = true;
        generator = GameObject.Find("Terrain").GetComponent<ChunkGenerator>();
    }
    void Update() {
        if (updatedParameters) {
            updatedParameters = false; 
            generator.UpdateTerrain();
        }
    }
    // returns noised pointset from the gpu that we need for mesh generation, it makes sense
    // to have different versions of this function later...
    // densityFunction.Generate (vertBuffer, pointsPerAxis, scale, drawArea, center);           
     public ComputeBuffer Generate (ComputeBuffer vertBuffer, int vertsPerAxis, float scale, float resolution) {
        int num_points = vertsPerAxis * vertsPerAxis * vertsPerAxis;
        // int numThreadsPerAxis = Mathf.CeilToInt (vertsPerAxis / (float) threadGroupSize);

        // fill up the point buffer in compute shader now
        // set parameters, add more params later
        densityFunction.SetBuffer(0, "verts", vertBuffer);
        densityFunction.SetInt("vertsPerAxis", vertsPerAxis);
        densityFunction.SetFloat("scale", scale);
        densityFunction.SetFloat("resolution", resolution);


        densityFunction.SetBool("noiseEnabled", noiseEnabled); 

        densityFunction.Dispatch(0, 8, 8, 8); // is that correct?

        return vertBuffer;
    }
    
    
}
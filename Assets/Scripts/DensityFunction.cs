// set input Parameters for density computation, here we can implement constraints later :)
// we need this here: https://docs.unity3d.com/ScriptReference/ComputeBuffer.html
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DensityFunction : MonoBehaviour
{
    [Header ("User parameters")]
    public int generator_seed;
    public float noiseScale = 1;
    public float noiseWeight = 1;
    public float floorLevel = 1;

    public Vector4 shaderParams;
    public ComputeShader densityFunction;
    
    /*
     public virtual ComputeBuffer Generate () {
        // returns data from the gpu that we need for mesh generation
    }
    */
}

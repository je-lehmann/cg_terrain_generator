// this script just tests basic compute shader functionality, we want to
// overwrite the material of a sphere with red here, the color assignment is
// executed on the gpu  
// https://docs.unity3d.com/ScriptReference/ComputeBuffer-ctor.html 
// https://docs.unity3d.com/ScriptReference/ComputeBuffer.SetData.html
// https://www.youtube.com/watch?v=BrZ4pWwkpto&ab_channel=GameDevGuide

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShaderTest : MonoBehaviour
{
    public ComputeShader shader;
    public RenderTexture renderTexture;

    public int m_width = 256;
    public int m_height = 256;


    // Start is called before the first frame update
    void Start()
    {
       
    }

    // Update is called once per frame
    void Update()
    {
        // trigger GPU execution with spacebar in play mode
        if (Input.GetKeyDown("space")) {
            executeShader();
        }
    }

    void executeShader() {
        
        renderTexture = new RenderTexture(m_width, m_height, 24);
        renderTexture.enableRandomWrite = true;
        renderTexture.Create();

        int kernelHandle = shader.FindKernel("FillWithRed");
        shader.SetFloat("width", renderTexture.width);
        shader.SetFloat("height", renderTexture.height);
        shader.SetTexture(kernelHandle, "tex", renderTexture);
        shader.Dispatch(kernelHandle, m_width, m_height, 1);
        gameObject.GetComponent<MeshRenderer>().sharedMaterial.mainTexture = renderTexture;

    }
}

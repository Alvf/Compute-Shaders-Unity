using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Cells_Takeover : MonoBehaviour
{
    // Start is called before the first frame update
    public ComputeShader computeShader;
    RenderTexture renderTexture;


    void Start()
    {
        renderTexture = new RenderTexture(8,8, 24);
        renderTexture.enableRandomWrite = true;
        renderTexture.Create();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    void OnRenderImage(RenderTexture src, RenderTexture dest){
        if (renderTexture == null){
            renderTexture = new RenderTexture(8, 8, 24);
            renderTexture.enableRandomWrite = true;
            renderTexture.Create();
        }

        float[] mouseCoords = {Input.mousePosition.x / Screen.width, Input.mousePosition.y / Screen.height};
        computeShader.SetTexture(0, "Result", renderTexture);
        computeShader.SetFloat("Resolutionx", renderTexture.width);
        computeShader.SetFloat("Resolutiony", renderTexture.height);
        computeShader.SetFloats("MouseP", mouseCoords);
        computeShader.Dispatch(0, renderTexture.width / 8, renderTexture.height / 8, 1);

        Graphics.Blit(renderTexture, dest);
    }
}

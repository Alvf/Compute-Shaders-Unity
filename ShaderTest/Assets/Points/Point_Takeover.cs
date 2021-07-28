using System.Collections;
using System.Collections.Generic;
using UnityEngine;

struct Point{
    public float x;
    public float y;
    public float vx;
    public float vy;
};
public class Point_Takeover : MonoBehaviour
{
    public ComputeShader computeShader;
    public ComputeShader diffuseFade;
    public RenderTexture renderTexture;
    public RenderTexture diffFadeTexture;
    public int xres;
    public int yres;
    public int numPoints;
    public float dt;
    public float diff;
    public float fade;
    private Point[] points;

    // Start is called before the first frame update
    void Start()
    {
        if (!renderTexture){
            renderTexture = new RenderTexture(xres, yres, 24);
            renderTexture.enableRandomWrite = true;
            renderTexture.Create();
        }
        
        if (!diffFadeTexture){
            diffFadeTexture = new RenderTexture(xres, yres, 24);
            diffFadeTexture.enableRandomWrite = true;
            diffFadeTexture.Create();
        }

        points = new Point[numPoints];
        for (int i = 0; i < numPoints; i++){
            points[i].x = Random.Range(0, renderTexture.width);
            points[i].y = Random.Range(0, renderTexture.height);
            points[i].vx = Random.Range(-1f, 1f);
            points[i].vy = Random.Range(-1f, 1f);
        }

    }

    // Update is called once per frame
    void Update()
    {
    }

    void OnRenderImage(RenderTexture src, RenderTexture dest){
        
        
        //Simulation step: making the blank trail andsetting it to renderTexture
        ComputeBuffer buff = new ComputeBuffer(numPoints, sizeof(float) * 4);
        buff.SetData(points);

        computeShader.SetBuffer(0, "points", buff);
        computeShader.SetTexture(0, "Result", renderTexture);
        computeShader.SetFloat("dt", dt);
        computeShader.SetInt("width", xres);
        computeShader.SetInt("height", yres);
        
        computeShader.Dispatch(0, xres / 16, yres / 16, 1);
        buff.GetData(points);
        buff.Release();

        diffuseFade.SetTexture(0, "Input_Texture", renderTexture);
        diffuseFade.SetTexture(0, "Diff_Faded_Texture", diffFadeTexture);
        diffuseFade.SetInt("width", xres);
        diffuseFade.SetInt("height", yres);
        diffuseFade.SetFloat("diff", diff);
        diffuseFade.SetFloat("fade", fade);
        diffuseFade.SetFloat("dt", dt);
        diffuseFade.Dispatch(0, xres/8, yres/8, 1);

        RenderTexture tmp = renderTexture;
        renderTexture = diffFadeTexture;
        diffFadeTexture = tmp;

        Graphics.Blit(renderTexture, dest);
    }
}

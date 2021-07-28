using System.Collections;
using System.Collections.Generic;
using UnityEngine;

struct Slime{
    public float x;
    public float y;
    public float theta;
};
public class Slime_Takeover : MonoBehaviour
{
    public ComputeShader computeShader;
    public ComputeShader diffuseFade;
    public ComputeShader turnManager;
    RenderTexture renderTexture;
    RenderTexture diffFadeTexture;
    RenderTexture colorizedTexture;
    public int xres;
    public int yres;
    public int numSlimes;
    public float v;
    public float dt;
    public float diff;
    public float fade;
    public float tStrength;
    public float sRange;
    public float sSize;
    public float rad;

    public float sensorOffsetFactor;
    public float sensorAngle;
    public Color color;
    private Slime[] slimes;

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

        if (!colorizedTexture){
            colorizedTexture = new RenderTexture(xres, yres, 24);
            colorizedTexture.enableRandomWrite = true;
            colorizedTexture.Create();
        }

        int x0 = xres / 2;
        int y0 = yres / 2;

        slimes = new Slime[numSlimes];
        for (int i = 0; i < numSlimes; i++){
            Vector2 pos = Random.insideUnitCircle;
            pos *= rad;
            float ang;
            if (pos.y >= 0){
                ang = Mathf.PI + Mathf.PI / 180 * Vector2.Angle(Vector2.right, pos);
            }
            else {
                ang = Mathf.PI - Mathf.PI / 180 * Vector2.Angle(Vector2.right, pos);
            }
            slimes[i].x = x0 + pos.x;
            slimes[i].y = y0 + pos.y;
            slimes[i].theta = Random.Range(ang - Mathf.PI / 10, ang + Mathf.PI / 10);
        }

    }

    // Update is called once per frame
    void Update()
    {
    }

    void OnRenderImage(RenderTexture src, RenderTexture dest){
        
        
        //Simulation step: making the blank trail andsetting it to renderTexture
        ComputeBuffer buff = new ComputeBuffer(numSlimes, sizeof(float) * 3);
        buff.SetData(slimes);

        computeShader.SetBuffer(0, "points", buff);
        computeShader.SetTexture(0, "Result", renderTexture);
        computeShader.SetFloat("dt", dt);
        computeShader.SetFloat("v", v);
        computeShader.SetInt("width", xres);
        computeShader.SetInt("height", yres);
        
        computeShader.Dispatch(0, xres / 16, yres / 16, 1);
        buff.GetData(slimes);
        buff.Release();

        //Evapoffusion of trails
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
        
        //Turning Logic
        ComputeBuffer buff2 = new ComputeBuffer(numSlimes, sizeof(float)*3);
        buff2.SetData(slimes);
        turnManager.SetBuffer(0, "slimes", buff2);
        turnManager.SetTexture(0, "trails", renderTexture);
        turnManager.SetTexture(0, "finalout", colorizedTexture);
        turnManager.SetInt("width", xres);
        turnManager.SetInt("height", yres);
        turnManager.SetFloat("dt", dt);
        turnManager.SetFloat("tStrength", tStrength);
        turnManager.SetFloat("sStrength", sRange);
        turnManager.SetFloat("sSize", sSize);
        float[] cols = {color.r, color.g, color.b, color.a};
        turnManager.SetFloats("finalCol", cols);
        turnManager.SetFloat("sensorOffsetFactor", sensorOffsetFactor);
        turnManager.SetFloat("sensorAngle",sensorAngle);
        turnManager.Dispatch(0, xres / 8, yres / 8, 1);
        buff2.GetData(slimes);

        buff2.Release();

        Graphics.Blit(colorizedTexture, dest);
    }
}

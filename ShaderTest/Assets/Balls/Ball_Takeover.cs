using System.Collections;
using System.Collections.Generic;
using UnityEngine;

struct Ball{
    public float x;
    public float y;
    public float vx;
    public float vy;
};
public class Ball_Takeover : MonoBehaviour
{
    public ComputeShader computeShader;
    RenderTexture renderTexture;
    Ball[] balls;
    public int numBalls;
    public float dt;

    // Start is called before the first frame update
    void Start()
    {
        if (!renderTexture){
            renderTexture = new RenderTexture(256, 256, 24);
            renderTexture.enableRandomWrite = true;
            renderTexture.Create();
        }

        balls = new Ball[numBalls];
        for (int i = 0 ; i < numBalls; i++){
            balls[i].x = 128;
            balls[i].y = 128;
            balls[i].vx = 1;
            balls[i].vy = 0;
        }
    }

    void OnRenderImage(RenderTexture src, RenderTexture dest){
        if (!renderTexture){
            renderTexture = new RenderTexture(256, 256, 24);
            renderTexture.enableRandomWrite = true;
            renderTexture.Create();
        }
        computeShader.SetTexture(0, "CSMain", renderTexture);
        computeShader.SetFloat("dt", dt);
        computeShader.SetInt("width", renderTexture.width);
        computeShader.SetInt("height", renderTexture.height);
        computeShader.SetTexture(0, "Result", renderTexture);
        ComputeBuffer buff = new ComputeBuffer(numBalls, sizeof(float) * 4);
        buff.SetData(balls);
        computeShader.SetBuffer(0, "balls", buff);
        computeShader.Dispatch(0, renderTexture.width / 16, 1, 1);
        buff.GetData(balls);
        buff.Release();

        Graphics.Blit(renderTexture, dest);
    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

struct Id{
    public uint x;
    public uint y;
};

public class ID_Printer : MonoBehaviour
{
    public ComputeShader computeShader;
    Id[] ids;
    public int xres;
    public int yres;

    int numIDs;

    // Start is called before the first frame update
    void Start()
    {

        numIDs = xres * yres;
        ids = new Id[numIDs];
        for (int i = 0 ; i < numIDs; i++){
            ids[i].x = 0;
            ids[i].y = 0;
        }

        doID();
    }

    void doID(){
        ComputeBuffer buff = new ComputeBuffer(numIDs, sizeof(int) * 2);
        buff.SetData(ids);

        computeShader.SetBuffer(0, "ids", buff);
        computeShader.SetInt("width", xres);
        computeShader.Dispatch(0, xres / 8, yres / 8 , 1);

        buff.GetData(ids);
        buff.Release();

        for (int i = 0; i < numIDs; i++){
            Debug.Log("ids[" + i + "].x: " + ids[i].x + "| ids[" + i + "].y: " + ids[i].y);
        }
    }
}

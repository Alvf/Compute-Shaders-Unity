using System.Collections;
using System.Collections.Generic;
using UnityEngine;

struct Cell{
    public float den;
    public float vx;
    public float vy;
    public int boundary;
};
public class Fluid_Manager : MonoBehaviour
{
    public ComputeShader painter, diffuser, projector, celldisplay, vort_conf;
    RenderTexture diffusein, diffuseout;

    public int xres;
    public int yres;
    public float maxStir;

    int numcells;
    Cell[] cells0;
    Cell[] cells1;

    float[] div;
    float[] p0;
    float[] p1;
    public float dt;
    public float diff;
    public float fade;

    public float brushrad;
    public float paintamount;
    public float stirrad;
    Vector2 cur_pos, last_pos;
    public int Diffusion_Jacobi_Steps;
    
    [Range(0, 1)]
    public float sat;
    [Range(0, 1)]
    public float dark;
    public float paintmax;
    public float vorticity;
    public Color color, background, inner, walls;
    public float lower_iso, upper_iso;
    int spacetoggle;
    // Start is called before the first frame update
    void Start(){
        if (!diffusein){
            diffusein = new RenderTexture(xres, yres, 24);
            diffusein.enableRandomWrite = true;
            diffusein.Create();
        }
        if (!diffuseout){
            diffuseout = new RenderTexture(xres, yres, 24);
            diffuseout.enableRandomWrite = true;
            diffuseout.Create();
        }
        numcells = xres * yres;
        cells0 = new Cell[numcells];
        cells1 = new Cell[numcells];
        div = new float[numcells];
        p0 = new float[numcells];
        p1 = new float[numcells];
        for (int i = 0; i < numcells; i ++){
            cells0[i].den = 0;
            cells0[i].vx = 0;
            cells0[i].vy = 0;
            cells0[i].boundary = 0;
            cells1[i].den = 0;
            cells1[i].vx = 0;
            cells1[i].vy = 0;
            cells1[i].boundary = 0;
            p0[i] = 0;
            p1[i] = 0;
            div[i] = 0;
        }
        cur_pos = Input.mousePosition;
        spacetoggle = 0;
    }

    void Update(){
        if (Input.GetKeyUp(KeyCode.Space)){
            spacetoggle = (spacetoggle + 1) % 3;
        }
    }

    void OnRenderImage(RenderTexture src, RenderTexture dest){ 
        // TODO: boundaries in the domain and drawing/displaying walls.
        // Need to call subroutines
        cur_pos = Input.mousePosition;
        screen_interact(cur_pos, last_pos);
        source_placing();
        velocity_step();
        density_step();
        cell_display();
        Graphics.Blit(diffusein, dest);
        last_pos = cur_pos;
    }

    void screen_interact(Vector2 pos, Vector2 prev_pos){
        float[] mousePos = {pos.x / Screen.width * xres, pos.y / Screen.height * yres};
        painter.SetFloats("mousePos", mousePos);
        painter.SetFloat("paintAmount", paintamount);
        painter.SetFloat("paintmax", paintmax);
        painter.SetFloat("brushrad", brushrad);
        painter.SetInt("width", xres);

        if (Input.GetMouseButton(0)){
            ComputeBuffer buff0 = new ComputeBuffer(numcells, sizeof(float) * 4);
            buff0.SetData(cells0);
            painter.SetBuffer(0 , "cells", buff0);
            painter.Dispatch(0, xres/8, yres/8, 1);
            buff0.GetData(cells0);
            buff0.Release();
        }
        if (Input.GetMouseButton(1)){
            ComputeBuffer buff1 = new ComputeBuffer(numcells, sizeof(float) * 4);
            buff1.SetData(cells0);
            painter.SetBuffer(1, "cells", buff1);
            Vector2 dp = Vector2.ClampMagnitude(pos - prev_pos, maxStir);
            float[] dmouse = {dp.x, dp.y};
            painter.SetFloats("dmouse", dmouse);
            painter.SetFloat("stirrad", stirrad);
            painter.Dispatch(1, xres / 8, yres / 8, 1);
            buff1.GetData(cells0);
            buff1.Release();
        }
        if (Input.GetMouseButton(2)){
            ComputeBuffer buff2 = new ComputeBuffer(numcells, sizeof(float) * 4);
            buff2.SetData(cells0);
            painter.SetBuffer(2, "cells", buff2);
            painter.Dispatch(2, xres/8, yres/8, 1);
            buff2.GetData(cells0);
            buff2.Release();
        }
        if (Input.GetKey(KeyCode.Q)){
            ComputeBuffer buff3 = new ComputeBuffer(numcells, sizeof(float) * 4);
            buff3.SetData(cells0);
            painter.SetBuffer(3, "cells", buff3);
            painter.Dispatch(3, xres/8, yres/8, 1);
            buff3.GetData(cells0);
            buff3.Release();
        }
    }
    
    void cell_display(){
        ComputeBuffer buff2 = new ComputeBuffer(numcells, sizeof(float) * 4);
        buff2.SetData(cells0);
        celldisplay.SetInt("width", xres);
        celldisplay.SetInt("height", yres);
        celldisplay.SetFloat("sat", sat);
        celldisplay.SetFloat("dark", dark);
        float[] col = {color.r, color.g, color.b , color.a};
        float[] bkg = {background.r, background.g, background.b, background.a};
        float[] ins = {inner.r, inner.g, inner.b, inner.a};
        float[] wall = {walls.r, walls.g, walls.b, walls.a};
        celldisplay.SetFloats("color", col);
        celldisplay.SetFloats("bkg", bkg);
        celldisplay.SetFloats("inner", ins);
        celldisplay.SetFloats("wall_col", wall);
        celldisplay.SetFloat("lb", lower_iso);
        celldisplay.SetFloat("ub", upper_iso);

        if (spacetoggle == 1){
            celldisplay.SetTexture(1, "Result", diffusein);
            celldisplay.SetBuffer(1, "cells", buff2);
            celldisplay.Dispatch(1, xres/8, yres/8, 1);
        }
        else if (spacetoggle == 2){
            celldisplay.SetTexture(0, "Result", diffusein);
            celldisplay.SetBuffer(0, "cells", buff2);
            celldisplay.Dispatch(0, xres/8, yres/8, 1);
        }
        else{
            celldisplay.SetTexture(2, "Result", diffusein);
            celldisplay.SetBuffer(2, "cells", buff2);
            celldisplay.Dispatch(2, xres/8, yres/8, 1);
        }
        

        buff2.Release();
    }

    void density_step(){
        // setting things in the shader and making the buffer
        ComputeBuffer buff0 = new ComputeBuffer(numcells, sizeof(float) * 4);
        ComputeBuffer buff1 = new ComputeBuffer(numcells, sizeof(float) * 4);
        buff0.SetData(cells0);
        diffuser.SetInt("width", xres);
        diffuser.SetInt("height", yres);
        diffuser.SetFloat("diff", diff);
        diffuser.SetFloat("fade", fade);
        diffuser.SetFloat("dt", dt);

        buff0.SetData(cells0);
        for(int i = 0; i < Diffusion_Jacobi_Steps; i++){ //jacobi loops for diffusion of density
            if (i % 2 == 0){
                diffuser.SetBuffer(0, "cells_in", buff0);
                diffuser.SetBuffer(0, "cells_out", buff1);
                diffuser.SetBuffer(4, "cells_out", buff1);
            }
            else{
                diffuser.SetBuffer(0, "cells_in", buff1);
                diffuser.SetBuffer(0, "cells_out", buff0);
                diffuser.SetBuffer(4, "cells_out", buff0);
            }
            diffuser.Dispatch(0, xres/8, yres/8 , 1); 
            diffuser.Dispatch(4, xres/8, yres/8, 1);
        }
        if (Diffusion_Jacobi_Steps % 2 == 1){
            buff1.GetData(cells0);
        }
        else{
            buff0.GetData(cells0);
        }
        buff0.SetData(cells0); //buff0 now has most updated cell info
        
        // advection
        diffuser.SetBuffer(2, "cells_in", buff0);
        diffuser.SetBuffer(2, "cells_out", buff1);
        diffuser.SetBuffer(4, "cells_out", buff1);
        diffuser.Dispatch(2, xres/8, yres/8, 1);
        diffuser.Dispatch(4, xres/8, yres/8, 1);

        buff1.GetData(cells0); //cells0 now has the most updated cell info

        buff0.Release();
        buff1.Release();
    }

    void velocity_step(){
        ComputeBuffer buff0 = new ComputeBuffer(numcells, sizeof(float) * 4);
        ComputeBuffer buff1 = new ComputeBuffer(numcells, sizeof(float) * 4);
        buff0.SetData(cells0);
        diffuser.SetInt("width", xres);
        diffuser.SetInt("height", yres);
        diffuser.SetFloat("diff", diff);
        diffuser.SetFloat("fade", fade);
        diffuser.SetFloat("dt", dt);

        buff0.SetData(cells0);
        for(int i = 0; i < Diffusion_Jacobi_Steps; i++){ //jacobi loops for diffusion of velocity
            if (i % 2 == 0){
                diffuser.SetBuffer(1, "cells_in", buff0);
                diffuser.SetBuffer(1, "cells_out", buff1);
                diffuser.SetBuffer(5, "cells_out", buff1);
            }
            else{
                diffuser.SetBuffer(1, "cells_in", buff1);
                diffuser.SetBuffer(1, "cells_out", buff0);
                diffuser.SetBuffer(5, "cells_out", buff0);
            }
            diffuser.Dispatch(1, xres/8, yres/8, 1); 
            diffuser.Dispatch(5, xres/8, yres/8, 1);
        }
        if (Diffusion_Jacobi_Steps % 2 == 1){
            buff1.GetData(cells0);
        }
        else{
            buff0.GetData(cells0);
        } //cells0 now has the most updated cell info
        buff0.SetData(cells0); //buff0 now has the most updated cell info
        
        project(buff0);

        //advection
        diffuser.SetBuffer(3, "cells_in", buff0);
        diffuser.SetBuffer(3, "cells_out", buff1);
        diffuser.SetBuffer(5, "cells_out", buff1);
        diffuser.Dispatch(3, xres / 8, yres / 8, 1);
        diffuser.Dispatch(5, xres / 8, yres / 8, 1);

        //vorticity confinement
        vort_conf.SetBuffer(0, "cells", buff1);
        vort_conf.SetFloat("dt", dt);
        vort_conf.SetInt("width", xres);
        vort_conf.SetInt("height", yres);
        vort_conf.SetFloat("vorticity", vorticity);
        vort_conf.Dispatch(0, xres / 8, yres / 8 , 1);

        project(buff1);
        buff1.GetData(cells0);

        buff0.Release();
        buff1.Release();
    }
    void project(ComputeBuffer buff0){ 
        // the goal: take buff0's cells and update buff0 so that it becomes divergence free
        // using buff1 as scratch space
        ComputeBuffer pbuff0 = new ComputeBuffer(numcells, sizeof(float));
        ComputeBuffer pbuff1 = new ComputeBuffer(numcells, sizeof(float));
        ComputeBuffer dbuff = new ComputeBuffer(numcells, sizeof(float));

        projector.SetBuffer(0, "cells_in", buff0);
        projector.SetBuffer(0, "div", dbuff);
        projector.SetBuffer(0, "p_in", pbuff0);

        projector.SetBuffer(5, "div", dbuff);
        projector.SetBuffer(5, "cells_in", buff0);

        projector.SetInt("width", xres);
        projector.SetInt("height", yres);

        projector.Dispatch(0, xres / 8, yres / 8, 1); //dbuff and pbuff0 are now set
        projector.Dispatch(5, xres / 8, yres / 8, 1); //div bound conditions

        projector.SetBuffer(1, "div", dbuff);
        projector.SetBuffer(4, "cells_in", buff0);
        for (int i = 0 ; i < Diffusion_Jacobi_Steps; i++){ //jacobi loops for making p more accurate
            if (i % 2 == 0){
                projector.SetBuffer(1, "p_in", pbuff0);
                projector.SetBuffer(1, "p_out", pbuff1);
                projector.SetBuffer(4, "p_out", pbuff1);
            }
            else{
                projector.SetBuffer(1, "p_in", pbuff1);
                projector.SetBuffer(1, "p_out", pbuff0);
                projector.SetBuffer(4, "p_out", pbuff0);
            }
            projector.Dispatch(1, xres/8, yres/8, 1);
            projector.Dispatch(4, xres/8, yres/8, 1); 
        }
        if (Diffusion_Jacobi_Steps % 2 == 1){
            pbuff1.GetData(p0);
        }
        else{
            pbuff0.GetData(p0);
        } //p0 now has the most updated p info
        pbuff0.SetData(p0); //pbuff0 now has the most updated p info

        projector.SetBuffer(2, "p_in", pbuff0);
        projector.SetBuffer(2, "cells_in", buff0);
        projector.SetBuffer(3, "cells_in", buff0);
        projector.Dispatch(2, xres/8, yres/8, 1); //buff 0 has now been projected.
        projector.Dispatch(3, xres/8, yres/8, 1); //boundary conditions post projection

        pbuff0.Release();
        pbuff1.Release();
        dbuff.Release();
    }
    void source_placing(){
        for (int i = -30 ; i <= 30; i++){
            for (int j = -30; j <= 30; j++){
                cells0[xres/2 + i + (50 + j) * xres].den = 1;
                cells0[xres/2 + i + (60 + j) * xres].vy = 1 + Random.Range(-1f, 1f);
            }
        }
    }
}

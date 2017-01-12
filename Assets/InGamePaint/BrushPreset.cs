using UnityEngine;
using System.Collections;

public class BrushPreset : Clickable {

    //public int brushSize = 50;
    public Texture2D brushTip;
    public float opacityFade = 0f, smudgeStrength = 0f, brushSpacing = 0.5f;
	
    void Start()
    {
        GetComponent<Renderer>().material.mainTexture = brushTip;
    }

}

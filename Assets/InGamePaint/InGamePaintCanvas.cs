using UnityEngine;
using System;

[DisallowMultipleComponent]
[RequireComponent (typeof (MeshCollider))]
[RequireComponent(typeof(MeshFilter))]

/// <summary>
/// Adds a paintable main texture to a GameObject
/// </summary>
public class InGamePaintCanvas : MonoBehaviour
{

    public int resolutionX = 1024;
    public int resolutionY = 1024;

    public Color backgroundColor = Color.white;

    protected Texture2D canvasTexture;

    /// <summary>
    /// Initialize texture to paint on and set it as main texture
    /// </summary>
    void Start()
    {
        canvasTexture = new Texture2D(resolutionX, resolutionY);
        GetComponent<Renderer>().material.mainTexture = canvasTexture;
        GetComponent<MeshCollider>().sharedMesh = GetComponent<MeshFilter>().sharedMesh;
        Clear(backgroundColor);
    }

    /// <summary>
    /// Updates the texture on the object
    /// </summary>
    void Update()
    {
        canvasTexture.Apply();
    }

    public void PaintPixel(int x, int y, Color color)
    {
        canvasTexture.SetPixel(x, y, color);
    }

    public void PaintPixelUv(Vector2 uvCoords, Color color)
    {
        PaintPixel(Mathf.RoundToInt(uvCoords.x * resolutionX), Mathf.RoundToInt(uvCoords.y * resolutionY), color);
    }

    public void PaintTexture(int x, int y, Texture2D texture)
    {
        PaintTexture(x, y, texture, texture);
    }

    public void PaintTexture(int x, int y, Texture2D alphaTexture, Texture2D colorTexture)
    {

        int brushWidth = alphaTexture.width;
        int brushHeight = alphaTexture.height;
        x -= brushWidth / 2;
        y -= brushHeight / 2;

        int brushSourceX = 0;
        int brushSourceY = 0;

        if (x < 0)
        {
            // brush is going over left border
            brushSourceX = Mathf.Abs(x);
            brushWidth -= brushSourceX;
            x = 0;
        }
        else if (x + brushWidth > canvasTexture.width)
        {
            // bush is going over right border
            brushWidth -= x + brushWidth - canvasTexture.width;
        }
        if (y + brushHeight > canvasTexture.height)
        {
            // brush is going over upper border
            brushHeight -= y + brushHeight - canvasTexture.height;
        }
        else if (y < 0)
        {
            // brush is going over lower border
            brushSourceY = Mathf.Abs(y);
            brushHeight -= brushSourceY;
            y = 0;
        }

        Color[] alphaTexturePixels = alphaTexture.GetPixels(brushSourceX, brushSourceY, brushWidth, brushHeight);
        Color[] colorTexturePixels = colorTexture.GetPixels(brushSourceX, brushSourceY, brushWidth, brushHeight);
        Color[] sourcePixels = canvasTexture.GetPixels(x, y, brushWidth, brushHeight);
        Color[] paintPixels = new Color[alphaTexturePixels.Length];

        // create paint texture (mix the RGBA of the source rectangle with the brush color depending on brush alpha)
        for (int i = 0; i < alphaTexturePixels.Length; i++)
        {
            float alpha = alphaTexturePixels[i].a * colorTexturePixels[i].a;
            paintPixels[i].r = Mathf.Lerp(sourcePixels[i].r, colorTexturePixels[i].r, alpha);
            paintPixels[i].g = Mathf.Lerp(sourcePixels[i].g, colorTexturePixels[i].g, alpha);
            paintPixels[i].b = Mathf.Lerp(sourcePixels[i].b, colorTexturePixels[i].b, alpha);
            paintPixels[i].a = sourcePixels[i].a + alpha;
        }
        Texture2D paintTexture = new Texture2D(brushWidth, brushHeight);
        paintTexture.SetPixels(paintPixels);

        // add paint texture to canvas texture
        Graphics.CopyTexture(paintTexture, 0, 0, 0, 0, brushWidth, brushHeight, canvasTexture, 0, 0, x, y);

    }

    public void PaintTextureUv(Vector2 uvCoordsCenter, Texture2D texture)
    {
        PaintTextureUv(uvCoordsCenter, texture, texture);
    }

    public void PaintTextureUv(Vector2 uvCoordsCenter, Texture2D alphaTexture, Texture2D colorTexture)
    {
        PaintTexture(Mathf.RoundToInt(uvCoordsCenter.x * resolutionX), Mathf.RoundToInt(uvCoordsCenter.y * resolutionY), alphaTexture, colorTexture);
    }

    public void Clear(Color color)
    {
        Color[] col = new Color[resolutionX * resolutionY];

        for (int i = 0; i < col.Length; i++)
        {
            col[i] = color;
        }

        canvasTexture.SetPixels(col);
    }

    //public void PaintTexture(int x, int y, Texture2D texture)
    //{
        //canvasTexture.SetPixels(...)
    //}
}

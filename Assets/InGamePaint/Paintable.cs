using UnityEngine;
using System;
using System.IO;

namespace InGamePaint
{

    [DisallowMultipleComponent]
    [RequireComponent(typeof(MeshCollider))]
    [RequireComponent(typeof(MeshFilter))]

    /// <summary>
    /// Makes the main texture of a GameObject paintable or, if none is present, adds a new paintable main texture
    /// based on resolutionX, resolutionY and backgroundColor fields
    /// </summary>
    public class Paintable : MonoBehaviour
    {

        /// <summary>
        /// X resolution of the new texture (if no main texture is present on the material)
        /// </summary>
        public int resolutionX = 1024;

        /// <summary>
        /// Y resolution of the new texture (if no main texture is present on the material)
        /// </summary>
        public int resolutionY = 1024;

        /// <summary>
        /// Background color of the new texture (if no main texture is present on the material)
        /// </summary>
        public Color backgroundColor = Color.white;

        /// <summary>
        /// Locks the texture from destructive painting operations
        /// </summary>
        public bool locked = false;

        protected bool changed, hasUnsavedChanges;

        public bool HasUnsavedChanges
        {
            get
            {
                return hasUnsavedChanges;
            }
        }

        protected Texture2D texture;

        /// <summary>
        /// Initialize texture to paint on and set it as main texture
        /// </summary>
        protected void Start()
        {
            Material material = GetComponent<Renderer>().material;

            if (material.mainTexture == null)
            {
                texture = new Texture2D(resolutionX, resolutionY);
                material.mainTexture = texture;
                Clear(backgroundColor);
            } else
            {
                // copy original texture
                resolutionX = material.mainTexture.width;
                resolutionY = material.mainTexture.height;
                texture = new Texture2D(resolutionX, resolutionY);
                texture.SetPixels(((Texture2D)material.mainTexture).GetPixels());
                material.mainTexture = texture;
            }
            GetComponent<MeshCollider>().sharedMesh = GetComponent<MeshFilter>().sharedMesh;

            changed = true;
            hasUnsavedChanges = false;
        }

        /// <summary>
        /// Updates the texture on the object
        /// </summary>
        protected void Update()
        {
            if (texture != null && changed)
            {
                texture.Apply();
                changed = false;
            }
        }

        /// <summary>
        /// Paint a single pixel
        /// </summary>
        /// <param name="x">X coordinate on the paintable texture</param>
        /// <param name="y">Y coordinate on the paintable texture</param>
        /// <param name="color">Color of the pixel</param>
        public void PaintPixel(int x, int y, Color color)
        {
            if (!locked)
            {
                texture.SetPixel(x, y, color);
            }
            hasUnsavedChanges = true;
            changed = true;
        }

        /// <summary>
        /// Paint a single pixel
        /// </summary>
        /// <param name="coords"></param>
        /// <param name="color">Color of the pixel</param>
        public void PaintPixel(Vector2 coords, Color color)
        {
            PaintPixel(Mathf.RoundToInt(coords.x * resolutionX), Mathf.RoundToInt(coords.y * resolutionY), color);
        }

        /// <summary>
        /// Paint a texture
        /// </summary>
        /// <param name="x">X coordinate of the center point of the texture on the Paintable</param>
        /// <param name="y">Y coordinate of the center point of the texture on the Paintable</param>
        /// <param name="texture">Texture that will be painted</param>
        public void PaintTexture(int x, int y, Texture2D texture)
        {
            PaintTexture(x, y, texture, texture);
        }

        /// <summary>
        /// Paint a texture with specific alpha and color components
        /// </summary>
        /// <param name="x">Center point of the texture on the Paintable</param>
        /// <param name="y">Center point of the texture on the Paintable</param>
        /// <param name="alphaTexture">Alpha component of the texture</param>
        /// <param name="colorTexture">Color component of the texture</param>
        public void PaintTexture(int x, int y, Texture2D alphaTexture, Texture2D colorTexture)
        {
            if (!locked)
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
                else if (x + brushWidth > texture.width)
                {
                    // bush is going over right border
                    brushWidth -= x + brushWidth - texture.width;
                }
                if (y + brushHeight > texture.height)
                {
                    // brush is going over upper border
                    brushHeight -= y + brushHeight - texture.height;
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
                Color[] sourcePixels = texture.GetPixels(x, y, brushWidth, brushHeight);
                Color[] paintPixels = new Color[alphaTexturePixels.Length];

                // create paint texture (mix the RGBA of the source rectangle with the brush color depending on brush alpha)
                for (int i = 0; i < alphaTexturePixels.Length; i++)
                {
                    float alpha = alphaTexturePixels[i].a * colorTexturePixels[i].a;
                    float colorMix = alpha;
                    // if the pixel is fully transparent, colorMix will be at least 0.5
                    //if (sourcePixels[i].a == 0)
                    //{
                    colorMix = Mathf.Min(1, colorMix / sourcePixels[i].a);
                    //}
                    paintPixels[i].r = Mathf.Lerp(sourcePixels[i].r, colorTexturePixels[i].r, colorMix);
                    paintPixels[i].g = Mathf.Lerp(sourcePixels[i].g, colorTexturePixels[i].g, colorMix);
                    paintPixels[i].b = Mathf.Lerp(sourcePixels[i].b, colorTexturePixels[i].b, colorMix);
                    paintPixels[i].a = sourcePixels[i].a + alpha;
                }
                Texture2D paintTexture = new Texture2D(brushWidth, brushHeight);
                paintTexture.SetPixels(paintPixels);

                // add paint texture to canvas texture
                Graphics.CopyTexture(paintTexture, 0, 0, 0, 0, brushWidth, brushHeight, texture, 0, 0, x, y);

                hasUnsavedChanges = true;
                changed = true;
            }
        }

        /// <summary>
        /// Paint a texture
        /// </summary>
        /// <param name="coordsCenter">UV coordinates of the center point of the texture on the Paintable</param>
        /// <param name="texture">Texture that will be painted</param>
        public void PaintTexture(Vector2 coordsCenter, Texture2D texture)
        {
            PaintTexture(coordsCenter, texture, texture);
        }

        /// <summary>
        /// Paint a texture with specific alpha and color components
        /// </summary>
        /// <param name="coordsCenter">UV coordinates of the center point of the texture on the Paintable</param>
        /// <param name="alphaTexture">Alpha component of the texture</param>
        /// <param name="colorTexture">Color component of the texture</param>
        public void PaintTexture(Vector2 coordsCenter, Texture2D alphaTexture, Texture2D colorTexture)
        {
            PaintTexture(Mathf.RoundToInt(coordsCenter.x), Mathf.RoundToInt(coordsCenter.y), alphaTexture, colorTexture);
        }

        /// <summary>
        /// Fill whole texture with solid color
        /// </summary>
        /// <param name="color">Texture will be filled with this color</param>
        public void Clear(Color color)
        {
            if (!locked)
            {
                Color[] col = new Color[resolutionX * resolutionY];

                for (int i = 0; i < col.Length; i++)
                {
                    col[i] = color;
                }

                texture.SetPixels(col);
                changed = true;
                hasUnsavedChanges = true;
            }
        }

        /// <summary>
        /// Pick color in specified radius. If radius is larger than 1 pixel, an average color of all pixels will be picked
        /// </summary>
        /// <param name="coordsCenter">Center of the color picking radius</param>
        /// <param name="radius">Not yet implemented</param>
        /// <returns></returns>
        public Color PickColor(Vector2 coordsCenter, float radius)
        {
            // TODO: implement radius
            return texture.GetPixel(Mathf.RoundToInt(coordsCenter.x), Mathf.RoundToInt(coordsCenter.y));
        }

        public Vector2 Uv2Pixel(Vector2 uvCoords)
        {
            return new Vector2(uvCoords.x * resolutionX, uvCoords.y * resolutionY);
        }

        public string SaveToFile()
        {
            string path = Application.persistentDataPath + "/" + name + "_" + DateTime.Now.ToString("MM-dd-yyyy-hh-mm") + ".png";
            File.WriteAllBytes(path, texture.EncodeToPNG());
            return path;
        }

    }

}
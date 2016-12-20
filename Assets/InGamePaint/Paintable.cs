using UnityEngine;
using System;
using System.IO;
using System.Collections.Generic;

namespace InGamePaint
{

    [DisallowMultipleComponent]
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
            set
            {
                hasUnsavedChanges = value;
            }
        }

        protected Texture2D texture;

        protected Color32[] texturePixels;

        /// <summary>
        /// Initialize texture to paint on and set it as main texture
        /// </summary>
        protected void Start()
        {
            Material material = GetComponent<Renderer>().material;

            if (material.mainTexture == null)
            {
                texture = new Texture2D(resolutionX, resolutionY, TextureFormat.ARGB32, false);
                material.mainTexture = texture;
                Clear(backgroundColor);
            } else
            {
                // copy original texture
                resolutionX = material.mainTexture.width;
                resolutionY = material.mainTexture.height;
                texture = new Texture2D(resolutionX, resolutionY, TextureFormat.ARGB32, false);
                texture.SetPixels32(((Texture2D)material.mainTexture).GetPixels32());
                material.mainTexture = texture;
            }
            MeshCollider meshCollider = gameObject.AddComponent<MeshCollider>();
            meshCollider.sharedMesh = GetComponent<MeshFilter>().sharedMesh;

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
        /// <param name="texturePixels">Texture that will be painted</param>
        public void PaintTexture(int x, int y, int brushWidth, int brushHeight, Color32[] texturePixels)
        {
            PaintTexture(x, y, brushWidth, brushHeight, texturePixels, texturePixels);
        }

        /// <summary>
        /// Paint a texture with specific alpha and color components
        /// </summary>
        /// <param name="x">Center point of the texture on the Paintable</param>
        /// <param name="y">Center point of the texture on the Paintable</param>
        /// <param name="alphaTexture">Alpha component of the texture</param>
        /// <param name="colorTexture">Color component of the texture</param>
        public void PaintTexture(int inputX, int inputY, int inputWidth, int inputHeight, Color32[] alphaTexture, Color32[] colorTexture)
        {
            if (!locked)
            {

                inputX -= inputWidth / 2;
                inputY -= inputHeight / 2;

                int x = inputX;
                int y = inputY;

                int brushSourceX = 0;
                int brushSourceY = 0;

                int width = inputWidth;
                int height = inputHeight;

                /*
                 * Check if brush is moving over a border and adjust brush coordinates accordingly
                 */
                if (x < 0)
                {
                    // brush is going over left border
                    brushSourceX = Mathf.Abs(x);
                    width -= brushSourceX;
                    x = 0;
                }
                else if (x + width > texture.width)
                {
                    // bush is going over right border
                    width -= x + width - texture.width;
                }
                if (y + height > texture.height)
                {
                    // brush is going over upper border
                    height -= y + height - texture.height;
                }
                else if (y < 0)
                {
                    // brush is going over lower border
                    brushSourceY = Mathf.Abs(y);
                    height -= brushSourceY;
                    y = 0;
                }

                /*
                 * Calculate new color values
                 */
                Color32[] paintPixels = new Color32[width * height];
                Color32[] sourcePixels = texture.GetPixels32();
                int sourceHeight = texture.height;
                int paintIndex = 0;

                for (int paintY = y; paintY < y + height; paintY++)
                {
                    for (int paintX = x; paintX < x + width; paintX++)
                    {
                        int sourceIndex = paintY * sourceHeight + paintX;
                        int brushIndex = (paintY - inputY) * inputHeight + paintX - inputX;

                        float sourceAlpha = sourcePixels[sourceIndex].a / 255;
                        float brushAlpha = (float)alphaTexture[brushIndex].a / 255;
                        float colorMix = brushAlpha;

                        if (brushAlpha > 0 && sourceAlpha > 0)
                        {
                            colorMix = Mathf.Min(1, colorMix * sourceAlpha);
                        }
                        else
                        {
                            // we're drawing on a fully transparent surface, so the color value should be multiplied to prevent bright or dark edges
                            colorMix = Mathf.Min(1, colorMix * 4);
                            brushAlpha = Mathf.Min(1, sourceAlpha + brushAlpha);
                        }

                        paintPixels[paintIndex] = Color32.Lerp(sourcePixels[sourceIndex], colorTexture[brushIndex], colorMix);
                        if (brushAlpha != colorMix)
                        {
                            paintPixels[paintIndex].a = System.Convert.ToByte(brushAlpha * 255);
                        }

                        paintIndex++;
                    }
                }

                Texture2D paintTexture = new Texture2D(width, height);
                paintTexture.SetPixels32(paintPixels);

                // add paint texture to canvas texture
                Graphics.CopyTexture(paintTexture, 0, 0, 0, 0, width, height, texture, 0, 0, x, y);

                hasUnsavedChanges = true;
                changed = true;
            }
        }

        /// <summary>
        /// Paint a texture
        /// </summary>
        /// <param name="coordsCenter">UV coordinates of the center point of the texture on the Paintable</param>
        /// <param name="texture">Texture that will be painted</param>
        public void PaintTexture(Vector2 coordsCenter, int brushWidth, int brushHeight, Color32[] texture)
        {
            PaintTexture(coordsCenter, brushWidth, brushHeight, texture, texture);
        }

        /// <summary>
        /// Paint a texture with specific alpha and color components
        /// </summary>
        /// <param name="coordsCenter">UV coordinates of the center point of the texture on the Paintable</param>
        /// <param name="alphaTexture">Alpha component of the texture</param>
        /// <param name="colorTexture">Color component of the texture</param>
        public void PaintTexture(Vector2 coordsCenter, int brushWidth, int brushHeight, Color32[] alphaTexture, Color32[] colorTexture)
        {
            PaintTexture(Mathf.RoundToInt(coordsCenter.x), Mathf.RoundToInt(coordsCenter.y), brushWidth, brushHeight, alphaTexture, colorTexture);
        }

        /// <summary>
        /// Fill whole texture with solid color
        /// </summary>
        /// <param name="color">Texture will be filled with this color</param>
        public void Clear(Color32 color)
        {
            if (!locked)
            {
                Color32[] col = new Color32[resolutionX * resolutionY];

                for (int i = 0; i < col.Length; i++)
                {
                    col[i] = color;
                }

                texture.SetPixels32(col);
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
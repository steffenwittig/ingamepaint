using UnityEngine;
using System;
using System.IO;

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
        /// Amplifies the smudge effect of a brush
        /// </summary>
        public float smudgeMultiplier = 0;

        /// <summary>
        /// Requests a minimal smudge value from a brush
        /// </summary>
        public float minSmudgeStrength = 0;

        /// <summary>
        /// Locks the texture from destructive painting operations
        /// </summary>
        public bool locked = false;

        /// <summary>
        /// Was the texture changed last frame?
        /// </summary>
        protected bool changed;

        /// <summary>
        /// Was the texture changed since it was last saved?
        /// </summary>
        protected bool hasUnsavedChanges;

        /// <summary>
        /// Was the texture changed since it was last saved?
        /// </summary>
        public bool HasUnsavedChanges
        {
            get
            {
                return hasUnsavedChanges;
            }
        }

        /// <summary>
        /// Texture of the paintable
        /// </summary>
        protected Texture2D texture;

        /// <summary>
        /// Initialize texture to paint on and set it as main texture, add MeshCollider and initialize internal values
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
            MeshCollider meshCollider = gameObject.AddComponent<MeshCollider>();
            meshCollider.sharedMesh = GetComponent<MeshFilter>().sharedMesh;

            changed = true;
            hasUnsavedChanges = false;
        }

        /// <summary>
        /// Updates the texture on the object if it was changed
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
        /// Paint a single pixel at the specified coordinates with the given color
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
        /// Paint a single pixel at the specified coordinates with the given color
        /// </summary>
        /// <param name="coords">X and Y coordinates on the paintable texture</param>
        /// <param name="color">Color of the pixel</param>
        public void PaintPixel(Vector2 coords, Color color)
        {
            PaintPixel(Mathf.RoundToInt(coords.x * resolutionX), Mathf.RoundToInt(coords.y * resolutionY), color);
        }

        /// <summary>
        /// Paint a texture with specific alpha and color components
        /// </summary>
        /// <param name="x">Center point of the texture on the Paintable</param>
        /// <param name="y">Center point of the texture on the Paintable</param>
        /// <param name="alphaTexture">Alpha component of the texture</param>
        /// <param name="color">Fill color of the texture</param>
        public void PaintTexture(int x, int y, Texture2D alphaTexture, Color color)
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
                Color[] sourcePixels = texture.GetPixels(x, y, brushWidth, brushHeight);
                Color[] paintPixels = new Color[alphaTexturePixels.Length];

                // create paint texture (mix the RGBA of the source rectangle with the brush color depending on brush alpha)
                for (int i = 0; i < alphaTexturePixels.Length; i++)
                {
                    float alpha = alphaTexturePixels[i].a * color.a;
                    float colorMix = alpha;
                    // if the pixel is fully transparent, colorMix will be at least 0.5
                    //if (sourcePixels[i].a == 0)
                    //{
                    colorMix = Mathf.Min(1, colorMix / sourcePixels[i].a);
                    //}
                    paintPixels[i].r = Mathf.Lerp(sourcePixels[i].r, color.r, colorMix);
                    paintPixels[i].g = Mathf.Lerp(sourcePixels[i].g, color.g, colorMix);
                    paintPixels[i].b = Mathf.Lerp(sourcePixels[i].b, color.b, colorMix);
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
        /// Paint a texture with specific alpha and color components
        /// </summary>
        /// <param name="coordsCenter">X and Y coordinates of the center point of the texture on the Paintable</param>
        /// <param name="alphaTexture">Alpha component of the texture</param>
        /// <param name="colorTexture">Color component of the texture</param>
        public void PaintTexture(Vector2 coordsCenter, Texture2D alphaTexture, Color color)
        {
            PaintTexture(Mathf.RoundToInt(coordsCenter.x), Mathf.RoundToInt(coordsCenter.y), alphaTexture, color);
        }

        /// <summary>
        /// Replace all pixels of the texture with given color
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
        /// Pick color in specified radius. If radius is larger than 1 pixel, an average color of all pixels in the square will be picked
        /// </summary>
        /// <param name="coordsCenter">Center of the color picking area</param>
        /// <param name="radius">Actually the width and height of a square, not a radius - defines the area to sample the average color</param>
        /// <returns></returns>
        public Color PickColor(Vector2 coordsCenter, int radius)
        {

            if (radius > 1)
            {
                // TODO: radius should result in a square area... fix or rename it
                int pickX = Mathf.Max(Mathf.RoundToInt(coordsCenter.x - radius / 2), 0);
                int pickY = Mathf.Max(Mathf.RoundToInt(coordsCenter.y - radius / 2), 0);
                int pickXmax = Mathf.Min(Mathf.RoundToInt(coordsCenter.x + radius / 2), texture.width);
                int pickYmax = Mathf.Min(Mathf.RoundToInt(coordsCenter.y + radius / 2), texture.height);

                Color[] pixels = texture.GetPixels(pickX, pickY, pickXmax - pickX, pickYmax - pickY);

                float sumR = 0, sumG = 0, sumB = 0, sumA = 0;

                foreach (Color pixel in pixels)
                {
                    sumR += pixel.r;
                    sumG += pixel.g;
                    sumB += pixel.b;
                    sumA += pixel.a;
                }

                float avgA = sumA / pixels.Length;

                return new Color(
                    sumR / pixels.Length,
                    sumG / pixels.Length,
                    sumB / pixels.Length,
                    avgA);

            } else
            {
                return texture.GetPixel(Mathf.RoundToInt(coordsCenter.x), Mathf.RoundToInt(coordsCenter.y));
            }
        }

        /// <summary>
        /// Transforms UV coordinates to XY texture coordinates
        /// </summary>
        /// <param name="uvCoords">UV coordinates</param>
        /// <returns>XY texture coordinate</returns>
        public Vector2 Uv2Pixel(Vector2 uvCoords)
        {
            return new Vector2(uvCoords.x * resolutionX, uvCoords.y * resolutionY);
        }

        /// <summary>
        /// Saves the texture to a file
        /// </summary>
        /// <returns>Filename with path</returns>
        public string SaveToFile()
        {
            string path = Application.persistentDataPath + "/" + name + "_" + DateTime.Now.ToString("MM-dd-yyyy-hh-mm") + ".png";
            File.WriteAllBytes(path, texture.EncodeToPNG());
            return path;
        }

    }

}
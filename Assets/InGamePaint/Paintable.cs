using UnityEngine;
using System;
using System.IO;
using System.Collections.Generic;
using System.Threading;

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
        public Color32 backgroundColor = Color.white;

        /// <summary>
        /// Locks the texture from destructive painting operations
        /// </summary>
        public bool locked = false;

        protected bool changed, hasUnsavedChanges;

        protected Queue<PaintTask> tasks = new Queue<PaintTask>();
        protected System.Object taskLock = new System.Object();
        protected Thread thread = null;
        protected bool doProcessTasks;
        protected Color32[] updatedTexturePixels;
        protected Texture2D texture;

        public bool HasUnsavedChanges
        {
            get
            {
                return hasUnsavedChanges;
            }
        }

        public Color32[] Pixels
        {
            get
            {
                return updatedTexturePixels;
            }
            set
            {
                updatedTexturePixels = value;
            }
        }

        public int Width
        {
            get
            {
                return resolutionX;
            }
        }

        public int Height
        {
            get
            {
                return resolutionY;
            }
        }

        /// <summary>
        /// Initialize texture to paint on and set it as main texture
        /// </summary>
        protected void Start()
        {
            Material material = GetComponent<Renderer>().material;

            if (material.mainTexture == null)
            {
                texture = new Texture2D(Width, Height);
                material.mainTexture = texture;
                Clear(backgroundColor);
            }
            else
            {
                // copy original texture
                resolutionX = material.mainTexture.width;
                resolutionY = material.mainTexture.height;
                texture = new Texture2D(resolutionX, resolutionY);
                texture.SetPixels(((Texture2D)material.mainTexture).GetPixels());
                texture.Apply();
                material.mainTexture = texture;
            }
            MeshCollider meshCollider = gameObject.AddComponent<MeshCollider>();
            meshCollider.sharedMesh = GetComponent<MeshFilter>().sharedMesh;

            changed = true;
            hasUnsavedChanges = false;

            doProcessTasks = true;
            thread = new Thread(new ThreadStart(ProcessTasks));
            thread.IsBackground = true;
            thread.Start();
        }

        /// <summary>
        /// Updates the texture on the object
        /// </summary>
        protected void Update()
        {

            if (updatedTexturePixels != null && updatedTexturePixels.Length > 0)
            {
                texture.SetPixels32(updatedTexturePixels);
                texture.Apply();
                changed = false;
            }
        }

        protected void OnDestroy()
        {
            doProcessTasks = false;
            try
            {
                if (this.thread != null && this.thread.IsAlive)
                {
                    Debug.Log("Terminate thread.");
                    this.thread.Abort();
                }
            }
            catch (Exception e)
            {
                Debug.Log(e.Message);
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
                tasks.Enqueue(new PaintTask(this, x, y, colorTexture, alphaTexture));
                hasUnsavedChanges = true;
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
                Color32[] col = new Color32[resolutionX * resolutionY];

                for (int i = 0; i < col.Length; i++)
                {
                    col[i] = color;
                }

                //texture.SetPixels(col);
                updatedTexturePixels = col;
                //changed = true;
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

        protected void ProcessTasks()
        {
            while (doProcessTasks)
            {
                try
                {
                    if (tasks.Count > 0)
                    {
                        PaintTask task = tasks.Dequeue();
                        task.Process();
                    }
                }
                catch (Exception e)
                {
                    Debug.Log(e.ToString());
                    doProcessTasks = false;
                    // something is seriously wrong
                }
            }
            
        }

    }

}
using UnityEngine;
using System.Collections.Generic;
using UnityEditor;

namespace InGamePaint
{
    public abstract class Brush : MonoBehaviour
    {

        /// <summary>
        /// Alpha texture of the brush
        /// </summary>
        public Texture2D brushTip;
        public float opacityFade = 0.1f, smudgeStrength = 0.2f, brushSpacing = 0.4f;
        public int brushSize;

        public float currentPaintableDistance;
        protected Color color = Color.black;
        protected Texture2D brushAlphaOriginal;
        protected Paintable currentPaintable, lastPaintable;
        protected Clickable currentClickable;
        protected Vector2 currentPaintableCoords, lastPaintableCoords;
        protected Vector3 shapeDisplayInitScale;
        protected Renderer colorDisplayRenderer, shapeDisplayRenderer;
        protected bool paintedLastFrame = false, showHelp = true;

        /// <summary>
        /// Initialize brush textures
        /// </summary>
        protected virtual void Start()
        {
            if (brushTip == null)
            {
                brushTip = new Texture2D(2, 2); // 1x1 pixel texture causes errors in TextureScale
                brushTip.SetPixels(new Color[] { color, color, color, color });
            }

            brushAlphaOriginal = brushTip;
        }

        /// <summary>
        /// Create new brush texture, after new color and/or opacity values were set
        /// </summary>
        virtual protected void ApplyBrushSettings()
        {
            // Clone and scale texture
            if (brushTip.width != DynamicBrushSize)
            {
                brushTip = new Texture2D(brushAlphaOriginal.width, brushAlphaOriginal.height);
                brushTip.SetPixels(brushAlphaOriginal.GetPixels());
                TextureScale.Bilinear(brushTip, DynamicBrushSize, DynamicBrushSize);
            }
        }

        /// <summary>
        /// Set the paintable object and UV coordinates
        /// </summary>
        protected void UpdatePaintableCoords()
        {
            lastPaintable = currentPaintable;
            lastPaintableCoords = currentPaintableCoords;

            RaycastHit hit;
            Ray ray = GetRay();
            Debug.DrawRay(transform.position, ray.direction * RayDistance, Color.yellow, 0.2f, false);

            Paintable hitPaintable = null;
            currentClickable = null;

            if (Physics.Raycast(ray, out hit, RayDistance))
            {
                GameObject go = hit.collider.gameObject;
                hitPaintable = go.GetComponent<Paintable>();
                currentClickable = go.GetComponent<Clickable>();
            }

            if (hitPaintable != null)
            {
                currentPaintableDistance = hit.distance;
                currentPaintable = hitPaintable;
                currentPaintableCoords = currentPaintable.Uv2Pixel(hit.textureCoord);
            }
            else
            {
                currentPaintable = null;
            }
        }

        /// <summary>
        /// Paint at the current UV coordinates on a paintable
        /// </summary>
        protected void Paint()
        {

            bool painted = false;

            if (paintedLastFrame && currentPaintable == lastPaintable)
            {
                // paint interpolated brush tips between the last painted coords and the current cords
                float distance = Vector2.Distance(lastPaintableCoords, currentPaintableCoords);
                int paintTips = Mathf.RoundToInt(distance / DynamicBrushSize / brushSpacing);
                if (paintTips > 0)
                {
                    for (int i = 1; i <= paintTips; i++)
                    {
                        PaintTexture(Vector2.Lerp(lastPaintableCoords, currentPaintableCoords, (float)i / paintTips));
                        painted = true;
                    }
                }
            }

            if (!painted && (!paintedLastFrame || currentPaintableCoords != lastPaintableCoords))
            {
                // paint a single brush tip at the current coords if we didn't paint at that position last frame
                PaintTexture();
                painted = true;
            }

            paintedLastFrame = painted;
        }

        /// <summary>
        /// Paint a texture at the current UV coordinates on a paintable
        /// </summary>
        protected void PaintTexture()
        {
            PaintTexture(currentPaintableCoords);
        }

        /// <summary>
        /// Paint a texture at the specified coordinates on a paintable
        /// </summary>
        /// <param name="coords"></param>
        protected void PaintTexture(Vector2 coords)
        {
            Color previousColor = currentPaintable.PickColor(coords, Mathf.RoundToInt(brushSize/2)); // Picks the color for the center quarter of the brush
            currentPaintable.PaintTexture(coords, brushTip, color);
            AddColor(previousColor, smudgeStrength/10, true); // Apply the picked color to the brush
            BrushOpacity -= opacityFade/250; // Reduce the opacity
        }

        protected void AddColor(Color addColor, float intensity, bool ignoreOpacity)
        {
            float alpha = color.a;
            color = Color.Lerp(color, addColor, Mathf.Min(1,Mathf.Max(0, intensity)));
            if (ignoreOpacity)
            {
                color.a = alpha;
            }
            ApplyBrushSettings();
        }

        protected void ApplyPreset(BrushPreset preset)
        {
            //brushSize = preset.brushSize;
            smudgeStrength = preset.smudgeStrength;
            opacityFade = preset.opacityFade;
            brushSpacing = preset.brushSpacing;
            brushTip = brushAlphaOriginal = preset.brushTip;
            ApplyBrushSettings();
        }

        /// <summary>
        /// The color that the brush will apply on paintables
        /// </summary>
        public Color BrushColor
        {
            get
            {
                return color;
            }
            set
            {
                color = new Color(
                    value.r,
                    value.g,
                    value.b,
                    color.a);
                ApplyBrushSettings();
            }
        }

        /// <summary>
        /// The opacity of the brush color
        /// </summary>
        public float BrushOpacity
        {
            get
            {
                return color.a;
            }
            set
            {
                color.a = Mathf.Max(0, Mathf.Min(value, 1));
                ApplyBrushSettings();
            }
        }

        /// <summary>
        /// Set size of the brush in pixels
        /// </summary>
        public int MaxBrushSize
        {
            set
            {
                brushSize = Mathf.Max(value, 1);
                ApplyBrushSettings();
            }
            get
            {
                return brushSize;
            }
        }

        /// <summary>
        /// Get the size of the brush in pixels, can be overridden in child classes
        /// to return dynamic brush sizes
        /// </summary>
        virtual public int DynamicBrushSize
        {
            get
            {
                return brushSize;
            }
        }

        /// <summary>
        /// Determines how far rays will be cast to detect the position of the brush on a Paintable object
        /// </summary>
        abstract protected float RayDistance
        {
            get;
        }

        /// <summary>
        /// Returns a ray, depending on the nature of the brush
        /// </summary>
        /// <returns></returns>
        abstract protected Ray GetRay();
    }
}
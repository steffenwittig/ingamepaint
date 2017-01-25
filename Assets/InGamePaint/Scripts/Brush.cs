using UnityEngine;

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

        protected float currentPaintableDistance, timeSinceLastUpdate, updateFrequency = 1f / 60f;
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

            GameObject colorDisplay = GameObject.Find("BrushColor");
            if (colorDisplay != null)
            {
                colorDisplayRenderer = colorDisplay.GetComponent<Renderer>();
            }
            GameObject shapeDisplay = GameObject.Find("BrushShape");
            if (shapeDisplay != null)
            {
                shapeDisplayRenderer = shapeDisplay.GetComponent<Renderer>();
                shapeDisplayInitScale = shapeDisplay.transform.localScale;
            }

            ApplyBrushSettings();
        }

        virtual protected void ApplyBrushSettings()
        {
            UpdateBrushTextureToSize();

            if (colorDisplayRenderer != null)
            {
                Texture2D tex = new Texture2D(1, 1);
                tex.SetPixel(0, 0, color);
                tex.Apply();
                colorDisplayRenderer.material.mainTexture = tex;

            }
            if (shapeDisplayRenderer != null)
            {
                shapeDisplayRenderer.material.mainTexture = brushAlphaOriginal;
                shapeDisplayRenderer.transform.localScale = brushSize * shapeDisplayInitScale / 128;
            }
        }

        protected void UpdateBrushTextureToSize()
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
            // calculate the strength of the smudge effect, depening on the brush's and paintable's settings
            float smudgeFactor = Mathf.Max(smudgeStrength, currentPaintable.minSmudgeStrength);
            smudgeFactor = (smudgeFactor * Mathf.Max(0, currentPaintable.smudgeMultiplier));
            smudgeFactor /= 10;

            if (smudgeFactor > 0)
            {
                // pick previous color from the center quarter of the brush
                Color previousColor = currentPaintable.PickColor(coords, Mathf.RoundToInt(brushSize / 2));
                // apply brush tip to canvas texture
                currentPaintable.PaintTexture(coords, brushTip, color);
                // mix previous color into brush color
                AddColor(previousColor, smudgeFactor, true);
            } else
            {
                currentPaintable.PaintTexture(coords, brushTip, color);
            }

            // reduce opacity
            BrushOpacity -= opacityFade/250;
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

        protected void ClickClickable()
        {
            switch (currentClickable.GetType().ToString())
            {
                case "InGamePaint.BrushPreset": ApplyPreset((BrushPreset)currentClickable); break;
                case "InGamePaint.ColorPreset": AddColor(((ColorPreset)currentClickable).color, 1f, false); break;
            }
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

        abstract protected void UpdateBrush();

        protected void Update()
        {
            timeSinceLastUpdate += Time.deltaTime;

            if (timeSinceLastUpdate > updateFrequency)
            {
                UpdateBrush();
                timeSinceLastUpdate = 0;
            }
        }
    }
}
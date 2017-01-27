using UnityEngine;

namespace InGamePaint
{
    /// <summary>
    /// Abstract class to paint brush tip textures in a specified color on a Paintable's texture
    /// </summary>
    public abstract class Brush : MonoBehaviour
    {

        /// <summary>
        /// Alpha texture (shape) of the brush
        /// </summary>
        public Texture2D brushTip;

        /// <summary>
        /// Specifies how much the alpha color of the brush should be reduced while painting
        /// </summary>
        public float opacityFade = 0f;

        /// <summary>
        /// Specified how much of the Paintable color should be added to the brush color while painting
        /// </summary>
        public float smudgeStrength = 0f;

        /// <summary>
        /// Specifies how many brush tips should be placed on a line drawn between two frames
        /// </summary>
        public float brushSpacing = 0.5f;

        /// <summary>
        /// Specifies the size of the brush in pixels
        /// </summary>
        public int brushSize;

        /// <summary>
        /// Distance of the brush's origin to the paintable, that the brush currently points at
        /// </summary>
        protected float currentPaintableDistance;

        /// <summary>
        /// Time passed since UpdateBrush() was called
        /// </summary>
        protected float timeSinceLastUpdate;

        /// <summary>
        /// Time between calls to UpdateBrush()
        /// </summary>
        protected float updateFrequency = 1f / 90f;

        /// <summary>
        /// RGBA color of the brush
        /// </summary>
        protected Color color = Color.black;

        /// <summary>
        /// Original alpha texture before scaling it to the brush's size
        /// </summary>
        protected Texture2D brushAlphaOriginal;

        /// <summary>
        /// Paintable at which the brush currently points at
        /// </summary>
        protected Paintable currentPaintable;

        /// <summary>
        /// Paintable at which the brush pointed last frame
        /// </summary>
        protected Paintable lastPaintable;

        /// <summary>
        /// Clickable at which the brush currently points at
        /// </summary>
        protected Clickable currentClickable;

        /// <summary>
        /// Coordinates of the Paintable's texture the brush currently points at
        /// </summary>
        protected Vector2 currentPaintableCoords;

        /// <summary>
        /// Coordinates of the Paintable's texture the brush painted on last frame
        /// </summary>
        protected Vector2 lastPaintableCoords;

        /// <summary>
        /// Initial object scale of the shape display object
        /// </summary>
        protected Vector3 shapeDisplayInitScale;

        /// <summary>
        /// Renderer of the object named "BrushColor" which will be used to display the current color of the brush
        /// </summary>
        protected Renderer colorDisplayRenderer;

        /// <summary>
        /// Renderer for the objects named "BrushShape" which will be used to display the current texture of the brush and will be scaled according to the brush's size
        /// </summary>
        protected Renderer shapeDisplayRenderer;

        /// <summary>
        /// Did the brush paint last frame?
        /// </summary>
        protected bool paintedLastFrame = false;

        /// <summary>
        /// Initialize internal values and link BrushColor and BrushShape objects for live updates
        /// </summary>
        protected virtual void Start()
        {

            if (brushTip == null)
            {
                brushTip = new Texture2D(2, 2); // 1x1 pixel texture would cause errors in TextureScale
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

        /// <summary>
        /// Update internal values after brush parameters changed
        /// </summary>
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

        /// <summary>
        /// Update brush tip texture to new size
        /// </summary>
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
        /// Detect, if the brush points at a paintable and set internal values (coordinates, distance, etc.)
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
        /// Paints a texture at the current UV coordinates on the current Paintable
        /// </summary>
        protected void PaintTexture()
        {
            PaintTexture(currentPaintableCoords);
        }

        /// <summary>
        /// Paint a texture at the specified coordinates on the current Paintable
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
                MixColor(previousColor, smudgeFactor, true);
            } else
            {
                currentPaintable.PaintTexture(coords, brushTip, color);
            }

            // reduce opacity
            BrushOpacity -= opacityFade/250;
        }

        /// <summary>
        /// Add mixColor to the current brush's color
        /// </summary>
        /// <param name="mixColor">Color to mix with the brush's color</param>
        /// <param name="mixValue">Specify from 0f to 1f how the colors should be mixed (0f: don't mix at all, 0.5f: equal mix, 1: replace brush's color) </param>
        /// <param name="ignoreOpacity">Don't change the alpha value of the brush's color</param>
        protected void MixColor(Color mixColor, float mixValue, bool ignoreOpacity)
        {
            float alpha = color.a;
            color = Color.Lerp(color, mixColor, Mathf.Min(1,Mathf.Max(0, mixValue)));
            if (ignoreOpacity)
            {
                color.a = alpha;
            }
            ApplyBrushSettings();
        }

        /// <summary>
        /// Apply values of the given preset
        /// </summary>
        /// <param name="preset">The brush preset</param>
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
        /// Execute clickable logic
        /// </summary>
        protected void ClickClickable()
        {
            switch (currentClickable.GetType().ToString())
            {
                case "InGamePaint.BrushPreset": ApplyPreset((BrushPreset)currentClickable); break;
                case "InGamePaint.ColorPreset": MixColor(((ColorPreset)currentClickable).color, 1f, false); break;
            }
        }

        /// <summary>
        /// Get or set the color that the brush will apply on paintables
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
        /// Get or set the opacity of the brush's color
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
        /// Get or set the size of the brush in pixels
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
        /// Get the size of the brush in pixels; Can be overridden in child classes
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
        /// Returns a ray to detect where the brush points at on a paintable
        /// </summary>
        /// <returns>A ray to use for collision detection</returns>
        abstract protected Ray GetRay();

        /// <summary>
        /// React to input
        /// </summary>
        abstract protected void HandleInput();

        /// <summary>
        /// Runs logic every frame (restricted by updateFrequency)
        /// </summary>
        protected void Update()
        {
            timeSinceLastUpdate += Time.deltaTime;

            if (timeSinceLastUpdate > updateFrequency)
            {
                UpdatePaintableCoords();
                HandleInput();
                timeSinceLastUpdate = 0;
            }
        }
    }
}
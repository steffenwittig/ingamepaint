using UnityEngine;
using System.Collections;

namespace InGamePaint
{
    /// <summary>
    /// Clickable brush preset
    /// </summary>
    public class BrushPreset : Clickable
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
        /// Sets brush tip as color of the mainTexture (brush shape preview)
        /// </summary>
        void Start()
        {
            GetComponent<Renderer>().material.mainTexture = brushTip;
        }

    }
}
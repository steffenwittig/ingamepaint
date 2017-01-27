using UnityEngine;
using System.Collections;

namespace InGamePaint {

    /// <summary>
    /// Clickable color preset
    /// </summary>
    public class ColorPreset : Clickable {

        /// <summary>
        /// RGBA color of the brush
        /// </summary>
        public Color color = Color.black;

        /// <summary>
        /// Sets color of the mainTexture (brush color preview)
        /// </summary>
        void Start()
        {
            GetComponent<Renderer>().material.color = color;
        }

    }
}
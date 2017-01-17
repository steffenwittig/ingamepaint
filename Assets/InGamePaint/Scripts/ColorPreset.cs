using UnityEngine;
using System.Collections;

namespace InGamePaint {
    public class ColorPreset : Clickable {

        public Color color;

        void Start()
        {
            GetComponent<Renderer>().material.color = color;
        }

    }
}
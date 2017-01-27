using UnityEngine;

namespace InGamePaint
{
    /// <summary>
    /// Reacts to global (non brush specific) input
    /// </summary>
    public class InGamePaintControl : MonoBehaviour
    {

        /// <summary>
        /// React to input
        /// </summary>
        void Update()
        {
            if (Input.GetKeyDown(KeyCode.S))
            {
                // save all paintables
                foreach(Paintable paintable in GameObject.FindObjectsOfType<Paintable>())
                {
                    if (paintable.HasUnsavedChanges)
                    {
                        Debug.Log("Saved " + paintable.SaveToFile());
                    }
                }

            }
        }
    }
}
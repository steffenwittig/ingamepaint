using UnityEngine;

namespace InGamePaint
{
    public class InGamePaintControl : MonoBehaviour
    {

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
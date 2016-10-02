using UnityEngine;
using VRTK;

namespace InGamePaint
{
    [RequireComponent(typeof(Rigidbody))]

    public class VrMoveable : MonoBehaviour
    {

        protected VRTK_InteractableObject interactObj;
        protected Rigidbody moveableRigidbody;
        protected bool initialIsKinematic = false;

        protected void Start()
        {
            interactObj = gameObject.AddComponent<VRTK_InteractableObject>();
            interactObj.isGrabbable = true;
            moveableRigidbody = GetComponent<Rigidbody>();
            moveableRigidbody.isKinematic = true;
        }

        protected void Update()
        {
            
            if (interactObj != null)
            {
                Debug.Log(interactObj.IsGrabbed());
                if (interactObj.IsGrabbed())
                {
                    // TODO: deactivate meshcollider
                    moveableRigidbody.isKinematic = false;
                }
                else
                {
                    moveableRigidbody.isKinematic = true;
                }
            }

        }
    }
}
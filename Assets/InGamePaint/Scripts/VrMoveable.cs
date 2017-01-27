using UnityEngine;
using VRTK;
using System.Collections.Generic;

namespace InGamePaint
{

    /// <summary>
    /// Allows to grab and move a paintable object
    /// </summary>
    public class VrMoveable : VRTK_InteractableObject
    {

        /// <summary>
        /// Is the rigidbody intially set to be kinematic?
        /// </summary>
        protected bool initialIsKinematic = false;

        /// <summary>
        /// Removed colliders, stored to be added after dropping the Moveable
        /// </summary>
        protected Stack<GameObject> RemovedColliders;

        /// <summary>
        /// Get the rigidbody
        /// </summary>
        protected Rigidbody MoveableRigidbody
        {
            get
            {
                return GetComponent<Rigidbody>();
            }
        }

        /// <summary>
        /// Override VRTK_InteractableObject.Awake() and set isGrabbable to true
        /// </summary>
        protected override void Awake()
        {
            // we won't call base.Awake() because we don't want VRTK_InteractableObject to initialize with a rigidbody
            isGrabbable = true;
            RemovedColliders = new Stack<GameObject>();
        }

        /// <summary>
        /// Handle grabbing interaction
        /// </summary>
        protected override void Update()
        {

            base.Update();

            // We need a non-convex collider to get UV coordinates from a paintable.
            // VRTK_InteractGrab adds a rigidbody, which will cause Unity to make all MeshColliders (even on children) convex.
            // Solution: Remove the rigidbody and mesh-collider when the grip is released
            // and re-add the mesh-colliders (because simply setting MeshCollider.convex to false won't help)

            if (IsGrabbed())
            {
                // VRTK_InteractGrab has added a rigidbody, so we have to remove all MeshCollider now (Unity has displayed an error message, though)
                foreach (MeshCollider meshCollider in GetComponentsInChildren<MeshCollider>())
                {
                    RemovedColliders.Push(meshCollider.gameObject);
                    Destroy(meshCollider);
                }
            }
            else
            {
                if (MoveableRigidbody != null)
                {
                    // grip was released this frame -> remove the rigidbody
                    Destroy(MoveableRigidbody);
                }
                else if (RemovedColliders != null && RemovedColliders.Count > 0)
                {
                    // rigidbody was release last frame -> add colliders to children again
                    foreach (GameObject go in RemovedColliders.ToArray())
                    {
                        MeshCollider newMeshCollider = go.AddComponent<MeshCollider>();
                        // TODO: store and reset correct materials and meshes
                    }
                    RemovedColliders.Clear();
                }
            }
        }
    }
}
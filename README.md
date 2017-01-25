# InGamePaint

This is a simple 3D painting application, focused on VR painting with the HTC Vive Hedset.

The project is currently being developed as a project at Reutlingen University and is in an early state. It can be controlled via mouse input (MouseBrush.cs) or via an HTC Vive controller (VrBrush.cs).

VR-Demonstration:
- [https://youtu.be/uFFpBnFXw7s](https://youtu.be/uFFpBnFXw7s)
- [https://youtu.be/7paKhXA_saQ](https://youtu.be/7paKhXA_saQ)

# Installation

- Clone this repository and open the project folder in Unity
- Install the SteamVR Plugin from the Asset Store: https://www.assetstore.unity3d.com/en/#!/content/32647
- Install the VRTK - SteamVR Unity Toolkit from the Asset Store: https://www.assetstore.unity3d.com/en/#!/content/64131

# Painting with a mouse

Open Assets/InGamePaint/Scenes/PlanceCanvas.unity. The instructions will be displayed on screen once you run the scene.

# Painting with an HTC Vive

- Go to Edit > Project Settings > Player and expand the the "PC, Mac & Linux Standalone" Tab. Look for "Other Settings" and "Virtual Reality SDKs". Move the OpenVR SDK to the top of the list.
- Open Assets/InGamePaint/Scenes/VrPlanceCanvas.unity and run the scene. On the tip of the right controller is a brush. Touch the canvas with it to paint. On the left controller is a palette to mix colors. Paint buckets can be grabbed with the grip buttons of both controllers. Brush and color presets can be applied by touching them with the brush tip.
# InGamePaint

InGamePaint allows you to draw on the textures of 3D objects in a Unity scene **during runtime**.

The project is currently being developed as a project at Reutlingen University and in an early state. Right now it features a simple brush that can be controlled via mouse input (MouseBrush.cs) or via an HTC Vive controller (VrBrush.cs).

Demonstration: [https://youtu.be/uFFpBnFXw7s](https://youtu.be/uFFpBnFXw7s)

# Installation

- Clone this repository and open the resulting folder with Unity
- Install the SteamVR Plugin: https://www.assetstore.unity3d.com/en/#!/content/32647
- Install the VRTK - SteamVR Unity Toolkit: https://www.assetstore.unity3d.com/en/#!/content/64131

# Painting with a mouse

Open Assets/InGamePaint/Examples/PlanceCanvas.unity. The instructions will be displayed on screen once you run the scene.

# Painting with an HTC Vive

- Go to Edit > Project Settings > Player and expand the the "PC, Mac & Linux Standalone" Tab. Look for "Other Settings" and "Virtual Reality SDKs". Move the OpenVR SDK to the top of the list.
- Open Assets/InGamePaint/Examples/VrPlanceCanvas.unity and run the scene. One controller will hold a palette, the other will be your brush (it has a little triangular brush tip at the front). Paint by pulling the trigger and pick colors by clicking on the trackpad.

# Missing features (TODO)

- eraser
- undo
- scale canvas

# Current Problems

- drops frames, when large portions of the textures are updated (texture manipulation is currently running on the main Unity thread, the Unity API cannot be accessed outside the main thread, though...)
- VR: jiggly lines when the tracking of the controller isn't precise enough for a frame or two
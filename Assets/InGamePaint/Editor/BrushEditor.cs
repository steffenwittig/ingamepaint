using UnityEditor;


[CustomEditor(typeof(InGamePaint.Brush))]
[CanEditMultipleObjects]
public class BrushEditor : BrushPresetEditor
{
    SerializedProperty brushSize;

    new protected void OnEnable()
    {
        base.OnEnable();
        brushSize = serializedObject.FindProperty("brushSize");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        EditorGUILayout.IntSlider(brushSize, 1, 512);
        serializedObject.ApplyModifiedProperties();
        base.OnInspectorGUI();
    }
}
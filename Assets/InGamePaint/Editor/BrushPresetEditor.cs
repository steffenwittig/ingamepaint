using UnityEditor;

[CustomEditor(typeof(InGamePaint.BrushPreset))]
[CanEditMultipleObjects]
public class BrushPresetEditor : Editor {

    SerializedProperty opacityFade, smudgeStrength, brushSpacing, brushTip;

    protected void OnEnable()
    {
        opacityFade = serializedObject.FindProperty("opacityFade");
        smudgeStrength = serializedObject.FindProperty("smudgeStrength");
        brushSpacing = serializedObject.FindProperty("brushSpacing");
        brushTip = serializedObject.FindProperty("brushTip");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        EditorGUILayout.Slider(opacityFade, 0f, 10f);
        EditorGUILayout.Slider(smudgeStrength, 0f, 1f);
        EditorGUILayout.Slider(brushSpacing, 0f, 1f);
        EditorGUILayout.PropertyField(brushTip);
        serializedObject.ApplyModifiedProperties();
    }
}
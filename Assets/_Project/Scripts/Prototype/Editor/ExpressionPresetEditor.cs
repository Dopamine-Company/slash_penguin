using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(ExpressionPreset))]
public class ExpressionPresetEditor : Editor
{
    private const string UnityTransformWorldPlacementPrefix = "UnityEditor.TransformWorldPlacementJSON:";

    [System.Serializable]
    private class TransformPlacementJson
    {
        public Vector3 position;
        public Quaternion rotation;
        public Vector3 scale;
    }

    private SerializedProperty clearUnspecifiedSlotsProperty;
    private SerializedProperty spriteStatesProperty;

    private void OnEnable()
    {
        clearUnspecifiedSlotsProperty = serializedObject.FindProperty("clearUnspecifiedSlots");
        spriteStatesProperty = serializedObject.FindProperty("spriteStates");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.PropertyField(clearUnspecifiedSlotsProperty);
        EditorGUILayout.Space(6f);

        DrawSpriteStates();

        serializedObject.ApplyModifiedProperties();
    }

    private void DrawSpriteStates()
    {
        EditorGUILayout.LabelField("Sprite States", EditorStyles.boldLabel);

        if (GUILayout.Button("Add Sprite State"))
        {
            spriteStatesProperty.arraySize++;
        }

        for (int i = 0; i < spriteStatesProperty.arraySize; i++)
        {
            SerializedProperty stateProperty = spriteStatesProperty.GetArrayElementAtIndex(i);

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.BeginHorizontal();
            stateProperty.isExpanded = EditorGUILayout.Foldout(stateProperty.isExpanded, $"State {i}", true);

            if (GUILayout.Button("Remove", GUILayout.Width(70f)))
            {
                spriteStatesProperty.DeleteArrayElementAtIndex(i);
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.EndVertical();
                break;
            }

            EditorGUILayout.EndHorizontal();

            if (stateProperty.isExpanded)
            {
                DrawSpriteState(stateProperty);
            }

            EditorGUILayout.EndVertical();
        }
    }

    private void DrawSpriteState(SerializedProperty stateProperty)
    {
        SerializedProperty slotIdProperty = stateProperty.FindPropertyRelative("slotId");
        SerializedProperty spriteProperty = stateProperty.FindPropertyRelative("sprite");
        SerializedProperty visibleProperty = stateProperty.FindPropertyRelative("visible");
        SerializedProperty copyFromGameObjectProperty = stateProperty.FindPropertyRelative("copyFromGameObject");
        SerializedProperty transformJsonProperty = stateProperty.FindPropertyRelative("transformJson");
        SerializedProperty useWorldTransformProperty = stateProperty.FindPropertyRelative("useWorldTransform");
        SerializedProperty localPositionProperty = stateProperty.FindPropertyRelative("localPosition");
        SerializedProperty localEulerAnglesProperty = stateProperty.FindPropertyRelative("localEulerAngles");
        SerializedProperty localScaleProperty = stateProperty.FindPropertyRelative("localScale");
        SerializedProperty colorProperty = stateProperty.FindPropertyRelative("color");

        EditorGUILayout.PropertyField(slotIdProperty);
        EditorGUILayout.PropertyField(spriteProperty);
        EditorGUILayout.PropertyField(visibleProperty);

        EditorGUILayout.Space(4f);
        EditorGUILayout.PropertyField(copyFromGameObjectProperty, new GUIContent("Copy From GameObject"));

        using (new EditorGUI.DisabledScope(copyFromGameObjectProperty.objectReferenceValue == null))
        {
            if (GUILayout.Button("Copy Local Transform Values"))
            {
                CopyLocalTransformValues(copyFromGameObjectProperty, localPositionProperty, localEulerAnglesProperty, localScaleProperty);
            }
        }

        EditorGUILayout.PropertyField(transformJsonProperty, new GUIContent("Transform JSON"));
        EditorGUILayout.PropertyField(useWorldTransformProperty, new GUIContent("Use World Transform"));

        using (new EditorGUI.DisabledScope(string.IsNullOrWhiteSpace(transformJsonProperty.stringValue)))
        {
            if (GUILayout.Button("Paste JSON Values To Transform"))
            {
                PasteJsonValuesToLocalTransform(
                    transformJsonProperty,
                    useWorldTransformProperty,
                    localPositionProperty,
                    localEulerAnglesProperty,
                    localScaleProperty);
            }
        }

        EditorGUILayout.PropertyField(localPositionProperty);
        EditorGUILayout.PropertyField(localEulerAnglesProperty);
        EditorGUILayout.PropertyField(localScaleProperty);
        EditorGUILayout.PropertyField(colorProperty);
    }

    private static void CopyLocalTransformValues(
        SerializedProperty gameObjectProperty,
        SerializedProperty localPositionProperty,
        SerializedProperty localEulerAnglesProperty,
        SerializedProperty localScaleProperty)
    {
        GameObject sourceObject = gameObjectProperty.objectReferenceValue as GameObject;

        if (sourceObject == null)
        {
            return;
        }

        Transform source = sourceObject.transform;

        localPositionProperty.vector3Value = source.localPosition;
        localEulerAnglesProperty.vector3Value = source.localEulerAngles;
        localScaleProperty.vector3Value = source.localScale;
    }

    private static void PasteJsonValuesToLocalTransform(
        SerializedProperty transformJsonProperty,
        SerializedProperty useWorldTransformProperty,
        SerializedProperty localPositionProperty,
        SerializedProperty localEulerAnglesProperty,
        SerializedProperty localScaleProperty)
    {
        string json = transformJsonProperty.stringValue.Trim();

        if (json.StartsWith(UnityTransformWorldPlacementPrefix))
        {
            json = json.Substring(UnityTransformWorldPlacementPrefix.Length);
        }

        TransformPlacementJson placement;

        try
        {
            placement = JsonUtility.FromJson<TransformPlacementJson>(json);
        }
        catch
        {
            Debug.LogWarning("Failed to parse transform JSON. Paste UnityEditor.TransformWorldPlacementJSON or raw JSON with position/rotation/scale.");
            return;
        }

        localPositionProperty.vector3Value = placement.position;
        localEulerAnglesProperty.vector3Value = placement.rotation.eulerAngles;
        localScaleProperty.vector3Value = placement.scale;
        useWorldTransformProperty.boolValue = true;
    }
}

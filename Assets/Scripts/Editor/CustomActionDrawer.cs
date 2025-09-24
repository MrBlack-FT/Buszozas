using UnityEditor;
using UnityEngine;

[CustomPropertyDrawer(typeof(UIActionConfig))]
public class CustomActionDrawer : PropertyDrawer
{
    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        var actionType = (ActionType)property.FindPropertyRelative("actionType").enumValueIndex;
        int lines = 1; // actionType

        if (actionType == ActionType.FadeIn || actionType == ActionType.FadeOut)
        {
            lines += 3; // target, duration, direction
        }
        else if (actionType == ActionType.InvokeEvent)
        {
            var unityEventProp = property.FindPropertyRelative("unityEvent");
            lines += (int)(EditorGUI.GetPropertyHeight(unityEventProp, true) / EditorGUIUtility.singleLineHeight);
        }

        // Ha nem az első elem, akkor a timing mező is megjelenik
        if (!IsFirstElement(property))
        {
            lines += 1;
        }

        // Egy extra sor az üres hely számára
        lines += 2;

        return EditorGUIUtility.singleLineHeight * lines + 4 + 1;
    }

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        var actionTypeProp = property.FindPropertyRelative("actionType");
        var targetProp = property.FindPropertyRelative("target");
        var durationProp = property.FindPropertyRelative("duration");
        var directionProp = property.FindPropertyRelative("direction");
        var unityEventProp = property.FindPropertyRelative("unityEvent");
        var timingProp = property.FindPropertyRelative("timing");

        var actionType = (ActionType)actionTypeProp.enumValueIndex;

        Rect r = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
        EditorGUI.PropertyField(r, actionTypeProp);

        r.y += EditorGUIUtility.singleLineHeight + 2;

        if (actionType == ActionType.FadeIn || actionType == ActionType.FadeOut)
        {
            EditorGUI.PropertyField(r, targetProp);
            r.y += EditorGUIUtility.singleLineHeight + 2;

            EditorGUI.PropertyField(r, durationProp);
            r.y += EditorGUIUtility.singleLineHeight + 2;

            EditorGUI.PropertyField(r, directionProp);
            r.y += EditorGUIUtility.singleLineHeight + 2;
        }
        else if (actionType == ActionType.InvokeEvent)
        {
            EditorGUI.PropertyField(r, unityEventProp, true);
            r.y += EditorGUI.GetPropertyHeight(unityEventProp, true) + 2;
        }

        //if (!IsFirstElement(property))
        //{
            EditorGUI.PropertyField(r, timingProp);
            r.y += EditorGUIUtility.singleLineHeight + 2;
        //}

        r.y += EditorGUIUtility.singleLineHeight;
    }

    private bool IsFirstElement(SerializedProperty property)
    {
        return property.propertyPath.EndsWith("[0]");
    }
}
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System;
using UnityEditorInternal;

[CustomEditor(typeof(ObjectPlacer))]
public class ObjectPlacerEditor : Editor
{

    ObjectPlacer script;
    ReorderableList staticObjectList;
    ReorderableList pathObjectList;
    int index;

    private void OnEnable()
    {
        staticObjectList = new ReorderableList(serializedObject, serializedObject.FindProperty("levelProps"), true, true, true, true);
        staticObjectList.drawHeaderCallback += DrawStaticsHeader;
        staticObjectList.drawElementCallback += DrawStaticElements;
        staticObjectList.onSelectCallback += OnSelect;
        staticObjectList.onReorderCallback += EndDragResize;
        staticObjectList.elementHeightCallback = RescaleStaticHeight;
        pathObjectList = new ReorderableList(serializedObject, serializedObject.FindProperty("pathObjs"), true, true, true, true);
        pathObjectList.drawHeaderCallback += DrawPathHeader;
        pathObjectList.drawElementCallback += DrawPathElements;
        pathObjectList.elementHeightCallback = RescalePathHeight;
    }

    public override void OnInspectorGUI()
    {
        script = (ObjectPlacer)target;

        serializedObject.Update();
        staticObjectList.DoLayoutList();
        pathObjectList.DoLayoutList();
        serializedObject.ApplyModifiedProperties();
    }
    public void DrawStaticsHeader(Rect rect)
    {
        EditorGUI.LabelField(rect, "Static Objects to Spawn");
    }
    public void DrawPathHeader(Rect rect)
    {
        EditorGUI.LabelField(rect, "Path Objects to Spawn");
    }

    public void DrawStaticElements(Rect rect, int index, bool isActive, bool isFocused)
    {
        SerializedProperty info = staticObjectList.serializedProperty.GetArrayElementAtIndex(index);
        string name = "Static Level Object: ";
        if (script.levelProps[index] != null && script.levelProps[index].gameObject)
        {
            name += script.levelProps[index].gameObject.name;
        }
        else { name += "Null"; }
        DrawSpawnInfo(info, rect, name);
    }
    public void OnSelect(ReorderableList list)
    {
        index = list.index;
    }
    public void EndDragResize(ReorderableList list)
    {
        SerializedProperty current = staticObjectList.serializedProperty.GetArrayElementAtIndex(list.index);
        SerializedProperty grabbed = staticObjectList.serializedProperty.GetArrayElementAtIndex(index);
        if (grabbed.isExpanded)
        {
            current.isExpanded = true;
        }
        grabbed.isExpanded = false;

    }
    public void DrawSpawnInfo(SerializedProperty sp, Rect rect, string name)
    {
        Rect foldout = new Rect(rect.x + 10, rect.y, GUI.skin.label.CalcSize(new GUIContent("Start End Points")).x + 15, EditorGUIUtility.singleLineHeight);
        sp.isExpanded = EditorGUI.Foldout(foldout, sp.isExpanded, name);
        if (sp.isExpanded)
        {
            IterateRectDown(ref rect);
            SerializedProperty go = sp.FindPropertyRelative("gameObject");
            go.objectReferenceValue = EditorGUI.ObjectField(rect, "Prefab", go.objectReferenceValue, typeof(GameObject), true);
            IterateRectDown(ref rect);
            SerializedProperty count = sp.FindPropertyRelative("count");
            count.intValue = EditorGUI.IntField(rect, "Spawn Count", count.intValue);
            IterateRectDown(ref rect);
            SerializedProperty attempts = sp.FindPropertyRelative("attempts");
            attempts.intValue = EditorGUI.IntField(rect, "Spawn Attempts", attempts.intValue);

            IterateRectDown(ref rect);
            EditorGUILayout.BeginHorizontal();
            SerializedProperty type = sp.FindPropertyRelative("scaleType");

            Rect def = rect;
            switch ((ObjectPlacer.ScaleType)type.enumValueIndex)
            {
                case ObjectPlacer.ScaleType.Static:
                    EditorGUIUtility.labelWidth = GUI.skin.label.CalcSize(new GUIContent("Scale Type")).x + 10;

                    rect.Set(def.x, def.y, def.width / 2, def.height);
                    type.enumValueIndex = Convert.ToInt32(EditorGUI.EnumPopup(rect, "Scale Type", (ObjectPlacer.ScaleType)type.enumValueIndex));

                    rect.Set(def.x + def.width / 2, def.y, def.width / 2, def.height);
                    SerializedProperty staticScale = sp.FindPropertyRelative("staticScale");
                    staticScale.floatValue = EditorGUI.FloatField(rect, "Static Scale", staticScale.floatValue);
                    break;
                case ObjectPlacer.ScaleType.ConsistantAxis:
                    EditorGUIUtility.labelWidth = GUI.skin.label.CalcSize(new GUIContent("Scale Type")).x + 10;

                    rect.Set(def.x, def.y, def.width / 3, def.height);
                    type.enumValueIndex = Convert.ToInt32(EditorGUI.EnumPopup(rect, "Scale Type", (ObjectPlacer.ScaleType)type.enumValueIndex));

                    rect.Set(def.x + def.width / 3, def.y, def.width / 3, def.height);
                    SerializedProperty minScale = sp.FindPropertyRelative("minConsistantScale");
                    minScale.floatValue = EditorGUI.FloatField(rect, "Min Scale", minScale.floatValue);

                    rect.Set(def.x + ((2 * def.width) / 3), def.y, def.width / 3, def.height);
                    SerializedProperty maxScale = sp.FindPropertyRelative("maxConsistantScale");
                    maxScale.floatValue = EditorGUI.FloatField(rect, "Max Scale", maxScale.floatValue);
                    break;
                case ObjectPlacer.ScaleType.IndepedentAxis:
                    EditorGUIUtility.labelWidth = GUI.skin.label.CalcSize(new GUIContent("Scale Type")).x + 5;

                    rect.Set(def.x, def.y, def.width / 3, def.height);
                    type.enumValueIndex = Convert.ToInt32(EditorGUI.EnumPopup(rect, "Scale Type", (ObjectPlacer.ScaleType)type.enumValueIndex));

                    EditorGUIUtility.labelWidth = GUI.skin.label.CalcSize(new GUIContent("Min")).x + 5;

                    rect.Set(def.x + def.width / 3, def.y, def.width / 3, def.height);
                    SerializedProperty minVectorScale = sp.FindPropertyRelative("minIndependentScale");
                    minVectorScale.vector3Value = EditorGUI.Vector3Field(rect, "Min", minVectorScale.vector3Value);

                    rect.Set(def.x + ((2 * def.width) / 3), def.y, def.width / 3, def.height);
                    SerializedProperty maxVectorScale = sp.FindPropertyRelative("maxIndependentScale");
                    maxVectorScale.vector3Value = EditorGUI.Vector3Field(rect, "Max", maxVectorScale.vector3Value);
                    break;
            }
            EditorGUIUtility.labelWidth = 0;
            EditorGUILayout.EndHorizontal();
        }
    }
    public void IterateRectDown(ref Rect rect)
    {
        rect.position = new Vector2(rect.position.x, rect.position.y + EditorGUIUtility.singleLineHeight);
        rect.height = EditorGUIUtility.singleLineHeight;
    }

    public void DrawPathElements(Rect rect, int index, bool isActive, bool isFocused)
    {
        SerializedProperty info = pathObjectList.serializedProperty.GetArrayElementAtIndex(index);
        string name = "Path Object: ";
        if (script.pathObjs[index] != null && script.pathObjs[index].gameObject)
        {
            name += script.pathObjs[index].gameObject.name;
        }
        else { name += "Null"; }
        DrawSpawnInfo(info, rect, name);
    }
    public float RescaleStaticHeight(int index)
    {

        SerializedProperty item = staticObjectList.serializedProperty.GetArrayElementAtIndex(index);
        if (item.isExpanded)
        {
            return EditorGUIUtility.singleLineHeight * 5.5f;
        }
        else
        {
            return EditorGUIUtility.singleLineHeight;
        }
    }
    public float RescalePathHeight(int index)
    {
        SerializedProperty item = pathObjectList.serializedProperty.GetArrayElementAtIndex(index);
        if (item.isExpanded)
        {
            return EditorGUIUtility.singleLineHeight * 5.5f;
        }
        else
        {
            return EditorGUIUtility.singleLineHeight;
        }
    }
}

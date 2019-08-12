using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System;
using UnityEditorInternal;

[CustomEditor(typeof(LevelGenerator))]
public class LevelGeneratorEditor : Editor
{

    LevelGenerator script;
    ReorderableList nonTileableList;


    private void OnEnable()
    {
        nonTileableList = new UnityEditorInternal.ReorderableList(serializedObject, serializedObject.FindProperty("prefabs"), true, true, true, true);
        nonTileableList.drawHeaderCallback = rect => { EditorGUI.LabelField(rect, "Spawn Surfaces"); };
        nonTileableList.drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) => { EditorGUI.ObjectField(rect, nonTileableList.serializedProperty.GetArrayElementAtIndex(index), GUIContent.none); };
       

    }
    private void OnDisable()
    {

    }

    public override void OnInspectorGUI()
    {
        script = (LevelGenerator)target;
        if(!script.lineGen || !script.ObjPlacer)
        {
            script.ObjPlacer = Selection.activeGameObject.GetComponent<ObjectPlacer>();
            script.lineGen = Selection.activeGameObject.GetComponent<RandomizedLine>();
        }


        #region Tilability
        EditorGUI.BeginChangeCheck();
        script.tileable = EditorGUILayout.Toggle("Tileable", script.tileable);
        bool value = EditorGUI.EndChangeCheck();

        if (value && script.tileable)
        {
            script.DeparentCustoms();
        }
        else if (value && !script.tileable)
        {
            script.DestroyTiles();
            script.ParentCustoms();
        }
        if (script.tileable)
        {
            script.tile = (GameObject)EditorGUILayout.ObjectField("Tile", script.tile, typeof(GameObject), true);
            script.meshScale = EditorGUILayout.FloatField("Mesh Scale", script.meshScale);
            script.xTiles = EditorGUILayout.IntField("Horizontal Tile Count", script.xTiles);
            script.yTiles = EditorGUILayout.IntField("Vertical Tile Count", script.yTiles);

            serializedObject.ApplyModifiedProperties();
            if (script.tile && (script.spawnPlatforms.Count != (script.xTiles * script.yTiles) || script.prevMeshScale != script.meshScale))
            {
                script.DestroyTiles();
                script.CreateTiles();
            }
        }
        else
        {
            script.includeChildren = EditorGUILayout.Toggle("Include Children", script.includeChildren);

            serializedObject.Update();
            nonTileableList.DoLayoutList();
            serializedObject.ApplyModifiedProperties();


            script.DeparentCustoms();
            script.ParentCustoms();
        }
        EditorGUILayout.Space();
        EditorGUILayout.Space();
        #endregion

        #region Line Generator

        script.lineGen = (RandomizedLine)EditorGUILayout.ObjectField("Line Generator", script.lineGen, typeof(RandomizedLine), true);

        EditorGUI.BeginDisabledGroup(script.lineGen == null || !script.tileable);
        if (GUILayout.Button("Generate Line"))
        {
            if (!new List<string>(UnityEditorInternal.InternalEditorUtility.tags).Contains("Ignorable"))
            {
                Debug.Log("Cannot place path object until the tag \"Ignorable\" as been added to the list of Unity tags.");
            }
            else
            {
                script.GeneratePrimaryLine();
            }
        }
        EditorGUI.EndDisabledGroup();
        if (script.lineGen != null)
        {
            GUILayout.BeginHorizontal();


            EditorGUI.BeginDisabledGroup(script.lineGen.path.primaryPath.segments.Count == 0 || !script.tileable);
            EditorGUIUtility.labelWidth = GUI.skin.label.CalcSize(new GUIContent("Start End Points")).x + 10;
            EditorGUILayout.MinMaxSlider("Start End Points", ref script.lineGen.startSublineVisualizerPoint, ref script.lineGen.endSublineVisualizerPoint, 0, script.lineGen.path.primaryPath.segments.Count - 1);
            script.lineGen.startSublineVisualizerPoint = Mathf.Round(script.lineGen.startSublineVisualizerPoint);
            script.lineGen.endSublineVisualizerPoint = Mathf.Round(script.lineGen.endSublineVisualizerPoint);
            if (script.lineGen.path.primaryPath.segments.Count > 0)
            {
                script.lineGen.subLineVisualizer = new LineSegment(script.lineGen.path.primaryPath.segments[(int)Mathf.Round(script.lineGen.startSublineVisualizerPoint)].start, script.lineGen.path.primaryPath.segments[(int)Mathf.Round(script.lineGen.endSublineVisualizerPoint)].start);
            }
            EditorGUIUtility.labelWidth = 0;

            EditorGUI.EndDisabledGroup();

            EditorGUI.BeginDisabledGroup(script.lineGen.path.primaryPath.segments.Count == 0 || !script.tileable || script.lineGen.subLineVisualizer.start == script.lineGen.subLineVisualizer.end);
            if (GUILayout.Button("Generate Subline"))
            {
                script.GenerateSubLine();
            }
            EditorGUI.EndDisabledGroup();

            GUILayout.EndHorizontal();
        }
        EditorGUILayout.Space();
        EditorGUILayout.Space();
        #endregion

        #region Object Placer
        script.ObjPlacer = (ObjectPlacer)EditorGUILayout.ObjectField("Object Placer", script.ObjPlacer, typeof(ObjectPlacer), true);

        // Debug.Log(script.lineGen.path.primaryPath.points.Count);

        EditorGUI.BeginDisabledGroup(script.ObjPlacer == null);
        if (GUILayout.Button("Place Static Objects"))
        {
            script.SpawnStaticObjects();
        }


        if (script.ObjPlacer != null)
        {
            EditorGUI.BeginDisabledGroup(script.ObjPlacer.path == null);

            GUILayout.BeginHorizontal();

            EditorGUIUtility.labelWidth = GUI.skin.label.CalcSize(new GUIContent("Path Spawn Type")).x + 10;
            script.ObjPlacer.spawnType = (ObjectPlacer.PathSpawnType)EditorGUILayout.EnumPopup("Path Spawn Type", script.ObjPlacer.spawnType);
            EditorGUIUtility.labelWidth = 0;


            if (script.ObjPlacer.spawnType == ObjectPlacer.PathSpawnType.Rectangular)
            {
                EditorGUIUtility.labelWidth = GUI.skin.label.CalcSize(new GUIContent("Height")).x + 10;
                script.ObjPlacer.pathWidth = EditorGUILayout.FloatField("Width", script.ObjPlacer.pathWidth);
                script.ObjPlacer.pathHeight = EditorGUILayout.FloatField("Height", script.ObjPlacer.pathHeight);
                EditorGUIUtility.labelWidth = 0;
            }
            else
            {
                EditorGUIUtility.labelWidth = GUI.skin.label.CalcSize(new GUIContent("Radius")).x + 10;
                script.ObjPlacer.pathRadius = EditorGUILayout.FloatField("Radius", script.ObjPlacer.pathRadius);
                EditorGUIUtility.labelWidth = 0;
            }

            GUILayout.EndHorizontal();
            if (GUILayout.Button("Place Path Objects"))
            {

                script.SpawnPathObjects();

            }
            GUILayout.BeginHorizontal();
            EditorGUIUtility.labelWidth = GUI.skin.label.CalcSize(new GUIContent("End Game Object")).x + 10;
            script.endPrefab = (GameObject)EditorGUILayout.ObjectField("End Game Object", script.endPrefab, typeof(GameObject), true);
            EditorGUIUtility.labelWidth = 0;
            if (GUILayout.Button("Place End Object"))
            {
                if (script.endPrefab)
                {
                    script.SpawnEndObject();
                }
                else
                {
                    Debug.Log("No end object is set, set one before trying to instantiate it.");
                }
            }
            GUILayout.EndHorizontal();
            EditorGUI.EndDisabledGroup();
            #endregion

            EditorGUILayout.Space();
            EditorGUILayout.Space();
            if (GUILayout.Button("Complete Level"))
            {
                script.CompleteLevel();
            }
        }
    }
}

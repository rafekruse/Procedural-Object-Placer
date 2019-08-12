using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[ExecuteInEditMode]
[RequireComponent(typeof(RandomizedLine), typeof(ObjectPlacer))]
public class LevelGenerator : MonoBehaviour
{

    public bool tileable;
    public List<GameObject> spawnPlatforms;
    public Transform level;
    public Transform staticsContainer;
    public Transform pathContainer;
    public Transform collidersContainer;
    public Transform endObject;
    public GameObject endPrefab;

    //tileable
    public GameObject tile;
    public float meshScale;
    public float prevMeshScale;
    public int xTiles;
    public int yTiles;

    //!tileable
    public List<GameObject> prefabs = new List<GameObject>();
    public bool includeChildren;

    public RandomizedLine lineGen;
    public ObjectPlacer ObjPlacer;



    private static string levelName = "Level";
    private static string staticObjectsName = "StaticObjects";
    private static string pathObjectName = "PathObjects";
    private static string colliderObjectName = "PathColliders";

    private void Update()
    {
        if (!level)
        {
            level = new GameObject(levelName).transform;
            Debug.Log("Level Container Created in Hierarchy");
            staticsContainer = new GameObject(staticObjectsName).transform;
            staticsContainer.parent = level;
            Debug.Log("Static Object Container Created in Hierarchy.");
            pathContainer = new GameObject(pathObjectName).transform;
            pathContainer.parent = level;
            Debug.Log("Path Object Container Created in Hierarchy.");
            collidersContainer = new GameObject(colliderObjectName).transform;
            collidersContainer.parent = level;
            Debug.Log("Path Collider Container Created in Hierarchy.");
        }
        else
        {
            staticsContainer.parent = level;
            pathContainer.parent = level;
            collidersContainer.parent = level;
        }

        UpdateColliders();
    }
    public void UpdateColliders()
    {
        CapsuleCollider[] colliders = collidersContainer.GetComponentsInChildren<CapsuleCollider>();
        foreach (CapsuleCollider col in colliders)
        {
            col.radius = lineGen.colliderRadius;
            if (!lineGen.threeDimensional)
            {
                col.transform.position = new Vector3(col.transform.position.x, lineGen.bounds.center.y, col.transform.position.z);
            }
        }
    }
    public void CompleteLevel()
    {
        DestroyImmediate(collidersContainer);
        DestroyImmediate(gameObject);
    }
    public void CreateTiles()
    {
        for (int i = 0; i < xTiles; i++)
        {
            for (int j = 0; j < yTiles; j++)
            {
                tile.transform.localScale = new Vector3(1, 1, 1) * meshScale;
                spawnPlatforms.Add(Instantiate(tile, new Vector3(i * tile.GetComponent<MeshRenderer>().bounds.size.x/* - (i * 0.1f)*/, 0, j * tile.GetComponent<MeshRenderer>().bounds.size.z/* - (j * 0.1f)*/), Quaternion.identity, staticsContainer));
            }
        }
        prevMeshScale = meshScale;
    }
    public void DestroyTiles()
    {
        foreach (GameObject go in spawnPlatforms)
        {
            DestroyImmediate(go);
        }
        spawnPlatforms.Clear();
    }
    public void DeparentCustoms()
    {
        if (spawnPlatforms != null)
        {
            spawnPlatforms.Clear();
        }
        for (int i = 0; i < staticsContainer.GetComponent<Transform>().childCount; i++)
        {
            if (staticsContainer.GetComponent<Transform>().GetChild(i))
            {
                staticsContainer.GetComponent<Transform>().GetChild(i).transform.parent = null;
            }
        }
    }
    public void ParentCustoms()
    {
        foreach (GameObject go in prefabs)
        {
            if (go)
            {
                if (includeChildren)
                {
                    foreach (Transform trans in go.GetComponent<Transform>())
                    {
                        spawnPlatforms.Add(trans.gameObject);
                    }
                    go.transform.parent = staticsContainer;
                }
                else
                {
                    spawnPlatforms.Add(go);
                    go.transform.parent = staticsContainer;
                }
            }
        }
    }
    public void GeneratePrimaryLine()
    {
        ClearLines();
        lineGen.GeneratePrimaryLine(collidersContainer);
        ObjPlacer.path = lineGen.path;
    }
    public void GenerateSubLine()
    {
        lineGen.GenerateSubline(collidersContainer);
    }
    public void ClearLines()
    {
        for (int i = collidersContainer.transform.childCount - 1; i >= 0; i--)
        {
            DestroyImmediate(collidersContainer.transform.GetChild(i).gameObject);
        }
    }
    public void SpawnEndObject()
    {
        if (endObject)
        {
            DestroyImmediate(endObject.gameObject);
        }
        endObject = Instantiate(endPrefab, lineGen.path.primaryPath.segments[lineGen.path.primaryPath.segments.Count - 1].end, Quaternion.identity, level).transform;
    }

    public void SpawnStaticObjects()
    {
        foreach (Transform platform in staticsContainer.transform)
        {
            Transform[] trans = platform.GetComponentsInChildren<Transform>();
            for (int i = trans.Length - 1; i >= 0; i--)
            {
                if (trans[i].tag == "Environmental")
                {
                    DestroyImmediate(trans[i].gameObject);
                }
            }
        }
        if (includeChildren)
        {
            foreach (GameObject go in prefabs)
            {
                foreach (Transform trans in go.GetComponentsInChildren<Transform>())
                {
                    spawnPlatforms.Add(trans.gameObject);
                }
            }
        }
        ObjPlacer.SpawnStaticObjects(spawnPlatforms.ToArray());
    }
    public void SpawnPathObjects()
    {
        ClearPath();
        GeneratePathContainerHeirarchy();
        ObjPlacer.SpawnPathObjects(pathContainer);
    }

    public void GeneratePathContainerHeirarchy()
    {
        GameObject primary = new GameObject("PrimaryPath");
        primary.transform.parent = pathContainer;
        for (int i = 0; i < ObjPlacer.path.primaryPath.segments.Count; i++)
        {
            new GameObject("Primary Path, Segment " + i).transform.parent = primary.transform;
        }
        for (int i = 0; i < ObjPlacer.path.secondaryPaths.Count; i++)
        {
            GameObject secondary = new GameObject("SecondaryPath " + i);
            secondary.transform.parent = pathContainer;
            for (int j = 0; j < ObjPlacer.path.secondaryPaths[i].segments.Count; j++)
            {
                new GameObject("Secondary Path " + i + ", Segment " + j).transform.parent = secondary.transform;
            }
        }
    }
    public void ClearPath()
    {
        for (int i = pathContainer.transform.childCount - 1; i >= 0; i--)
        {
            DestroyImmediate(pathContainer.transform.GetChild(i).gameObject);
        }
    }

}



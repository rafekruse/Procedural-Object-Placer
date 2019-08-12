using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class ObjectPlacer : MonoBehaviour
{
    public enum ScaleType
    {
        Static,
        ConsistantAxis,
        IndepedentAxis
    }

    public enum PathSpawnType { Cylinderical, Rectangular };


    [System.Serializable]
    public class ObjectSpawnInfo
    {
        public GameObject gameObject;
        public int count;
        public int attempts;
        public ScaleType scaleType;
        public float staticScale;
        public float minConsistantScale;
        public float maxConsistantScale;
        public Vector3 minIndependentScale;
        public Vector3 maxIndependentScale;

        ObjectSpawnInfo(GameObject g, int c, int a, ScaleType s, float sS) : this(g, c, a, s)
        {
            staticScale = sS;
        }
        ObjectSpawnInfo(GameObject g, int c, int a, ScaleType s, float minS, float maxS) : this(g, c, a, s)
        {
            minConsistantScale = minS;
            maxConsistantScale = maxS;
        }
        ObjectSpawnInfo(GameObject g, int c, int a, ScaleType s, Vector3 minS, Vector3 maxS) : this(g, c, a, s)
        {
            minIndependentScale = minS;
            maxIndependentScale = maxS;
        }
        ObjectSpawnInfo(GameObject g, int c, int a, ScaleType s)
        {
            gameObject = g;
            count = c;
            attempts = a;
            scaleType = s;
        }
    }

    public List<ObjectSpawnInfo> levelProps;
    public List<ObjectSpawnInfo> pathObjs;

    public GameObject pathContainer;

    private int totalPlaced;
    private int order;
    private int totalRequested;

    public float pathRadius;
    public float pathWidth;
    public float pathHeight;

    public PathSpawnType spawnType;
    public LevelPath path;



    //Update the path part



    #region Spawning Static Level Objects
    public void SpawnStaticObjects(GameObject[] spawnSurfaces)
    {
        Debug.Log("Statics Placement started.");
        totalPlaced = 0;
        totalRequested = 0;
        order = 0;
        levelProps.Sort(SortByVolume);
        StartCoroutine(StartSpawning(spawnSurfaces));
    }
    IEnumerator StartSpawning(GameObject[] spawnSurfaces)
    {
        if(spawnSurfaces.Length <= 0)
        {
            Debug.Log("No surfaces to Spawn on.");
            yield return null;
        }

        for (int i = 0; i < levelProps.Count; i++)
        {
            if (levelProps[i].gameObject)
            {
                if (levelProps[i].gameObject.GetComponent<CheckCollisions>())
                {
                    yield return StartCoroutine(SpawnStaticObject(spawnSurfaces, levelProps[i]));
                    totalRequested += levelProps[i].count;
                }
                else
                {
                    Debug.Log("The gameobject : " + levelProps[i].gameObject.name + " lacks the necessary components to be spawned. Must have CheckCollisions & InitializeColliders components on highest parent object.");
                }
            }
            else
            {
                Debug.Log("There is no current Gameobject for Object Spawn Info at index: " + i);
            }
        }
        Debug.Log("Object placement completed, a total of : " + totalPlaced + " / " + totalRequested + " objects were successfully placed.");
    }
    IEnumerator SpawnStaticObject(GameObject[] spawnSurfaces, ObjectSpawnInfo spawnObject)
    {
        foreach(GameObject go in spawnSurfaces)
        {
            if(go.GetComponent<Collider>())
                go.GetComponent<Collider>().enabled = false;
        }
        List<GameObject> toBePlaced = initialStaticSpawn(spawnSurfaces, spawnObject);
        yield return StartCoroutine(RepositionCollidingStaticObjects(spawnSurfaces, spawnObject, toBePlaced, spawnObject.attempts));

        foreach (GameObject go in spawnSurfaces)
        {
            if (go.GetComponent<Collider>())
                go.GetComponent<Collider>().enabled = true;
        }

        foreach (GameObject go in toBePlaced)
        {
            DestroyImmediate(go);
        }
        totalPlaced += spawnObject.count - toBePlaced.Count;
        Debug.Log((spawnObject.count - toBePlaced.Count) + " / " + spawnObject.count + " " + spawnObject.gameObject.name + " were sucessfully placed, the remainder were destroyed.");
        yield break;
    }
    public List<GameObject> initialStaticSpawn(GameObject[] spawnSurfaces, ObjectSpawnInfo spawnObject)
    {
        List<GameObject> toBePlaced = new List<GameObject>();
        for (int i = 0; i < spawnObject.count; i++)
        {
            if (spawnSurfaces.Length > 0)
            {
                GameObject prop = Instantiate(spawnObject.gameObject, spawnObject.gameObject.transform);
                
                CheckCollisions collisionScript = prop.GetComponent<CheckCollisions>() ? prop.GetComponent<CheckCollisions>() : prop.AddComponent<CheckCollisions>();
                int index = PositionOnMeshRandomizer(spawnSurfaces, collisionScript, spawnObject);
                if (index >= 0)
                {
                    prop.transform.parent = spawnSurfaces[index].transform;

                    collisionScript.order = order;
                    order++;

                    prop.transform.localScale = new Vector3(spawnObject.gameObject.transform.localScale.x / spawnSurfaces[index].transform.localScale.x, spawnObject.gameObject.transform.localScale.y / spawnSurfaces[index].transform.localScale.y, spawnObject.gameObject.transform.localScale.z / spawnSurfaces[index].transform.localScale.z);

                    ScaleRotateProp(prop, spawnObject);
                    

                    prop.name = spawnObject.gameObject.name + " " + i;
                    prop.tag = "Environmental";
                    toBePlaced.Add(prop);
                }
                else DestroyImmediate(prop);
            }
            else
            {
                Debug.Log("There are no current spawn surfaces, must be created before object placement.");
            }
        }
        return toBePlaced;
    }
    public IEnumerator RepositionCollidingStaticObjects(GameObject[] spawnSurfaces, ObjectSpawnInfo info, List<GameObject> toBePlaced, int maxAttempts)
    {
        int currentAttempts = 0;
        while (toBePlaced.Count > 0 && currentAttempts < maxAttempts)
        {
            yield return StartCoroutine(RemoveValid(toBePlaced));

            currentAttempts++;
            for (int i = toBePlaced.Count - 1; i >= 0; i--)
            {
                int index = PositionOnMeshRandomizer(spawnSurfaces, toBePlaced[i].GetComponent<CheckCollisions>(), info);
                if (index >= 0)
                {
                    toBePlaced[i].transform.parent = spawnSurfaces[index].transform;
                    ScaleRotateProp(toBePlaced[i], info);
                }
            }
        }
    }
    private int PositionOnMeshRandomizer(GameObject[] spawnSurfaces, CheckCollisions position, ObjectSpawnInfo info)
    {
        if (spawnSurfaces.Length == 0 )
            return -1;

        int randomMeshIndex = Random.Range(0, spawnSurfaces.Length);
        if (spawnSurfaces[randomMeshIndex].GetComponent<MeshFilter>())
        {
            int[] tris = spawnSurfaces[randomMeshIndex].GetComponent<MeshFilter>().sharedMesh.triangles;
            Vector3[] verts = spawnSurfaces[randomMeshIndex].GetComponent<MeshFilter>().sharedMesh.vertices;

            int randomTris = Random.Range(0, Mathf.RoundToInt(tris.Length / 3f));

            Bounds bounds = spawnSurfaces[randomMeshIndex].GetComponent<MeshFilter>().sharedMesh.bounds;

            Vector3 a = spawnSurfaces[randomMeshIndex].transform.TransformPoint(verts[tris[randomTris * 3]]);
            Vector3 b = spawnSurfaces[randomMeshIndex].transform.TransformPoint(verts[tris[(randomTris * 3) + 1]]);
            Vector3 c = spawnSurfaces[randomMeshIndex].transform.TransformPoint(verts[tris[(randomTris * 3) + 2]]);

            Vector3 side1 = b - a;
            Vector3 side2 = c - a;
            Vector3 normal = Vector3.Cross(side1, side2);

            float randX = Random.value;
            float randY = Random.value;
            float randZ = Random.value;

            Vector3 randPointOnTri = (randX * a + randY * b + randZ * c) / (randX + randY + randZ);
            Quaternion rotation = Quaternion.FromToRotation(Vector3.up, normal.normalized);

            position.SetNewLocation(randPointOnTri, rotation);
            position.transform.RotateAround(position.transform.position, position.transform.up, Random.Range(0, 360));
            return randomMeshIndex;
        }
        else return -1;

        
    }
    #endregion

    #region Spawning Path Objects
    public void SpawnPathObjects(Transform pathContainer)
    {
        Debug.Log("Path Placement started.");
        totalPlaced = 0;
        totalRequested = 0;
        for (int i = 0; i < pathObjs.Count; i++)
        {
            if (pathObjs[i].gameObject)
            {
                if (pathObjs[i].gameObject.GetComponent<CheckCollisions>())
                {
                    StartCoroutine(SpawnPathObject(pathContainer, pathObjs[i]));
                    totalRequested += pathObjs[i].count;
                }
                else
                {
                    Debug.Log("The gameobject : " + pathObjs[i].gameObject.name + " lacks the necessary components to be spawned. Must have CheckCollisions & InitializeColliders components on highest parent object.");
                }
            }
            else
            {
                Debug.Log("There is no current Gameobject for Object Spawn Info at index: " + i);
            }
        }
    }
    IEnumerator SpawnPathObject(Transform pathContainer, ObjectSpawnInfo spawnObject)
    {
        List<GameObject> toBePlaced = initialPathSpawn(pathContainer, spawnObject);

        yield return StartCoroutine(RepositionCollidingPathObjects(toBePlaced, spawnObject.attempts, pathContainer));

        totalPlaced += spawnObject.count - toBePlaced.Count;
        Debug.Log((spawnObject.count - toBePlaced.Count) + " / " + spawnObject.count + " " + spawnObject.gameObject.name + " were sucessfully placed, the remainder were destroyed.");
        foreach (GameObject go in toBePlaced)
        {
            DestroyImmediate(go);
        }
        yield break;
    }
    public List<GameObject> initialPathSpawn(Transform pathContainer, ObjectSpawnInfo spawnObject)
    {
        CheckCollisions template = spawnObject.gameObject.AddComponent<CheckCollisions>();
        List<GameObject> toBePlaced = new List<GameObject>();
        for (int i = 0; i < spawnObject.count; i++)
        {
            int index = spawnType == PathSpawnType.Cylinderical ? PositionInCylinder(template) : PositionInRectangle(template);
            if (index >= 0)
            {
                GameObject prop = Instantiate(template.gameObject, template.transform.position, template.transform.rotation, ContainerFromSegmentNumber(pathContainer, index));
                ScaleRotateProp(prop, spawnObject);

                prop.transform.localScale = new Vector3(prop.transform.localScale.x / prop.transform.parent.localScale.x, prop.transform.localScale.y / prop.transform.parent.localScale.y, prop.transform.localScale.z / prop.transform.parent.localScale.z);

                prop.name = template.gameObject.name + " " + i;
                prop.GetComponent<CheckCollisions>().ignoreEnvironmentCollisions = true;
                toBePlaced.Add(prop);
            }
            else
            {
                Debug.Log("Missing path, you need to have one generated before placing path objects.");
                break;
            }
        }
        return toBePlaced;
    }
    public IEnumerator RepositionCollidingPathObjects(List<GameObject> toBePlaced, int maxAttempts, Transform pathContainer)
    {
        int currentAttempts = 0;
        while (toBePlaced.Count > 0 && currentAttempts < maxAttempts)
        {
            yield return StartCoroutine(RemoveValid(toBePlaced));
            currentAttempts += toBePlaced.Count;
            for (int i = toBePlaced.Count - 1; i >= 0; i--)
            {
                int segmentNum = spawnType == PathSpawnType.Cylinderical ? PositionInCylinder(toBePlaced[i].GetComponent<CheckCollisions>()) : PositionInRectangle(toBePlaced[i].GetComponent<CheckCollisions>());
                toBePlaced[i].transform.parent = ContainerFromSegmentNumber(pathContainer, segmentNum);
            }
        }
    }
    public int PositionInCylinder(CheckCollisions position)
    {
        if (path.primaryPath.segments.Count == 0)
            return -1;

        List<LineSegment> allSegments = AllSegmentsFromPath();
        LineSegment randomSegment = RandomEquallyDistributedSegment(allSegments, path.minlength, path.maxLength);
        int randomIndex = allSegments.IndexOf(randomSegment);

        Vector3 randPointOnTri = RandomPointInCylinder(randomSegment.start, randomSegment.end, pathRadius);

        position.SetNewLocation(randPointOnTri, Quaternion.identity);

        return randomIndex;
    }
    public int PositionInRectangle(CheckCollisions position)
    {
        if (path.primaryPath.segments.Count == 0)
            return -1;

        List<LineSegment> allSegments = AllSegmentsFromPath();
        LineSegment randomSegment = RandomEquallyDistributedSegment(allSegments, path.minlength, path.maxLength);
        int randomIndex = allSegments.IndexOf(randomSegment);

        Vector3 randPointOnTri = RandomPointInRectangularPrism(randomSegment.start, randomSegment.end, pathWidth, pathHeight);

        position.SetNewLocation(randPointOnTri, Quaternion.identity);

        return randomIndex;
    }
    public List<LineSegment> AllSegmentsFromPath()
    {
        List<LineSegment> allSegments = new List<LineSegment>(path.primaryPath.segments);
        for (int i = 0; i < path.secondaryPaths.Count; i++)
        {
            allSegments.AddRange(new List<LineSegment>(path.secondaryPaths[i].segments));
        }
        return allSegments;
    }
    public LineSegment RandomEquallyDistributedSegment(List<LineSegment> lineSegments, float minSegmentLength, float maxSegmentLength)
    {
        LineSegment selected = lineSegments[Random.Range(0, lineSegments.Count)];
        while (selected.dist / maxSegmentLength < Random.Range(0f, 1f))
        {
            selected = lineSegments[Random.Range(0, lineSegments.Count)];
        }
        return selected;
    }
    public Transform ContainerFromSegmentNumber(Transform pathContainer, int segment)
    {
        int currentChildIndex = 0;

        if (path.primaryPath.segments.Count > segment)
        {
            return pathContainer.GetChild(currentChildIndex).GetChild(segment);
        }
        else
        {
            segment -= path.primaryPath.segments.Count;
        }
        while (currentChildIndex < path.secondaryPaths.Count && path.secondaryPaths[currentChildIndex].segments.Count < segment)
        {
            segment -= path.secondaryPaths[currentChildIndex].segments.Count;
            currentChildIndex++;

        }
        currentChildIndex++;
        return pathContainer.GetChild(currentChildIndex).GetChild(segment);
    }



    #endregion
    #region Generic Spawning Methods For Both Types
    IEnumerator RemoveValid(List<GameObject> gos)
    {
        yield return new WaitForFixedUpdate();
        for (int i = gos.Count - 1; i >= 0; i--)
        {
            if (!gos[i].GetComponent<CheckCollisions>().isColliding)
            {
                DestroyImmediate(gos[i].GetComponent<CheckCollisions>());
                DestroyImmediate(gos[i].GetComponent<InitializeColliders>());
                DestroyImmediate(gos[i].GetComponent<Rigidbody>());
                gos.Remove(gos[i]);
            }
        }
        for (int i = 0; i < gos.Count; i++)
        {
            gos[i].GetComponent<CheckCollisions>().isColliding = false;

            gos[i].GetComponent<CheckCollisions>().invalid = false;
        }
      

        
    }
    public void ScaleRotateProp(GameObject obj, ObjectSpawnInfo info)
    {
        obj.transform.localScale = new Vector3(info.gameObject.transform.localScale.x / obj.transform.parent.transform.localScale.x, info.gameObject.transform.localScale.y / obj.transform.parent.transform.localScale.y, info.gameObject.transform.localScale.z / obj.transform.parent.transform.localScale.z);
       

        if (info.scaleType == ScaleType.Static)
        {
            ScaleObject(obj.transform, info.staticScale);
        }
        else if (info.scaleType == ScaleType.ConsistantAxis)
        {
            ScaleObject(obj.transform, info.minConsistantScale, info.maxConsistantScale);
        }
        else
        {
            ScaleObject(obj.transform, info.minIndependentScale, info.maxIndependentScale);
        }
    }
    public void ScaleObject(Transform trans, float staticScale)
    {
        trans.localScale *= staticScale;
    }
    public void ScaleObject(Transform trans, float minScale, float maxScale)
    {
        float rand = Random.Range(minScale, maxScale);
        trans.localScale *= rand;
    }
    public void ScaleObject(Transform trans, Vector3 minScale, Vector3 maxScale)
    {
        Vector3 random = new Vector3(Random.Range(minScale.x, maxScale.x), Random.Range(minScale.y, maxScale.y), Random.Range(minScale.z, maxScale.z));
        trans.localScale = Vector3.Scale(trans.localScale, random);
    }
    #endregion

    static int SortByVolume(ObjectSpawnInfo info1, ObjectSpawnInfo info2)
    {
        Collider[] colliders1 = info1.gameObject.GetComponentsInChildren<Collider>();
        float go1SumVolume = VolumeOfColliders(colliders1);
        go1SumVolume *= Mathf.Pow(Mathf.Max(info1.staticScale, info1.maxConsistantScale, info1.maxIndependentScale.magnitude), 3);

        Collider[] colliders2 = info2.gameObject.GetComponentsInChildren<Collider>();
        float go2SumVolume = VolumeOfColliders(colliders2);
        go2SumVolume *= Mathf.Pow(Mathf.Max(info2.staticScale, info2.maxConsistantScale, info2.maxIndependentScale.magnitude), 3);

        return go2SumVolume.CompareTo(go1SumVolume);
    }
    static float VolumeOfColliders(Collider[] cols)
    {
        float volumeSum = 0;
        foreach (Collider col in cols)
        {
            if (col.GetType() == typeof(BoxCollider))
            {
                GameObject temp = Instantiate(col.gameObject);
                volumeSum += BoxColliderVolume(temp.GetComponent<BoxCollider>());
                DestroyImmediate(temp);
            }
            else if (col.GetType() == typeof(SphereCollider))
            {
                GameObject temp = Instantiate(col.gameObject);
                volumeSum += SphereColliderVolume(temp.GetComponent<SphereCollider>());
                DestroyImmediate(temp);
            }
            else if(col.GetType() == typeof(CapsuleCollider))
            {
                GameObject temp = Instantiate(col.gameObject);
                volumeSum += CapsuleColliderVolume(temp.GetComponent<CapsuleCollider>());
                DestroyImmediate(temp);
            }
            else
            {
                GameObject temp = Instantiate(col.gameObject);
                volumeSum += MeshColliderVolume(temp.GetComponent<MeshCollider>().sharedMesh);
                DestroyImmediate(temp);
            }
        }
        return volumeSum;
    }
    static float BoxColliderVolume(BoxCollider col)
    {
        return col.bounds.size.x * col.bounds.size.y * col.bounds.size.z;
    }
    static float SphereColliderVolume(SphereCollider col)
    {
        return (4 / 3) * Mathf.PI * Mathf.Pow(col.radius, 3);
    }
    static float CapsuleColliderVolume(CapsuleCollider col)
    {
        float cylV = Mathf.PI * Mathf.Pow(col.radius, 2) * (col.height - (2 * col.radius));
        float endsV = (4 / 3) * Mathf.PI * Mathf.Pow(col.radius, 3);
        return cylV + endsV;
    }
    static float SignedVolumeOfTriangle(Vector3 p1, Vector3 p2, Vector3 p3)
    {
        float v321 = p3.x * p2.y * p1.z;
        float v231 = p2.x * p3.y * p1.z;
        float v312 = p3.x * p1.y * p2.z;
        float v132 = p1.x * p3.y * p2.z;
        float v213 = p2.x * p1.y * p3.z;
        float v123 = p1.x * p2.y * p3.z;
        return (1.0f / 6.0f) * (-v321 + v231 + v312 - v132 - v213 + v123);
    }

    static float MeshColliderVolume(Mesh mesh)
    {
        float sum = 0;
        for (int i = 0; i < mesh.vertices.Length - 2; i+=3)
        {
            sum += SignedVolumeOfTriangle(mesh.vertices[i], mesh.vertices[i + 1], mesh.vertices[i + 2]);
        }
        return Mathf.Abs(sum);
    }

    #region Finding random points in volumes, ie Rectangular Prism or Cylinder. Used to generate random point on path.
    public static Vector3 RandomPointInRectangularPrism(Vector3 startCenter, Vector3 endCenter, float width, float height)
    {
        Vector3 perpendicular = RandomRectanglePerpendicular(startCenter, endCenter, width, height);
        return RandomPointAlongLine(startCenter, endCenter) + perpendicular;
    }
    public static Vector3 RandomRectanglePerpendicular(Vector3 start, Vector3 end, float width, float height)
    {
        return RandomRectanglePerpendicular(end - start, width, height);
    }
    public static Vector3 RandomRectanglePerpendicular(Vector3 normal, float width, float height)
    {
        Vector3 second = Vector3.Dot(new Vector3(1, 0, 0), normal) < Vector3.Dot(new Vector3(0, 1, 0), normal) ? new Vector3(1, 0, 0) : new Vector3(0, 1, 0);
        Vector3 u = Vector3.Cross(normal, second);
        Vector3 v = Vector3.Cross(normal, u);
        Vector3 output = (Random.Range(-width / 2, width / 2) * u.normalized) +
                                 (Random.Range(-height / 2, height / 2) * v.normalized);
        return output;
    }
    public static Vector3 RandomPointInCylinder(Vector3 startCenter, Vector3 endCenter, float radius)
    {
        Vector3 perpendicular = RandomCirclePerpendicular(startCenter, endCenter, radius);
        return RandomPointAlongLine(startCenter, endCenter) + perpendicular;
    }
    public static Vector3 RandomCirclePerpendicular(Vector3 start, Vector3 end, float radius)
    {
        return RandomCirclePerpendicular(end - start, radius);
    }
    public static Vector3 RandomCirclePerpendicular(Vector3 normal, float radius)
    {
        Vector3 second = Vector3.Dot(new Vector3(1, 0, 0), normal) < Vector3.Dot(new Vector3(0, 1, 0), normal) ? new Vector3(1, 0, 0) : new Vector3(0, 1, 0);
        Vector3 u = Vector3.Cross(normal, second);
        Vector3 v = Vector3.Cross(normal, u);
        Vector3 output = (Mathf.Cos(Random.Range(0, 2 * Mathf.PI)) * u.normalized) +
                                 (Mathf.Sin(Random.Range(0, 2 * Mathf.PI)) * v.normalized);
        return output.normalized * Random.Range(0, radius);
    }
    public static Vector3 RandomPointAlongLine(Vector3 start, Vector3 end)
    {
        return start + Random.Range(0f, 1f) * (end - start);
    }
    #endregion

}


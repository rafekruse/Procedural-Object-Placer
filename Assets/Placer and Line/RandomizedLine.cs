using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RandomizedLine : MonoBehaviour
{
    public bool threeDimensional;
    public bool drawLineAndBounds;
    public BoundingBox bounds;
    public float lineLengthMin;
    public float lineLengthMax;
    public float segmentLengthMin;
    public float segmentLengthMax;
    public float colliderRadius;
    public float PathVariation;//Max Angle Difference allowed from the end point
    public float TurnSharpness;//Max angle difference from previous line segment
    public float maxGenerationAttempts;
    public float randomizationAttempts;
    private int currentGenAttempts;
    private int currentRandAttempts;

    public LevelPath path;
    [HideInInspector]
    public LineSegment subLineVisualizer;
    [HideInInspector]
    public float startSublineVisualizerPoint;
    [HideInInspector]
    public float endSublineVisualizerPoint;


    private GameObject colliderContainer;
    private string colliderTagName = "Ignorable";

    private void Awake()
    {
        InitializePathVariable();
    }

    private void OnDrawGizmos()
    {
        if (path == null)
        {
            InitializePathVariable();
        }
        if (drawLineAndBounds)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireCube(bounds.center, new Vector3(bounds.xBound * 2, bounds.yBound * 2, bounds.zBound * 2));




            for (int i = 0; i < path.primaryPath.segments.Count; i++)
            {
                Debug.DrawLine(path.primaryPath.segments[i].start, path.primaryPath.segments[i].end, Color.black, Time.deltaTime);
            }
            if (path.primaryPath.segments.Count > 0)
            {
                Debug.DrawLine(subLineVisualizer.start, subLineVisualizer.end, Color.green, Time.deltaTime);

                for (int i = path.secondaryPaths.Count - 1; i >= 0; i--)
                {
                    for (int j = 0; j < path.secondaryPaths[i].segments.Count; j++)
                    {
                        Debug.DrawLine(path.secondaryPaths[i].segments[j].start, path.secondaryPaths[i].segments[j].end, Color.red, Time.deltaTime);
                    }

                }

            }
        }
    }

    public void GenerateSubline(Transform parent)
    {
        List<LineSegment> subSegments = new List<LineSegment>();
        subSegments.Add(subLineVisualizer);

        while (!GenerateSegments(subSegments) && currentGenAttempts < maxGenerationAttempts) { currentGenAttempts++; }

        Line line = new Line(subSegments);
        if (currentGenAttempts >= maxGenerationAttempts)
        {
            Debug.Log("Failed to Create Path with specified settings. Give more attempts, tweak angles or just try again");
            currentGenAttempts = 0;
        }
        else
        {
            CreateLineColliders(subSegments.ToArray(), colliderRadius, "Secondary Path " + path.secondaryPaths.Count, colliderTagName, parent);
            currentGenAttempts = 0;
        }
        path.secondaryPaths.Add(line);
    }
    public void GeneratePrimaryLine(Transform parent)
    {
        ClearPaths();
        startSublineVisualizerPoint = 0;
        endSublineVisualizerPoint = 0;
        PickStartPoints(path.primaryPath.segments, threeDimensional);

        while (!GenerateSegments(path.primaryPath.segments) && currentGenAttempts < maxGenerationAttempts) { currentGenAttempts++; }

        if (currentGenAttempts >= maxGenerationAttempts)
        {
            Debug.Log("Failed to Create Path with specified settings. Give more attempts, tweak angles or just try again");
            currentGenAttempts = 0;
        }
        else
        {
            CreateLineColliders(path.primaryPath.segments.ToArray(), colliderRadius, "Primary Line", colliderTagName, parent);
            currentGenAttempts = 0;
        }
    }
    public void ClearPaths()
    {
        path.primaryPath.segments.Clear();
        foreach(Line line in path.secondaryPaths)
        {
            line.segments.Clear();
        }
    }

    private void PickStartPoints(List<LineSegment> segments, bool threeDimensional)
    {
        if (!threeDimensional)
        {
            bounds.yBound = 0;
        }
        Vector3 start = RandomInArea(bounds);
        Vector3 end = RandomInArea(bounds);
        currentRandAttempts = 0;
        while ((Vector3.Distance(start, end) < lineLengthMin || Vector3.Distance(start, end) > lineLengthMax) && randomizationAttempts > currentRandAttempts)
        {
            start = RandomInArea(bounds);
            end = RandomInArea(bounds);
            currentRandAttempts++;
        }
        segments.Add(new LineSegment(start, end));
    }

    public bool GenerateSegments(List<LineSegment> segments)
    {
        Vector3 previousDirection = segments[0].end - segments[0].start;
        while (segments[segments.Count - 1].dist > segmentLengthMax)
        {
            Vector3 newDir = new Vector3(-1, -1, -1);
            Vector3 dirToEnd = new Vector3(1, 1, 1);
            Vector3 newPoint = Vector3.zero;
            while (Vector3.Angle(newDir, dirToEnd) > PathVariation || !inBounds(bounds, newPoint))
            {
                float horizontalAngle = Mathf.Rad2Deg * Mathf.Atan2(previousDirection.z, previousDirection.x);
                float horizontalComponent = Mathf.Sqrt(Mathf.Pow(previousDirection.x, 2) + Mathf.Pow(previousDirection.z, 2));
                float randomHori = horizontalAngle + Random.Range(-TurnSharpness, TurnSharpness);

                float verticalAngle = Mathf.Rad2Deg * Mathf.Atan2(previousDirection.z, horizontalComponent);
                float randomVert = verticalAngle + Random.Range(-TurnSharpness, TurnSharpness);

                float randSegmentLength = Random.Range(segmentLengthMin, segmentLengthMax);

                Vector3 currentPoint = segments[segments.Count - 1].start;

                float xChange = Mathf.Cos(Mathf.Deg2Rad * randomHori) * randSegmentLength;
                float zChange = Mathf.Sin(Mathf.Deg2Rad * randomHori) * randSegmentLength;
                float yChange = Mathf.Sin(Mathf.Deg2Rad * randomVert) * randSegmentLength;

                if (!threeDimensional)
                {
                    yChange = 0;
                }
                newPoint = new Vector3(currentPoint.x + xChange, currentPoint.y + yChange, currentPoint.z + zChange);

                newDir = newPoint - currentPoint;
                dirToEnd = segments[segments.Count - 1].end - newPoint;

                currentRandAttempts++;
                if (currentRandAttempts > randomizationAttempts)
                {
                    currentRandAttempts = 0;
                    return false;
                }
            }
            LineSegment newLast = new LineSegment(newPoint, segments[segments.Count - 1].end);
            segments[segments.Count - 1].end = newPoint;

            segments.Insert(segments.Count, newLast);
            previousDirection = (segments[segments.Count - 1].end - segments[segments.Count - 1].start).normalized;
            currentRandAttempts++;
            if (currentRandAttempts > randomizationAttempts)
            {
                currentRandAttempts = 0;
                return false;
            }

        }
        return true;
    }

    public static Vector3 RandomInArea(BoundingBox bounds)
    {
        return bounds.center + new Vector3(Random.Range(-bounds.xBound, bounds.xBound), Random.Range(-bounds.yBound, bounds.yBound), Random.Range(-bounds.zBound, bounds.zBound));
    }

    public static bool inBounds(BoundingBox bounds, Vector3 point)
    {
        if (Mathf.Abs(bounds.center.x - point.x) > bounds.xBound || Mathf.Abs(bounds.center.y - point.y) > bounds.yBound || Mathf.Abs(bounds.center.z - point.z) > bounds.zBound)
        {
            return false;
        }
        return true;
    }

    public static GameObject CreateLineColliders(LineSegment[] segments, float radius, string name, string tagName, Transform parent)
    {
        GameObject container = new GameObject(name);
        CapsuleCollider template = new GameObject().AddComponent<CapsuleCollider>();
        for (int i = 0; i < segments.Length; i++)
        {
            Vector3 p1 = segments[i].start;
            Vector3 p2 = segments[i].end;
            Vector3 midPoint = (p1 + p2) / 2;

            template.height = Vector3.Distance(p1, p2) + radius;
            template.radius = radius;

            CapsuleCollider col = Instantiate(template, midPoint, Quaternion.Euler(GetOrietationBetweenPoint(p1, p2)), container.transform);
            col.name = name + ", Segment " + i;
            col.tag = tagName;
        }
        DestroyImmediate(template.gameObject);
        container.transform.parent = parent;
        return container;
    }

    public static Vector3 GetOrietationBetweenPoint(Vector3 p1, Vector3 p2)
    {
        Vector3 previousDirection = (p2 - p1).normalized;
        float horizontalAngle = Mathf.Rad2Deg * Mathf.Atan2(previousDirection.z, previousDirection.x);
        float horizontalComponent = Mathf.Sqrt(Mathf.Pow(previousDirection.x, 2) + Mathf.Pow(previousDirection.z, 2));
        float verticalAngle = Mathf.Rad2Deg * Mathf.Atan2(previousDirection.y, horizontalComponent);


        return new Vector3(0, 180 - horizontalAngle, 90 - verticalAngle);
    }

    public void InitializePathVariable()
    {
        path = new LevelPath();
        path.minlength = segmentLengthMin;
        path.maxLength = segmentLengthMax;
        path.primaryPath = new Line();
        path.secondaryPaths = new List<Line>();
        path.primaryPath.segments = new List<LineSegment>();
    }
}


public class LevelPath
{
    public Line primaryPath { get; set; }
    public List<Line> secondaryPaths { get; set; }
    public float minlength;
    public float maxLength;
}
public class Line
{
    public List<LineSegment> segments { get; set; }

    public Line()
    {
        segments = new List<LineSegment>();
    }
    public Line(List<LineSegment> segs)
    {
        segments = segs;
    }
}
[System.Serializable]
public class LineSegment
{
    [SerializeField]
    public Vector3 start
    {
        get { return m_start; }
        set { m_start = value; m_dist = Vector3.Distance(start, end); }
    }
    [SerializeField]
    private Vector3 m_start;

    [SerializeField]
    public Vector3 end
    {
        get { return m_end; }
        set { m_end = value; m_dist = Vector3.Distance(start, end); }
    }
    [SerializeField]
    private Vector3 m_end;

    [SerializeField]
    public float dist
    {
        get { return m_dist; }
        set { m_dist = value; }
    }
    [SerializeField]
    private float m_dist;

    public LineSegment(Vector3 s, Vector3 e)
    {
        m_start = s;
        m_end = e;
        m_dist = Vector3.Distance(s, e);
    }
}

[System.Serializable]
public class BoundingBox
{
    [SerializeField]
    public Vector3 center
    {
        get { return m_center; }
    }
    [SerializeField]
    private Vector3 m_center;

    [SerializeField]
    public float xBound
    {
        get { return m_xBound; }
        set { m_xBound = value; }
    }
    [SerializeField]
    private float m_xBound;

    [SerializeField]
    public float yBound
    {
        get { return m_yBound; }
        set { m_yBound = value; }
    }
    [SerializeField]
    private float m_yBound;

    [SerializeField]
    public float zBound
    {
        get { return m_zBound; }
        set { m_zBound = value; }
    }
    [SerializeField]
    private float m_zBound;

    public BoundingBox(Vector3 _center, float _xBound, float _yBound, float _zBound)
    {
        m_xBound = _xBound;
        m_yBound = _yBound;
        m_zBound = _zBound;
        m_center = _center;
    }

}

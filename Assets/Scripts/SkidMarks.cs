using UnityEngine;
using System.Collections.Generic;

public class SkidMarks : MonoBehaviour
{
    public static SkidMarks Instance { get; private set; }

    private Mesh skidMesh;
    private Material skidMaterial;
    private List<Vector3> vertices = new List<Vector3>();
    private List<int> triangles = new List<int>();
    private List<Vector2> uvs = new List<Vector2>();
    private List<Color> colors = new List<Color>();

    private const int MAX_MARKS = 2000;
    private const float MARK_WIDTH = 0.22f;

    private Dictionary<int, SkidState> activeSkids = new Dictionary<int, SkidState>();

    private class SkidState
    {
        public Vector3 lastPos;
        public Vector3 lastLeft;
        public Vector3 lastRight;
        public bool hasLast;
        public float intensity;
    }

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        // Create mesh
        GameObject meshObj = new GameObject("SkidMarksMesh");
        meshObj.transform.SetParent(transform);

        MeshFilter mf = meshObj.AddComponent<MeshFilter>();
        MeshRenderer mr = meshObj.AddComponent<MeshRenderer>();

        skidMesh = new Mesh();
        skidMesh.MarkDynamic();
        mf.mesh = skidMesh;

        // Create material
        skidMaterial = new Material(Shader.Find("Sprites/Default"));
        skidMaterial.color = new Color(0.1f, 0.1f, 0.1f, 0.7f);
        mr.material = skidMaterial;
        mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        mr.receiveShadows = false;
    }

    public void AddSkidMark(int wheelId, Vector3 position, Vector3 forward, Vector3 up, float intensity)
    {
        if (intensity < 0.1f)
        {
            // End this skid
            if (activeSkids.ContainsKey(wheelId))
            {
                activeSkids[wheelId].hasLast = false;
            }
            return;
        }

        if (!activeSkids.ContainsKey(wheelId))
        {
            activeSkids[wheelId] = new SkidState();
        }

        SkidState state = activeSkids[wheelId];

        Vector3 right = Vector3.Cross(up, forward).normalized;
        Vector3 left = position - right * MARK_WIDTH * 0.5f;
        Vector3 rightPos = position + right * MARK_WIDTH * 0.5f;

        // Lift slightly off ground to prevent z-fighting
        left.y = position.y + 0.02f;
        rightPos.y = position.y + 0.02f;

        if (state.hasLast)
        {
            float dist = Vector3.Distance(position, state.lastPos);
            if (dist < 0.1f) return; // Too close
            if (dist > 2f)
            {
                // Gap too large, start new segment
                state.hasLast = false;
            }
        }

        if (state.hasLast)
        {
            // Add quad
            int baseIdx = vertices.Count;

            vertices.Add(state.lastLeft);
            vertices.Add(state.lastRight);
            vertices.Add(left);
            vertices.Add(rightPos);

            triangles.Add(baseIdx);
            triangles.Add(baseIdx + 2);
            triangles.Add(baseIdx + 1);
            triangles.Add(baseIdx + 1);
            triangles.Add(baseIdx + 2);
            triangles.Add(baseIdx + 3);

            float u = (vertices.Count / 4f) * 0.1f;
            uvs.Add(new Vector2(0, u - 0.1f));
            uvs.Add(new Vector2(1, u - 0.1f));
            uvs.Add(new Vector2(0, u));
            uvs.Add(new Vector2(1, u));

            Color c = new Color(0.08f, 0.08f, 0.08f, intensity * 0.6f);
            colors.Add(c);
            colors.Add(c);
            colors.Add(c);
            colors.Add(c);

            // Trim old marks
            if (vertices.Count > MAX_MARKS * 4)
            {
                int removeCount = 400;
                vertices.RemoveRange(0, removeCount);
                triangles.RemoveRange(0, removeCount / 4 * 6);
                uvs.RemoveRange(0, removeCount);
                colors.RemoveRange(0, removeCount);

                // Fix triangle indices
                for (int i = 0; i < triangles.Count; i++)
                {
                    triangles[i] -= removeCount;
                }
            }

            UpdateMesh();
        }

        state.lastPos = position;
        state.lastLeft = left;
        state.lastRight = rightPos;
        state.hasLast = true;
        state.intensity = intensity;
    }

    void UpdateMesh()
    {
        skidMesh.Clear();
        if (vertices.Count < 4) return;

        skidMesh.SetVertices(vertices);
        skidMesh.SetTriangles(triangles, 0);
        skidMesh.SetUVs(0, uvs);
        skidMesh.SetColors(colors);
        skidMesh.RecalculateNormals();
        skidMesh.RecalculateBounds();
    }
}

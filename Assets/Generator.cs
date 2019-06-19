using System.Collections.Generic;
using UnityEngine;

public class Generator : MonoBehaviour {

    private Vector4[, , ] vertices;
    private Mesh mesh;

    [Range(0f, 1f)]
    public float decrease = 0.5f;
    [Range(0f, 1f)]
    public float isoLevel = 0.5f;
    public bool smooth = true;
    public bool gizmos = true;
    public float scale = 0.5f;
    public Vector3Int cubes = new Vector3Int(15, 15, 15);
    public Cel c = new Cel();

    private void Awake() {
        mesh = new Mesh();
        mesh.MarkDynamic();
        GetComponent<MeshFilter>().mesh = mesh;
        GetComponent<MeshCollider>().sharedMesh = mesh;
    }

    private void OnEnable() {
        GenerateVertices();
        UpdateMesh();
    }

    private void Update() {

        var m = Input.GetMouseButton(0) ? 1 :
            Input.GetMouseButton(1) ? -1 : 0;

        if (m != 0) {
            var ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            var hit = new RaycastHit();

            if (Physics.Raycast(ray, out hit)) {
                for (var x = 0; x < vertices.GetLength(0); x++)
                    for (var y = 0; y < vertices.GetLength(1); y++)
                        for (var z = 0; z < vertices.GetLength(2); z++) {
                            var us = vertices[x, y, z];
                            var distance = Vector3.Distance(hit.point, us);

                            if (distance > 2f)
                                continue;

                            distance /= distance;
                            distance = Mathf.Min(distance, 0.5f);

                            us.w += m * distance * Time.deltaTime;
                            vertices[x, y, z] = us;
                        }

                GetComponent<MeshCollider>().sharedMesh = mesh;
            }
        }

        // c.simulate_compression(vertices);
        UpdateMesh();
    }

    private void GenerateVertices() {
        vertices = new Vector4[cubes.x + 1, cubes.y + 1, cubes.z + 1];

        for (var x = 0; x < vertices.GetLength(0); x++)
            for (var y = 0; y < vertices.GetLength(1); y++)
                for (var z = 0; z < vertices.GetLength(2); z++) {
                    // var px = Mathf.PerlinNoise(x * Mathf.PI * scale, 0f / 3f);
                    // var py = Mathf.PerlinNoise(y * Mathf.PI * scale, 1f / 3f);
                    // var pz = Mathf.PerlinNoise(z * Mathf.PI * scale, 2f / 3f);

                    // var w = (px + py + pz) / 3f;
                    // var w = 1f;

                    // if (y == cubes.y - 1)
                    //     w = 0f;

                    var v = new Vector4(x, y, z, 1f);

                    // var lng = Mathf.Lerp(-180f, 180f, x / (float)cubes.x) * Mathf.Deg2Rad;
                    // var lat = Mathf.Lerp(-90f, 90f, z / (float)cubes.z) * Mathf.Deg2Rad;
                    // var height = y;

                    // v.x = Mathf.Cos(lat) * Mathf.Cos(lng);
                    // v.z = Mathf.Cos(lat) * Mathf.Sin(lng);
                    // v.y = Mathf.Sin(lat);

                    // v *= height;
                    v.w = y / (float)cubes.y * Mathf.PerlinNoise(x / (float)cubes.x * scale, z / (float)cubes.z * scale);
                    // v.w %= 10f;

                    vertices[x, y, z] = v;
                }

    }

    private void UpdateMesh() {

        var currentIndex = 0;
        var triangles = new List<Vector3>();

        for (var x = 0; x < vertices.GetLength(0) - 1; x++)
            for (var y = 0; y < vertices.GetLength(1) - 1; y++)
                for (var z = 0; z < vertices.GetLength(2) - 1; z++) {
                    var cubeVertices = new [] {
                        vertices[x + 0, y + 0, z + 0],
                        vertices[x + 1, y + 0, z + 0],
                        vertices[x + 1, y + 0, z + 1],
                        vertices[x + 0, y + 0, z + 1],
                        vertices[x + 0, y + 1, z + 0],
                        vertices[x + 1, y + 1, z + 0],
                        vertices[x + 1, y + 1, z + 1],
                        vertices[x + 0, y + 1, z + 1],
                    };

                    var value = 0;

                    for (var i = 0; i < 8; i++)
                        value |= (cubeVertices[i].w > isoLevel ? 1 : 0) << i;

                    for (int i = 0; Tables.triangulation[value, i] != -1; i += 3) {
                        // Get indices of corner points A and B for each of the three edges
                        // of the cube that need to be joined to form the triangle.
                        var a0 = Tables.cornerIndexAFromEdge[Tables.triangulation[value, i]];
                        var b0 = Tables.cornerIndexBFromEdge[Tables.triangulation[value, i]];

                        var a1 = Tables.cornerIndexAFromEdge[Tables.triangulation[value, i + 1]];
                        var b1 = Tables.cornerIndexBFromEdge[Tables.triangulation[value, i + 1]];

                        var a2 = Tables.cornerIndexAFromEdge[Tables.triangulation[value, i + 2]];
                        var b2 = Tables.cornerIndexBFromEdge[Tables.triangulation[value, i + 2]];

                        var index = currentIndex++ * 3;
                        var vertexA = InterpolateVerts(cubeVertices[a0], cubeVertices[b0]);
                        var vertexB = InterpolateVerts(cubeVertices[a1], cubeVertices[b1]);
                        var vertexC = InterpolateVerts(cubeVertices[a2], cubeVertices[b2]);

                        triangles.Add(vertexB);
                        triangles.Add(vertexA);
                        triangles.Add(vertexC);
                    }
                }

        var v = new Vector3[triangles.Count];
        var meshTriangles = new int[triangles.Count];

        for (int i = 0; i < triangles.Count; i++) {
            meshTriangles[i] = i;
            v[i] = triangles[i];
        }

        mesh.Clear();
        mesh.vertices = v;
        mesh.triangles = meshTriangles;
        mesh.RecalculateBounds();
        mesh.RecalculateNormals();
    }

    private Vector3 InterpolateVerts(Vector4 v1, Vector4 v2) {
        if (smooth) {
            float t = (isoLevel - v1.w) / (v2.w - v1.w);
            return v1 + t * (v2 - v1);
        } else {
            return (v1 + v2) / 2f;
        }
    }

    private void OnValidate() {
        if (mesh) {
            GenerateVertices();
            UpdateMesh();
        }
    }

    private void OnDrawGizmos() {
        if (vertices == null || !gizmos)
            return;

        for (var x = 0; x < vertices.GetLength(0); x++)
            for (var y = 0; y < vertices.GetLength(1); y++)
                for (var z = 0; z < vertices.GetLength(2); z++) {
                    var v = vertices[x, y, z];
                    Gizmos.color = Color.Lerp(Color.black, Color.white, v.w);

                    if (v.w > isoLevel)
                        Gizmos.DrawSphere(v, 0.05f);
                    else
                        Gizmos.DrawCube(v, Vector3.one * 2f * 0.05f);
                }
    }

}
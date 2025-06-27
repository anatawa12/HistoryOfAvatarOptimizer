using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

#if UNITY_EDITOR
using UnityEditor.Build.Reporting;
using TMPro;
using UnityEditor;
using UnityEditor.Build;
#endif

namespace HistoryOfAvatarOptimizer.ReleaseNoteCard
{
    public class GenerateFloor : MonoBehaviour
    {
        public TextAsset textAsset;
        public string beginDate = "2022-12-27";
        public string endDate = "2025-06-27";
        public string epocDate = "2023-06-27";
        public float width = 1;
        public float dayLength = 0.02f;
        public Shader shader;
        public GameObject versionNamePrefab;

#if UNITY_EDITOR
        class Processor : IProcessSceneWithReport
        {
            public int callbackOrder => 0;
            public void OnProcessScene(Scene scene, BuildReport report)
            {
                foreach (var generateFloor in scene.GetRootGameObjects().SelectMany(x => x.GetComponentsInChildren<GenerateFloor>()))
                {
                    generateFloor.Generate();
                    Object.DestroyImmediate(generateFloor);
                }
            }
        }

        private void Generate()
        {
            var lines = new List<(string name, DateTime begin, DateTime end)>();
            foreach (var line in textAsset.text.Split('\n'))
            {
                if (line.StartsWith('#')) continue;
                var trim = line.Trim();
                var parts = trim.Split(' ');
                if (parts.Length < 3) continue;
                var name = parts[0];
                var begin = DateTime.Parse(parts[1]);
                var end = DateTime.Parse(parts[2]);
                lines.Add((name, begin, end));
            }

            var epoc = DateTime.Parse(epocDate);
            var lastBegin = (float)(DateTime.Parse(beginDate) - epoc).TotalDays * dayLength;
            var lastEnd = lastBegin;

            var vertices = new List<Vector3>();
            var colors = new List<Color>();
            var triangles = new List<int>();

            void AddQuadrangle(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, Color color)
            {
                var vertexBaseIndex = vertices.Count;
                vertices.Add(p0);
                vertices.Add(p1);
                vertices.Add(p2);
                vertices.Add(p3);
                colors.Add(color);
                colors.Add(color);
                colors.Add(color);
                colors.Add(color);
                triangles.Add(vertexBaseIndex + 0);
                triangles.Add(vertexBaseIndex + 1);
                triangles.Add(vertexBaseIndex + 2);
                triangles.Add(vertexBaseIndex + 1);
                triangles.Add(vertexBaseIndex + 3);
                triangles.Add(vertexBaseIndex + 2);
            }

            var colorHueRatio = 1 / ((float)lines.Count + 1);
            var prevVersionName = "0.0.x";

            for (var i = 0; i < lines.Count; i++)
            {
                var color = Color.HSVToRGB(i * colorHueRatio, 1, 1);

                var begin = (float)(lines[i].begin - epoc).TotalDays * dayLength;
                var end = (float)(lines[i].end - epoc).TotalDays * dayLength;

                AddQuadrangle(
                    new Vector3(lastBegin, 0, -width / 2),
                    new Vector3(lastEnd, 0, +width / 2),
                    new Vector3(begin, 0, -width / 2),
                    new Vector3(end, 0, +width / 2),
                    color);

                AddText(prevVersionName, (lastBegin + lastEnd + begin + end) / 4);

                lastBegin = begin;
                lastEnd = end;
                // remove .0 and replace with .x
                prevVersionName = lines[i].name.Substring(0, lines[i].name.Length - 2) + ".x";
            }

            {
                var endDateTime = DateTime.Parse(endDate);
                var end = (float)(endDateTime - epoc).TotalDays * dayLength;
                var color = Color.HSVToRGB(lines.Count * colorHueRatio, 1, 1);

                AddQuadrangle(
                    new Vector3(lastBegin, 0, -width / 2),
                    new Vector3(lastEnd, 0, +width / 2),
                    new Vector3(end, 0, -width / 2),
                    new Vector3(end, 0, +width / 2),
                    color);
            }

            // create mesh
            Mesh mesh = new Mesh
            {
                name = "FloorMesh",
                vertices = vertices.ToArray(),
                colors = colors.ToArray(),
                triangles = triangles.ToArray()
            };

            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            mesh.Optimize();
            mesh.UploadMeshData(markNoLongerReadable:true);

            // add mesh to game object
            var meshFilter = gameObject.AddComponent<MeshFilter>();
            meshFilter.sharedMesh = mesh;
            var meshRenderer = gameObject.AddComponent<MeshRenderer>();
            meshRenderer.sharedMaterial = new Material(shader)
            {
                color = Color.white
            };
        }

        private void AddText(string versionName, float position)
        {
            if (versionNamePrefab == null) return;

            var versionNameObject = Instantiate(versionNamePrefab, transform);
            versionNameObject.transform.localPosition = new Vector3(position, 0.1f, 0);
            versionNameObject.name = versionName;
            EditorUtility.SetDirty(versionNameObject.transform);
            var textMesh = versionNameObject.GetComponentInChildren<TextMeshPro>();
            textMesh.text = versionName;
        }
#endif
    }
}
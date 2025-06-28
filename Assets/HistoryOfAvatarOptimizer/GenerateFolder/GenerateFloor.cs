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
        public TextAsset tagsAsset;
        public string beginDate = "2022-12-27";
        public string endDate = "2025-06-27";
        public string epocDate = "2023-06-27";
        public float width = 1;
        public float dayLength = 0.02f;
        public Shader shader;
        public GameObject versionNamePrefab;
        public GameObject datePrefab;
        public GameObject eventsPrefab;
        public EventInfo[] events;

        [Serializable]
        public struct EventInfo
        {
            [Multiline]
            public string message;
            public string date;
        }

#if UNITY_EDITOR
        class Processor : IProcessSceneWithReport
        {
            public int callbackOrder => 0;
            public void OnProcessScene(Scene scene, BuildReport report)
            {
                foreach (var generateFloor in scene.GetRootGameObjects().SelectMany(x => x.GetComponentsInChildren<GenerateFloor>()))
                {
                    generateFloor.Generate();
                    generateFloor.GenerateDates();
                    generateFloor.GenerateEvents();
                    Object.DestroyImmediate(generateFloor);
                }
            }
        }

        private List<(string name, DateTime begin, DateTime end)> ParseFile()
        {
            var minDates = new Dictionary<string, DateTime>();
            var maxDates = new Dictionary<string, DateTime>();
            var channelNames = new List<string>();

            foreach (var (date, tagName) in GenerateReleaseNoteCardSettings.ParseTagsText(tagsAsset.text))
            {
                // tagName is 0.1.0 etc or 0.1.0-alpha.1 etc
                var releaseChannel = tagName;
                releaseChannel = releaseChannel.Trim();
                if (releaseChannel.Contains('-')) releaseChannel = releaseChannel.Substring(0, releaseChannel.IndexOf('-'));
                releaseChannel = releaseChannel.Substring(0, releaseChannel.LastIndexOf('.'));

                if (!minDates.ContainsKey(releaseChannel) || date < minDates[releaseChannel]) minDates[releaseChannel] = date;
                if (!maxDates.ContainsKey(releaseChannel) || date > maxDates[releaseChannel]) maxDates[releaseChannel] = date;
                if (!channelNames.Contains(releaseChannel)) channelNames.Add(releaseChannel);
            }

            var result = new List<(string name, DateTime begin, DateTime end)>();
            var prevChannelName = channelNames[0];
            foreach (var channelName in channelNames.Skip(1))
            {
                var beginDate = minDates[channelName];
                var endDate = maxDates[prevChannelName];
                if (endDate < beginDate) endDate = beginDate;
                result.Add((channelName + ".x", beginDate, endDate));
                prevChannelName = channelName;
            }
         
            return result;
        }

        private void Generate()
        {
            var lines = ParseFile();

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
                
                AddText(prevVersionName, (lastBegin + lastEnd + end + end) / 4);
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

        private void GenerateDates()
        {
            var epoc = DateTime.Parse(epocDate);
            var beginDateTime = DateTime.Parse(beginDate);
            var endDateTime = DateTime.Parse(endDate);

            // for each month from beginDate to endDate
            var currentDate = beginDateTime;
            currentDate = new DateTime(currentDate.Year, currentDate.Month, 1).AddMonths(1);
            
            while (currentDate <= endDateTime)
            {
                var position = (float)(currentDate - epoc).TotalDays * dayLength;
                var dateName = currentDate.ToString("yyyy/MM/dd");

                var dateObject = Instantiate(datePrefab, transform);
                dateObject.transform.localPosition = new Vector3(position, 0, 0);
                dateObject.name = dateName;
                EditorUtility.SetDirty(dateObject.transform);
                var textMesh = dateObject.GetComponentInChildren<TMP_Text>();
                textMesh.text = $"{dateName}";
                
                currentDate = currentDate.AddMonths(1);
            }
        }

        private void GenerateEvents()
        {
            var epoc = DateTime.Parse(epocDate);
            foreach (var eventInfo in events)
            {
                var dateTime = DateTime.Parse(eventInfo.date);
                var position = (float)(dateTime - epoc).TotalDays * dayLength;

                var eventObject = Instantiate(eventsPrefab, transform);
                eventObject.transform.localPosition = new Vector3(position, 0.005f, 0);
                eventObject.name = eventInfo.message;
                var textMesh = eventObject.GetComponentInChildren<TMP_Text>();
                textMesh.text = $"{eventInfo.message}";
            }
        }

        private void AddText(string versionName, float position)
        {
            if (versionNamePrefab == null) return;

            var versionNameObject = Instantiate(versionNamePrefab, transform);
            versionNameObject.transform.localPosition = new Vector3(position, 0.005f, 0);
            versionNameObject.name = versionName;
            EditorUtility.SetDirty(versionNameObject.transform);
            var textMesh = versionNameObject.GetComponentInChildren<TextMeshPro>();
            textMesh.text = versionName;
        }
#endif
    }
}
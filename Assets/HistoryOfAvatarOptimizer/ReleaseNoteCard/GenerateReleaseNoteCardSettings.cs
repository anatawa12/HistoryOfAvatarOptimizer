using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

#if UNITY_EDITOR
using UnityEditor.Build.Reporting;
using UnityEditor.Build;
#endif

namespace HistoryOfAvatarOptimizer.ReleaseNoteCard
{
    public class GenerateReleaseNoteCardSettings : MonoBehaviour
    {
        public TextAsset releaseNote;
        public TextAsset tagsNote;
        public GameObject cardPrefab;
        public string rootPath = "";

        public string epicDate = "2023-06-27";
        public float lengthPerDay = 0.01f;

        public GameObject textTemplate;
        public GameObject h2Template;
        public GameObject h3Template;
        public GameObject ulTemplate;
        public GameObject liTemplate;

        public const int listLevelsCount = 4 * 2;

        struct VersionInfo
        {
            public GameObject rootObject;
            public TextMeshProUGUI versionField;
            public string name;
        }

        public static List<(DateTime, string)> ParseTagsText(string tags)
        {
            var result = new List<(DateTime, string)>();
            foreach (var line in tags.Split('\n'))
            {
                var tokens = line.Split(new[] { ' ' }, 2);
                if (tokens.Length < 2) continue;
                var date = tokens[0];
                var versionName = tokens[1].Trim();
                // remove leading 'v' if present
                if (versionName.StartsWith("v")) versionName = versionName.Substring(1);
                var dateTime = DateTime.Parse(date);

                result.Add((dateTime, versionName));
            }
            result.Sort((a, b) => a.Item1.CompareTo(b.Item1));

            return result;
        }

        public void Generate()
        {
            var text = releaseNote.text;

            var epocDate = DateTime.Parse(epicDate);

            Transform noteRoot = null;
            var listLevels = new Transform[listLevelsCount];
            var listParentLevels = new Transform[listLevelsCount];

            var versions = new Dictionary<string, VersionInfo>();

            foreach (var line in text.Split('\n'))
            {
                if (line.StartsWith("## [Unreleased")) continue;

                if (line.StartsWith("## ["))
                {
                    // create message that describes no changes since last release if empty
                    if (noteRoot && noteRoot.childCount <= 1)
                    {
                        var message = Instantiate(textTemplate, noteRoot).GetComponentInChildren<TextMeshProUGUI>();
                        message.text = "No changes since last release.";
                    }

                    // ## [release-name] - YYYY-MM-DD
                    var end = line.IndexOf(']');
                    var currentReleaseName = end >= 0 ? line.Substring(4, end - 4) : line;

                    // create card for current release note
                    var card = Instantiate(cardPrefab, transform);
                    card.name = currentReleaseName;
                    card.transform.SetSiblingIndex(0);
                    noteRoot = card.transform.Find(rootPath);
                    var h2 = Instantiate(h2Template, noteRoot).GetComponentInChildren<TextMeshProUGUI>();

                    versions[currentReleaseName] = new VersionInfo
                    {
                        rootObject = card,
                        versionField = h2,
                        name = currentReleaseName
                    };

                    // reset on section start
                    listLevels = new Transform[listLevelsCount];
                    listParentLevels = new Transform[listLevelsCount];
                }
                else if (noteRoot)
                {
                    if (line.StartsWith("### "))
                    {
                        var h3 = Instantiate(h3Template, noteRoot).GetComponentInChildren<TextMeshProUGUI>();
                        h3.text = line.Substring(4);
                        // reset list levels on section start
                        listLevels = new Transform[listLevelsCount];
                        listParentLevels = new Transform[listLevelsCount];
                    }
                    else if (line.TrimStart().StartsWith("- "))
                    {
                        // unordered list item
                        var level = line.IndexOf('-');
                        var currentLevelList = listLevels[level];
                        if (!currentLevelList)
                        {
                            // find parent level
                            var parentList = noteRoot;
                            for (var i = level - 1; i >= 0; i--)
                            {
                                if (listParentLevels[i])
                                {
                                    parentList = listParentLevels[i];
                                    break;
                                }
                            }

                            // create current level list
                            currentLevelList = Instantiate(ulTemplate, parentList).transform;
                            listLevels[level] = currentLevelList;
                        }

                        // clear child levels
                        //Array.Fill(listLevels, null, level + 1, listLevelsCount - (level + 1));
                        for (var i = level + 1; i < listLevels.Length; i++) listLevels[i] = null;

                        // create list item
                        var li = Instantiate(liTemplate, currentLevelList).GetComponentInChildren<TextMeshProUGUI>();
                        li.text = ApplyReplacements(StripMarkdownLinks(line.Substring(level + 1).Trim()));
                        listParentLevels[level] = li.transform.parent;
                    }
                }
            }

            foreach (var (dateTime, versionName) in ParseTagsText(tagsNote.text))
            {
                if (!versions.TryGetValue(versionName, out var versionInfo)) continue;
                var daysSinceEpic = (dateTime - epocDate).TotalDays;

                versionInfo.versionField.text = $"{versionName} - {dateTime:yyyy-MM-dd}";
                versionInfo.rootObject.transform.localPosition = new Vector3(0, 0, (float)(daysSinceEpic * lengthPerDay));

                var height = ((RectTransform)versionInfo.rootObject.GetComponentInChildren<Canvas>()
                    .GetComponent<RectTransform>().GetChild(0)).sizeDelta.y;
                Debug.Log($"height: {height}");
            }
        }

        private string ApplyReplacements(string line)
        {
            line = line.Replace("renamed to `Trace And Optimize`", "renamed to <color=red>`Trace And Optimize`</color>");
            line = line.Replace("only 3 hours", "<color=red>only 3 hours</color>");
            return line;
        }

        private Regex _markdownLinkRegex;

        private string StripMarkdownLinks(string line)
        {
            if (_markdownLinkRegex == null) _markdownLinkRegex = new Regex(@"\[([^\]]+)\]\([^\)]+\)");
            return _markdownLinkRegex.Replace(line, "$1");
        }

#if UNITY_EDITOR
        class SceneProcessor : IProcessSceneWithReport
        {
            public int callbackOrder => 0;

            public void OnProcessScene(Scene scene, BuildReport report)
            {
                foreach (var settings in scene.GetRootGameObjects().SelectMany(x => x.GetComponentsInChildren<GenerateReleaseNoteCardSettings>()))
                {
                    settings.Generate();
                    Object.DestroyImmediate(settings);
                }
            }
        }
#endif
    }
}
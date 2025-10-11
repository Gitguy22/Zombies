using UnityEngine;
using UnityEditor;
using System.IO;
using System.Linq;
using System.Collections.Generic;

public class LineCounterWindow : EditorWindow
{
    private List<string> folderPaths = new List<string> { "Assets/Scripts" };
    private string[] fileExtensions = { ".cs", ".js" };
    private int totalLines = 0;
    private int totalFiles = 0;
    private bool countBlankLines = false;
    private bool countComments = false;
    private Vector2 scrollPosition;

    // Store results per path
    private Dictionary<string, PathResult> pathResults = new Dictionary<string, PathResult>();

    private class PathResult
    {
        public int lines;
        public int files;
    }

    [MenuItem("Tools/Line Counter")]
    public static void ShowWindow()
    {
        GetWindow<LineCounterWindow>("Line Counter");
    }

    void OnGUI()
    {
        GUILayout.Label("Code Line Counter", EditorStyles.boldLabel);
        GUILayout.Space(10);

        // File extensions section
        GUILayout.Label("File Extensions (comma-separated):");
        string extensionsStr = string.Join(",", fileExtensions);
        extensionsStr = EditorGUILayout.TextField(extensionsStr);
        fileExtensions = extensionsStr.Split(',').Select(x => x.Trim()).ToArray();

        GUILayout.Space(10);

        // Options
        countBlankLines = EditorGUILayout.Toggle("Count Blank Lines", countBlankLines);
        countComments = EditorGUILayout.Toggle("Count Comment Lines", countComments);

        GUILayout.Space(10);

        // Folder paths section
        GUILayout.Label("Folder Paths:", EditorStyles.boldLabel);

        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, GUILayout.Height(200));

        for (int i = 0; i < folderPaths.Count; i++)
        {
            EditorGUILayout.BeginHorizontal();

            // Path index label
            GUILayout.Label($"{i + 1}.", GUILayout.Width(20));

            // Path text field
            folderPaths[i] = EditorGUILayout.TextField(folderPaths[i]);

            // Browse button
            if (GUILayout.Button("Browse", GUILayout.Width(60)))
            {
                string path = EditorUtility.OpenFolderPanel("Select Folder", folderPaths[i], "");
                if (!string.IsNullOrEmpty(path))
                {
                    // Convert absolute path to relative path if inside project
                    if (path.StartsWith(Application.dataPath))
                    {
                        folderPaths[i] = "Assets" + path.Substring(Application.dataPath.Length);
                    }
                    else
                    {
                        folderPaths[i] = path;
                    }
                }
            }

            // Remove button
            if (GUILayout.Button("-", GUILayout.Width(25)))
            {
                folderPaths.RemoveAt(i);
                if (pathResults.ContainsKey(folderPaths[i]))
                {
                    pathResults.Remove(folderPaths[i]);
                }
                i--;
            }

            EditorGUILayout.EndHorizontal();

            // Show results for this path if available
            if (pathResults.ContainsKey(folderPaths[i]))
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.LabelField($"Files: {pathResults[folderPaths[i]].files}, Lines: {pathResults[folderPaths[i]].lines}", EditorStyles.miniLabel);
                EditorGUI.indentLevel--;
            }
        }

        EditorGUILayout.EndScrollView();

        // Add new path button
        if (GUILayout.Button("+ Add Path", GUILayout.Height(25)))
        {
            folderPaths.Add("Assets/");
        }

        GUILayout.Space(20);

        // Count button
        if (GUILayout.Button("Count Lines", GUILayout.Height(30)))
        {
            CountLines();
        }

        GUILayout.Space(20);

        // Total results
        if (totalFiles > 0)
        {
            EditorGUILayout.HelpBox($"Total Files: {totalFiles}\nTotal Lines: {totalLines}", MessageType.Info);

            // Detailed results per path
            if (pathResults.Count > 0)
            {
                GUILayout.Label("Results by Path:", EditorStyles.boldLabel);
                foreach (var kvp in pathResults)
                {
                    EditorGUILayout.LabelField($"{kvp.Key}: {kvp.Value.files} files, {kvp.Value.lines} lines");
                }
            }
        }
    }

    void CountLines()
    {
        totalLines = 0;
        totalFiles = 0;
        pathResults.Clear();

        foreach (string folderPath in folderPaths)
        {
            if (string.IsNullOrEmpty(folderPath))
                continue;

            CountLinesInPath(folderPath);
        }

        Debug.Log($"Counted {totalLines} lines in {totalFiles} files across {folderPaths.Count} paths");
    }

    void CountLinesInPath(string folderPath)
    {
        string fullPath;

        // Handle both absolute and relative paths
        if (Path.IsPathRooted(folderPath))
        {
            fullPath = folderPath;
        }
        else
        {
            fullPath = Path.Combine(Application.dataPath, folderPath.Replace("Assets/", ""));
        }

        if (!Directory.Exists(fullPath))
        {
            Debug.LogWarning("Folder not found: " + fullPath);
            return;
        }

        PathResult result = new PathResult { lines = 0, files = 0 };

        foreach (string extension in fileExtensions)
        {
            string searchPattern = "*" + extension;
            string[] files = Directory.GetFiles(fullPath, searchPattern, SearchOption.AllDirectories);

            foreach (string file in files)
            {
                int linesInFile = CountLinesInFile(file);
                result.lines += linesInFile;
                result.files++;
                totalLines += linesInFile;
                totalFiles++;
            }
        }

        pathResults[folderPath] = result;
    }

    int CountLinesInFile(string filePath)
    {
        string[] lines = File.ReadAllLines(filePath);
        int lineCount = 0;

        foreach (string line in lines)
        {
            string trimmedLine = line.Trim();

            // Skip blank lines if not counting them
            if (!countBlankLines && string.IsNullOrWhiteSpace(trimmedLine))
                continue;

            // Skip comment lines if not counting them
            if (!countComments)
            {
                if (trimmedLine.StartsWith("//") ||
                    trimmedLine.StartsWith("/*") ||
                    trimmedLine.StartsWith("*"))
                    continue;
            }

            lineCount++;
        }

        return lineCount;
    }
}
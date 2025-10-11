using UnityEditor;
using UnityEngine;
using System;

namespace Tonic.PostProcessingEditor
{
    public class TonicPostProcessingAbout : EditorWindow
    {
        private const string ToolsMenuItemPath = "Tools/Tonic Post Processing/About";
        private const string AssetVersion = "1.0.0";
        private const string SupportEmail = "Gospodinsime@outlook.com";

        private Vector2 scrollPosition;
        private GUIStyle titleStyle;
        private GUIStyle headerStyle;
        private GUIStyle bodyStyle;
        private GUIStyle checkMarkStyle;
        private GUIStyle warningStyle;
        private bool stylesInitialized = false;

        [MenuItem(ToolsMenuItemPath, false, 2)]
        private static void ShowAboutWindow()
        {
            TonicPostProcessingAbout window = GetWindow<TonicPostProcessingAbout>(true, "Tonic Post Processing - About", true);
            window.minSize = new Vector2(550, 600);
            window.maxSize = new Vector2(550, 600);
            window.Show();
        }

        private void InitializeStyles()
        {
            if (stylesInitialized) return;

            titleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 18,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = EditorGUIUtility.isProSkin ? Color.white : Color.black },
                hover = { textColor = EditorGUIUtility.isProSkin ? Color.white : Color.black }
            };

            headerStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 14,
                fontStyle = FontStyle.Bold,
                normal = { textColor = EditorGUIUtility.isProSkin ? new Color(0.8f, 0.8f, 0.8f) : new Color(0.2f, 0.2f, 0.2f) },
                hover = { textColor = EditorGUIUtility.isProSkin ? new Color(0.8f, 0.8f, 0.8f) : new Color(0.2f, 0.2f, 0.2f) }
            };

            bodyStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 12,
                wordWrap = true,
                normal = { textColor = EditorGUIUtility.isProSkin ? new Color(0.9f, 0.9f, 0.9f) : new Color(0.3f, 0.3f, 0.3f) }
            };

            checkMarkStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 12,
                normal = { textColor = new Color(0.2f, 0.8f, 0.2f) },
                hover = { textColor = new Color(0.2f, 0.8f, 0.2f) }
            };

            warningStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 12,
                normal = { textColor = new Color(0.9f, 0.6f, 0.2f) },
                hover = { textColor = new Color(0.9f, 0.6f, 0.2f) }
            };

            stylesInitialized = true;
        }

        private void OnGUI()
        {
            InitializeStyles();

            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

            GUILayout.Space(5);

            DrawHeader();

            GUILayout.Space(20);

            DrawSystemRequirements();

            GUILayout.Space(20);

            DrawAssetInfo();

            GUILayout.Space(20);

            DrawSupportSection();

            EditorGUILayout.EndScrollView();
        }

        private void DrawHeader()
        {
            GUILayout.BeginVertical("box");

            GUILayout.Label("TONIC POST PROCESSING", titleStyle);
            GUILayout.Space(5);
            GUILayout.Label("High-Quality Image Effects for Built-in Render Pipeline", bodyStyle);
            GUILayout.Label($"Version: {AssetVersion}", bodyStyle);

            GUILayout.EndVertical();
        }

        private void DrawSystemRequirements()
        {
            GUILayout.BeginVertical("box");

            GUILayout.Label("System Requirements Check", headerStyle);
            GUILayout.Space(10);

            bool isUnity6OrAbove = IsUnity6OrAbove();
            DrawStatusLine("Unity 6.0 or above:", isUnity6OrAbove, $"Current: Unity {Application.unityVersion}");

            bool isHDREnabled = IsHDRCameraEnabled();
            DrawStatusLine("HDR Camera enabled:", isHDREnabled, GetHDRStatusText(isHDREnabled));

            bool isLinearSpace = IsLinearColorSpace();
            DrawStatusLine("Linear Color Space:", isLinearSpace, $"Current: {(isLinearSpace ? "Linear" : "Gamma")}");

            GUILayout.EndVertical();
        }

        private void DrawAssetInfo()
        {
            GUILayout.BeginVertical("box");

            GUILayout.Label("Asset Information", headerStyle);
            GUILayout.Space(10);

            GUILayout.Label($"Release Date: {GetReleaseDate()}", bodyStyle);
            GUILayout.Label("Target Pipeline: Built-in Render Pipeline (BiRP)", bodyStyle);
            GUILayout.Label("Compatible with Unity 2022.3+ (Recommended: Unity 6+)", bodyStyle);

            GUILayout.EndVertical();
        }

        private void DrawSupportSection()
        {
            GUILayout.BeginVertical("box");

            GUILayout.Label("Support & Help", headerStyle);
            GUILayout.Space(10);

            GUILayout.Label("Need assistance or found a bug?", bodyStyle);
            GUILayout.Space(5);

            GUILayout.BeginHorizontal();
            GUILayout.Label($"Contact: {SupportEmail}", bodyStyle);
            if (GUILayout.Button("Copy Email", GUILayout.Width(80)))
            {
                EditorGUIUtility.systemCopyBuffer = SupportEmail;
                Debug.Log($"Email copied to clipboard: {SupportEmail}");
            }
            GUILayout.EndHorizontal();

            GUILayout.Space(10);

            GUILayout.EndVertical();

            GUILayout.Space(20);

            GUILayout.BeginVertical("box", GUILayout.Height(80));
            GUILayout.Label("Love Tonic Post Processing?", headerStyle);
            GUILayout.Space(5);
            GUILayout.Label("Please consider leaving a review on the Asset Store!", bodyStyle);
            GUILayout.Label("Your feedback helps us create more amazing tools and updates.", bodyStyle);
            GUILayout.Space(10);
            if (GUILayout.Button("Leave Review"))
            {
                // Add store page!
            }
            GUILayout.EndVertical();
        }

        private void DrawStatusLine(string label, bool isValid, string details)
        {
            GUILayout.BeginHorizontal();

            GUILayout.Label(label, bodyStyle, GUILayout.Width(150));

            if (isValid)
            {
                GUILayout.Label("✓", checkMarkStyle, GUILayout.Width(20));
            }
            else
            {
                GUILayout.Label("⚠", warningStyle, GUILayout.Width(20));
            }

            GUILayout.Label(details, bodyStyle);

            GUILayout.EndHorizontal();
        }

        private bool IsUnity6OrAbove()
        {
            string version = Application.unityVersion;
            string[] versionParts = version.Split('.');

            if (int.TryParse(versionParts[0], out int majorVersion))
            {
                return majorVersion >= 2023;
            }

            return false;
        }

        private bool IsHDRCameraEnabled()
        {
            Camera mainCamera = Camera.main;
            if (mainCamera == null)
            {
                Camera[] cameras = FindObjectsByType<Camera>(FindObjectsSortMode.None);
                if (cameras.Length > 0)
                    mainCamera = cameras[0];
            }

            return mainCamera != null && mainCamera.allowHDR;
        }

        private string GetHDRStatusText(bool isEnabled)
        {
            if (isEnabled)
                return "HDR is enabled on main camera";

            Camera mainCamera = Camera.main;
            if (mainCamera == null)
            {
                Camera[] cameras = FindObjectsByType<Camera>(FindObjectsSortMode.None);
                if (cameras.Length == 0)
                    return "No cameras found in scene";
                return "HDR disabled on cameras";
            }

            return "HDR disabled on main camera";
        }

        private bool IsLinearColorSpace()
        {
            return PlayerSettings.colorSpace == ColorSpace.Linear;
        }

        private string GetReleaseDate()
        {
            return DateTime.Now.ToString("MMMM yyyy");
        }

        private void OnDestroy()
        {
            stylesInitialized = false;
        }
    }
}
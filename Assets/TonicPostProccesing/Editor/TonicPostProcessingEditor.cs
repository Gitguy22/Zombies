#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Linq;
using Tonic.PostProcessing;

namespace Tonic.PostProcessingEditor
{
    [CustomEditor(typeof(TonicPostProcessing))]
    public class TonicPostProcessingEditor : Editor
    {
        // Styles
        private GUIStyle _fallbackTitleStyle;
        private GUIStyle _effectHeaderStyle;
        private GUIStyle _effectSubtitleStyle;
        private GUIStyle _sectionBoxStyle;
        private GUIStyle _activeEffectBoxStyle;
        private GUIStyle _addEffectButtonStyle;
        private GUIStyle _removeEffectButtonStyle;
        private GUIStyle _revertButtonStyle;
        private GUIStyle _toggleButtonStyle;
        private GUIStyle _separatorStyle;

        private TonicPostProcessing _targetScript;
        private Texture2D _titleBannerTexture;
        private bool _stylesInitialized = false;

        // Use EditorPrefs key prefix for persistent foldout states
        private string _prefsKeyPrefix;

        void OnEnable()
        {
            _targetScript = (TonicPostProcessing)target;

            // Create unique key based on object instance ID and scene path
            GameObject go = _targetScript.gameObject;
            string scenePath = go.scene.path;
            string objectPath = GetGameObjectPath(go);
            _prefsKeyPrefix = $"TonicPostProcessingEditor_{scenePath}_{objectPath}_";

            // Load title banner
            string imageGUID = "15aa600a9ffe2d64e86cc4464e6a5680";
            string assetPath = AssetDatabase.GUIDToAssetPath(imageGUID);
            if (!string.IsNullOrEmpty(assetPath))
            {
                _titleBannerTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);
            }
            if (_titleBannerTexture == null)
            {
                Debug.LogWarning("[TonicPostProcessingEditor] Title banner with GUID " + imageGUID + " not found. Using text title.");
            }

            // Initialize foldout states from EditorPrefs
            InitializeFoldoutStates();
        }

        private string GetGameObjectPath(GameObject go)
        {
            string path = go.name;
            while (go.transform.parent != null)
            {
                go = go.transform.parent.gameObject;
                path = go.name + "/" + path;
            }
            return path;
        }

        private void InitializeFoldoutStates()
        {
            if (_targetScript == null) return;

            var allEffects = _targetScript.GetAllEffects();
            foreach (var effect in allEffects)
            {
                // Load foldout state from EditorPrefs, default to true (expanded)
                string key = _prefsKeyPrefix + effect.EffectName + "_foldout";
                bool savedState = EditorPrefs.GetBool(key, true);
                // We don't need to store in a dictionary anymore since we read directly from EditorPrefs
            }
        }

        private bool GetFoldoutState(string effectName)
        {
            string key = _prefsKeyPrefix + effectName + "_foldout";
            return EditorPrefs.GetBool(key, true);
        }

        private void SetFoldoutState(string effectName, bool state)
        {
            string key = _prefsKeyPrefix + effectName + "_foldout";
            EditorPrefs.SetBool(key, state);
        }

        private void InitializeStylesIfNeeded()
        {
            if (_stylesInitialized) return;

            // Main title style
            _fallbackTitleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 18,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                richText = true,
                padding = new RectOffset(0, 0, 8, 12),
                normal = { textColor = EditorGUIUtility.isProSkin ? new Color(0.9f, 0.9f, 0.9f) : new Color(0.1f, 0.1f, 0.1f) },
                hover = { textColor = EditorGUIUtility.isProSkin ? new Color(0.9f, 0.9f, 0.9f) : new Color(0.1f, 0.1f, 0.1f) }
            };

            // Effect header style
            _effectHeaderStyle = new GUIStyle(EditorStyles.foldout)
            {
                fontStyle = FontStyle.Bold,
                fontSize = 13,
                richText = true,
                margin = new RectOffset(4, 4, 2, 2)
            };

            // Effect subtitle style
            _effectSubtitleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 10,
                fontStyle = FontStyle.Italic,
                normal = { textColor = EditorGUIUtility.isProSkin ? new Color(0.7f, 0.7f, 0.7f) : new Color(0.5f, 0.5f, 0.5f) },
                hover = { textColor = EditorGUIUtility.isProSkin ? new Color(0.7f, 0.7f, 0.7f) : new Color(0.5f, 0.5f, 0.5f) }
            };

            // Main section box
            _sectionBoxStyle = new GUIStyle(GUI.skin.box)
            {
                padding = new RectOffset(12, 12, 8, 8),
                margin = new RectOffset(4, 4, 2, 6),
                border = new RectOffset(1, 1, 1, 1)
            };

            // Active effect box with subtle highlight
            _activeEffectBoxStyle = new GUIStyle(GUI.skin.box)
            {
                padding = new RectOffset(12, 12, 10, 10),
                margin = new RectOffset(0, 0, 4, 4),
                border = new RectOffset(2, 2, 2, 2),
                normal = {
                background = CreateColorTexture(EditorGUIUtility.isProSkin ?
                    new Color(0.3f, 0.3f, 0.3f, 0.3f) :
                    new Color(0.85f, 0.85f, 0.85f, 0.5f))
            }
            };

            // Professional add effect button
            _addEffectButtonStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 12,
                fontStyle = FontStyle.Normal,
                padding = new RectOffset(20, 20, 10, 10),
                alignment = TextAnchor.MiddleCenter,
                richText = true,
                fixedHeight = 35
            };

            // Compact remove button
            _removeEffectButtonStyle = new GUIStyle(GUI.skin.button)
            {
                fixedWidth = 24,
                fixedHeight = 24,
                alignment = TextAnchor.MiddleCenter,
                fontSize = 12,
                fontStyle = FontStyle.Bold,
                padding = new RectOffset(0, 0, 0, 0),
                normal = { textColor = EditorGUIUtility.isProSkin ? new Color(0.9f, 0.5f, 0.5f) : new Color(0.8f, 0.2f, 0.2f) }
            };

            // Revert button style
            _revertButtonStyle = new GUIStyle(GUI.skin.button)
            {
                fixedWidth = 24,
                fixedHeight = 24,
                alignment = TextAnchor.MiddleCenter,
                fontSize = 12,
                fontStyle = FontStyle.Bold,
                padding = new RectOffset(0, 0, 0, 0),
                normal = { textColor = EditorGUIUtility.isProSkin ? new Color(0.7f, 0.7f, 0.9f) : new Color(0.4f, 0.4f, 0.8f) }
            };

            // Toggle button style
            _toggleButtonStyle = new GUIStyle(GUI.skin.button)
            {
                fixedWidth = 60,
                fixedHeight = 20,
                fontSize = 10,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };

            _stylesInitialized = true;
        }

        private Texture2D CreateColorTexture(Color color)
        {
            Texture2D texture = new Texture2D(1, 1);
            texture.SetPixel(0, 0, color);
            texture.Apply();
            return texture;
        }

        public override void OnInspectorGUI()
        {
            InitializeStylesIfNeeded();
            serializedObject.Update();

            DrawHeader();
            DrawDefaultSettings();
            DrawActiveEffectsSection();
            DrawAddEffectSection();

            serializedObject.ApplyModifiedProperties();
        }

        private new void DrawHeader()
        {
            EditorGUILayout.Space(8);

            if (_titleBannerTexture != null)
            {
                GUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                float bannerHeight = 60f;
                float bannerWidth = (_titleBannerTexture.width / (float)_titleBannerTexture.height) * bannerHeight;
                GUILayout.Label(_titleBannerTexture, GUILayout.Width(bannerWidth), GUILayout.Height(bannerHeight));
                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();
            }
            else
            {
                GUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                EditorGUILayout.LabelField("<color=#4A9EFF>TONIC</color> POST PROCESSING", _fallbackTitleStyle);
                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();
            }

            EditorGUILayout.Space(12);
        }



        private void DrawDefaultSettings()
        {
            EditorGUILayout.Space(8);

            // Use the default settings box style for professional look
            EditorGUILayout.BeginVertical(_activeEffectBoxStyle);

            // Section header with icon
            GUIContent headerContent = new GUIContent("Default Settings", EditorGUIUtility.IconContent("d_Settings").image);
            EditorGUILayout.LabelField(headerContent, EditorStyles.boldLabel);

            EditorGUILayout.Space(6);

            // Render Scale with custom layout
            SerializedProperty renderScaleProp = serializedObject.FindProperty("renderScale");
            if (renderScaleProp != null)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Render Scale", GUILayout.Width(EditorGUIUtility.labelWidth - 4));
                renderScaleProp.floatValue = EditorGUILayout.Slider(renderScaleProp.floatValue, 0.5f, 1.0f);
                EditorGUILayout.EndHorizontal();

                // Get actual Game View resolution
                Vector2 gameViewSize = GetGameViewSize();
                int gameWidth = Mathf.RoundToInt(gameViewSize.x);
                int gameHeight = Mathf.RoundToInt(gameViewSize.y);
                int scaledWidth = Mathf.RoundToInt(gameWidth * _targetScript.renderScale);
                int scaledHeight = Mathf.RoundToInt(gameHeight * _targetScript.renderScale);

                string resolutionInfo = $"Resolution: {gameWidth}×{gameHeight} → {scaledWidth}×{scaledHeight}";
                float performanceGain = 1.0f / (_targetScript.renderScale * _targetScript.renderScale);
                string performanceInfo = _targetScript.renderScale < 1.0f ?
                    $" ({performanceGain:F1}× performance boost)" :
                    _targetScript.renderScale > 1.0f ?
                    $" ({(1 / performanceGain):F1}× performance cost)" : "";

                EditorGUILayout.LabelField(resolutionInfo + performanceInfo, _effectSubtitleStyle);
            }

            EditorGUILayout.Space(4);

            // Anti-Aliasing Mode
            SerializedProperty antiAliasingProp = serializedObject.FindProperty("antiAliasingMode");
            if (antiAliasingProp != null)
            {
                EditorGUILayout.PropertyField(antiAliasingProp, new GUIContent("Anti Aliasing", "Method for smoothing jagged edges"));

                // Show AA info
                string aaInfo = GetAntiAliasingInfo(_targetScript.antiAliasingMode);
                if (!string.IsNullOrEmpty(aaInfo))
                {
                    EditorGUILayout.LabelField(aaInfo, _effectSubtitleStyle);
                }
            }

            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(12);
        }

        private Vector2 GetGameViewSize()
        {
            System.Type T = System.Type.GetType("UnityEditor.GameView,UnityEditor");
            System.Reflection.MethodInfo GetSizeOfMainGameView = T.GetMethod("GetSizeOfMainGameView", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            System.Object Res = GetSizeOfMainGameView.Invoke(null, null);
            return (Vector2)Res;
        }

        private string GetAntiAliasingInfo(AntiAliasingMode mode)
        {
            switch (mode)
            {
                case AntiAliasingMode.None: return "No anti-aliasing applied";
                case AntiAliasingMode.FXAA: return "Fast Approximate Anti-Aliasing";
                case AntiAliasingMode.NFAA: return "Normal Filter Anti-Aliasing";
                case AntiAliasingMode.DLAA: return "Directional Local Anti-Aliasing";
                case AntiAliasingMode.SSAA: return "Supersampling Anti-Aliasing";
                default: return "";
            }
        }



        private void DrawActiveEffectsSection()
        {
            var activeEffects = _targetScript.GetActiveEffects();
            if (activeEffects.Count == 0) return;

            // Section header with count
            EditorGUILayout.LabelField($"Active Effects ({activeEffects.Count})", EditorStyles.boldLabel);
            EditorGUILayout.Space(4);

            // Sort effects by execution order for display
            var sortedEffects = activeEffects.OrderBy(e => e.ExecutionOrder).ToList();

            // Draw each active effect
            foreach (var effect in sortedEffects)
            {
                DrawProfessionalEffectSection(effect);
            }

            EditorGUILayout.Space(12);
        }

        private void DrawAddEffectSection()
        {
            EditorGUILayout.BeginVertical(_sectionBoxStyle);

            EditorGUILayout.LabelField("Add New Effect", EditorStyles.boldLabel);
            EditorGUILayout.Space(4);

            // Available effects info
            int availableEffects = GetAvailableEffectCount();
            if (availableEffects > 0)
            {
                EditorGUILayout.LabelField($"{availableEffects} effect(s) available to add", _effectSubtitleStyle);
            }
            else
            {
                EditorGUILayout.LabelField("All effects have been added", _effectSubtitleStyle);
            }

            EditorGUILayout.Space(8);

            // Add effect button
            EditorGUI.BeginDisabledGroup(availableEffects == 0);
            GUIContent addEffectButtonContent = new GUIContent("  Add Effect", EditorGUIUtility.IconContent("d_Toolbar Plus").image);
            if (GUILayout.Button(addEffectButtonContent, _addEffectButtonStyle))
            {
                ShowAddEffectMenu();
            }
            EditorGUI.EndDisabledGroup();

            EditorGUILayout.EndVertical();
        }

        private void DrawProfessionalEffectSection(TonicEffectBase effect)
        {
            if (effect == null) return;

            EditorGUILayout.BeginVertical(_activeEffectBoxStyle);

            // Get foldout state from EditorPrefs
            bool foldoutState = GetFoldoutState(effect.EffectName);

            // Header with toggle and buttons
            EditorGUILayout.BeginHorizontal();

            // Effect toggle button (active/inactive)
            bool isActive = effect.active;
            GUIContent toggleContent = new GUIContent(isActive ? "ON" : "OFF");
            Color originalColor = GUI.backgroundColor;
            //GUI.backgroundColor = isActive ? new Color(0.3f, 0.8f, 0.3f) : new Color(0.8f, 0.4f, 0.3f);
            GUI.backgroundColor = isActive ? new Color(0.92f, 0.92f, 0.92f) : new Color(0.3f, 0.3f, 0.3f);

            if (GUILayout.Button(toggleContent, _toggleButtonStyle))
            {
                effect.active = !effect.active;
                EditorUtility.SetDirty(_targetScript);
            }
            GUI.backgroundColor = originalColor;

            EditorGUILayout.Space(8);

            // Effect name and foldout
            EditorGUILayout.BeginVertical();
            EditorGUILayout.BeginHorizontal();

            // Show execution order
            string effectTitle = $"{effect.EffectName}";
            bool newFoldoutState = EditorGUILayout.Foldout(foldoutState, effectTitle, true, _effectHeaderStyle);

            // Save foldout state if it changed
            if (newFoldoutState != foldoutState)
            {
                SetFoldoutState(effect.EffectName, newFoldoutState);
            }
            foldoutState = newFoldoutState;

            GUILayout.FlexibleSpace();

            // Create button contents with fallbacks
            GUIContent revertButtonContent = new GUIContent("↺");
            GUIContent removeButtonContent = new GUIContent("×");

            // Try to get icons, but use text fallback if they fail
            var refreshIcon = EditorGUIUtility.IconContent("d_Refresh");
            if (refreshIcon != null && refreshIcon.image != null)
            {
                revertButtonContent.image = refreshIcon.image;
                revertButtonContent.text = "";
            }

            var trashIcon = EditorGUIUtility.IconContent("d_TreeEditor.Trash");
            if (trashIcon != null && trashIcon.image != null)
            {
                removeButtonContent.image = trashIcon.image;
                removeButtonContent.text = "";
            }

            // Revert button - only show if RevertToDefaults method exists
            if (GUILayout.Button(revertButtonContent, _revertButtonStyle ?? GUI.skin.button, GUILayout.Width(24), GUILayout.Height(24)))
            {
                if (EditorUtility.DisplayDialog("Revert Effect", $"Reset '{effect.EffectName}' to default values?", "Revert", "Cancel"))
                {
                    effect.RevertToDefaults();
                    EditorUtility.SetDirty(_targetScript);
                }
            }

            EditorGUILayout.Space(4);

            // Remove button
            if (GUILayout.Button(removeButtonContent, _removeEffectButtonStyle ?? GUI.skin.button, GUILayout.Width(24), GUILayout.Height(24)))
            {
                if (EditorUtility.DisplayDialog("Remove Effect", $"Remove '{effect.EffectName}' from the effect stack?", "Remove", "Cancel"))
                {
                    effect.enabled = false;
                    EditorUtility.SetDirty(_targetScript);
                    return;
                }
            }

            EditorGUILayout.EndHorizontal();

            // Description
            if (!string.IsNullOrEmpty(effect.Description))
            {
                EditorGUILayout.LabelField(effect.Description, _effectSubtitleStyle);
            }
            EditorGUILayout.EndVertical();

            EditorGUILayout.EndHorizontal();

            // Separator line
            if (foldoutState)
            {
                EditorGUILayout.Space(6);
                Rect lineRect = EditorGUILayout.GetControlRect(false, 1);
                lineRect.xMin += 8;
                lineRect.xMax -= 8;
                EditorGUI.DrawRect(lineRect, EditorGUIUtility.isProSkin ?
                    new Color(0.4f, 0.4f, 0.4f, 0.5f) :
                    new Color(0.6f, 0.6f, 0.6f, 0.3f));
                EditorGUILayout.Space(8);
            }

            // Effect properties
            if (foldoutState)
            {
                EditorGUI.indentLevel++;

                // Disable properties if effect is inactive
                EditorGUI.BeginDisabledGroup(!isActive);

                // Draw effect properties directly
                DrawEffectProperties(effect);

                EditorGUI.EndDisabledGroup();
                EditorGUI.indentLevel--;
                EditorGUILayout.Space(4);
            }

            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(4);
        }

        private void DrawEffectProperties(TonicEffectBase effect)
        {
            // Get the SerializedProperty for this effect
            SerializedProperty effectProperty = GetEffectProperty(effect);

            if (effectProperty != null)
            {
                SerializedProperty propertyIterator = effectProperty.Copy();
                SerializedProperty endProperty = effectProperty.GetEndProperty();

                bool enterChildren = true;
                if (propertyIterator.NextVisible(enterChildren))
                {
                    enterChildren = false;
                    do
                    {
                        if (SerializedProperty.EqualContents(propertyIterator, endProperty)) break;
                        if (!propertyIterator.propertyPath.StartsWith(effectProperty.propertyPath + ".")) break;

                        if (propertyIterator.name == "enabled" || propertyIterator.name == "active") continue;

                        EditorGUILayout.PropertyField(propertyIterator, true);
                    } while (propertyIterator.NextVisible(false));
                }
            }
        }


        // HERE
        private SerializedProperty GetEffectProperty(TonicEffectBase effect)
        {
            // Map effect types to their property names
            if (effect is TonicVignetteEffect)
                return serializedObject.FindProperty("tonicVignette");
            else if (effect is TonicSharpnessEffect)
                return serializedObject.FindProperty("tonicSharpness");
            else if (effect is TonicSSAOEffect)
                return serializedObject.FindProperty("tonicSSAO");
            else if (effect is TonicTonemapper)
                return serializedObject.FindProperty("tonicTonemapper");
            else if (effect is TonicNoiseEffect)
                return serializedObject.FindProperty("tonicNoise");
            else if (effect is TonicMotionBlurEffect)
                return serializedObject.FindProperty("tonicMotionBlur");
            else if (effect is TonicBloomEffect)
                return serializedObject.FindProperty("tonicBloom");
            else if (effect is TonicLensEffects)
                return serializedObject.FindProperty("tonicLensEffects");



            return null;
        }

        private void ShowAddEffectMenu()
        {
            GenericMenu menu = new GenericMenu();

            var allEffects = _targetScript.GetAllEffects();

            foreach (var effect in allEffects)
            {
                if (!effect.enabled)
                {
                    // Determine category based on effect type
                    string category = GetEffectCategory(effect);
                    string menuPath = $"{category}/{effect.EffectName}";

                    menu.AddItem(new GUIContent(menuPath), false, () => AddEffect(effect));
                }
                else
                {
                    string category = GetEffectCategory(effect);
                    string menuPath = $"{category}/{effect.EffectName} (Already Added)";
                    menu.AddDisabledItem(new GUIContent(menuPath));
                }
            }

            menu.ShowAsContext();
        }

        private string GetEffectCategory(TonicEffectBase effect)
        {
            // categories as you create effectsx
            return "Effects";
        }

        private void AddEffect(TonicEffectBase effect)
        {
            effect.enabled = true;
            SetFoldoutState(effect.EffectName, true);
            EditorUtility.SetDirty(_targetScript);
        }

        private int GetAvailableEffectCount()
        {
            var allEffects = _targetScript.GetAllEffects();
            return allEffects.Count(e => !e.enabled);
        }
    }
}
#endif
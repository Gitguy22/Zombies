using UnityEngine;
using UnityEditor;

public class DarkLightmapGenerator : EditorWindow
{
    private Color darkColor = Color.black;
    private bool includeAlpha = false;

    [MenuItem("Tools/Generate Dark Lightmaps")]
    public static void ShowWindow()
    {
        GetWindow<DarkLightmapGenerator>("Dark Lightmap Generator");
    }

    void OnGUI()
    {
        GUILayout.Label("Dark Lightmap Generator", EditorStyles.boldLabel);
        GUILayout.Space(10);

        darkColor = EditorGUILayout.ColorField("Dark Color", darkColor);
        includeAlpha = EditorGUILayout.Toggle("Include Alpha", includeAlpha);

        GUILayout.Space(20);

        if (GUILayout.Button("Generate Dark Lightmaps from Current", GUILayout.Height(30)))
        {
            GenerateDarkLightmaps();
        }

        GUILayout.Space(10);

        EditorGUILayout.HelpBox("This will create dark versions of your current lightmaps. " +
            "The generated textures will be saved in Assets/DarkLightmaps/", MessageType.Info);
    }

    void GenerateDarkLightmaps()
    {
        // Create directory if it doesn't exist
        string path = "Assets/DarkLightmaps";
        if (!AssetDatabase.IsValidFolder(path))
        {
            AssetDatabase.CreateFolder("Assets", "DarkLightmaps");
        }

        LightmapData[] currentLightmaps = LightmapSettings.lightmaps;

        for (int i = 0; i < currentLightmaps.Length; i++)
        {
            if (currentLightmaps[i].lightmapColor != null)
            {
                // Get the original lightmap
                Texture2D originalLightmap = currentLightmaps[i].lightmapColor;

                // Create a new dark texture
                Texture2D darkLightmap = new Texture2D(originalLightmap.width, originalLightmap.height,
                    originalLightmap.format, originalLightmap.mipmapCount > 1);

                // Fill with dark color
                Color[] pixels = new Color[originalLightmap.width * originalLightmap.height];
                for (int p = 0; p < pixels.Length; p++)
                {
                    pixels[p] = includeAlpha ? darkColor : new Color(darkColor.r, darkColor.g, darkColor.b, 1f);
                }

                darkLightmap.SetPixels(pixels);
                darkLightmap.Apply();

                // Save the texture
                byte[] bytes = darkLightmap.EncodeToPNG();
                string filename = $"{path}/DarkLightmap_{i}.png";
                System.IO.File.WriteAllBytes(filename, bytes);

                AssetDatabase.Refresh();

                // Import with correct settings
                TextureImporter importer = AssetImporter.GetAtPath(filename) as TextureImporter;
                if (importer != null)
                {
                    importer.textureType = TextureImporterType.Lightmap;
                    importer.textureCompression = TextureImporterCompression.CompressedHQ;
                    importer.SaveAndReimport();
                }

                DestroyImmediate(darkLightmap);
            }
        }

        Debug.Log($"Generated {currentLightmaps.Length} dark lightmaps in {path}");
        AssetDatabase.Refresh();
    }
}
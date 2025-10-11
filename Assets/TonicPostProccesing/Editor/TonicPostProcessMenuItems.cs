#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using Tonic.PostProcessing;

namespace Tonic.PostProcessingEditor
{
    public class TonicPostProcessMenuItems
    {
        private const string ToolsMenuItemPath = "Tools/Tonic Post Processing/Add Post Processing";

        [MenuItem(ToolsMenuItemPath, false, 1)]
        private static void AddManagerToCameraSmart()
        {
            Camera targetCamera = null;
            GameObject selectedObject = Selection.activeGameObject;

            if (selectedObject != null)
            {
                targetCamera = selectedObject.GetComponent<Camera>();
            }

            if (targetCamera == null && Camera.main != null)
            {
                targetCamera = Camera.main;
                Debug.Log("No camera selected, found and using Camera.main: " + targetCamera.name);
            }

            if (targetCamera == null)
            {
                GameObject mainCameraObjectByTag = GameObject.FindGameObjectWithTag("MainCamera");
                if (mainCameraObjectByTag != null)
                {
                    targetCamera = mainCameraObjectByTag.GetComponent<Camera>();
                    if (targetCamera != null)
                    {
                        Debug.Log("No camera selected and Camera.main not found. Using camera tagged 'MainCamera': " + targetCamera.name);
                    }
                }
            }

            if (targetCamera != null)
            {
                if (targetCamera.gameObject.GetComponent<TonicPostProcessing>() == null)
                {
                    Undo.AddComponent<TonicPostProcessing>(targetCamera.gameObject);
                    Debug.Log("Tonic Post Processing Manager added to camera: " + targetCamera.name);
                    Selection.activeGameObject = targetCamera.gameObject;
                }
                else
                {
                    Debug.LogWarning("Camera '" + targetCamera.name + "' already has a Tonic Post Processing Manager.", targetCamera.gameObject);
                    Selection.activeGameObject = targetCamera.gameObject;
                }
            }
            else
            {
                // 5. If no suitable existing camera found, create a new one
                Debug.Log("No suitable camera found (selected, Camera.main, or tagged 'MainCamera'). Creating a new camera.");
                GameObject newCameraObject = new GameObject("Main Camera with Tonic PP");
                Camera cameraComponent = newCameraObject.AddComponent<Camera>();
                // Basic camera setup
                cameraComponent.clearFlags = CameraClearFlags.Skybox;
                cameraComponent.backgroundColor = new Color(0.192f, 0.301f, 0.474f);
                cameraComponent.nearClipPlane = 0.3f;
                cameraComponent.farClipPlane = 1000f;
                cameraComponent.depth = -1;
                cameraComponent.tag = "MainCamera";

                newCameraObject.AddComponent<TonicPostProcessing>();
                Undo.RegisterCreatedObjectUndo(newCameraObject, "Create Camera with Tonic PP");
                Selection.activeGameObject = newCameraObject;
                Debug.Log("New camera with Tonic Post Processing Manager created, tagged as 'MainCamera', and selected.");
            }
        }


        [MenuItem(ToolsMenuItemPath, true)]
        private static bool ValidateAddManagerToCameraSmart()
        {
            return true;
        }

    }
}
#endif

using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(DoorSetup))]
public class DoorSetupEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        DoorSetup doorSetup = (DoorSetup)target;

        if (GUILayout.Button("Setup Doors"))
        {
            doorSetup.SetupDoors();
        }
    }
}
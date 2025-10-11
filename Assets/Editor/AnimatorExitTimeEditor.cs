using UnityEngine;
using UnityEditor;
using UnityEngine.Animations;
using UnityEditor.Animations;

public class AnimatorExitTimeEditor : EditorWindow
{
    private AnimatorController animatorController;
    private float newExitTime = 0.2f; // Default exit time
    private bool setExitTime = true;  // Whether to update exit time
    private bool enableExitTime = true; // Whether to enable or disable hasExitTime

    [MenuItem("Tools/Animator Exit Time Editor")]
    public static void ShowWindow()
    {
        GetWindow<AnimatorExitTimeEditor>("Animator Exit Time Editor");
    }

    private void OnGUI()
    {
        GUILayout.Label("Animator Exit Time Editor", EditorStyles.boldLabel);

        animatorController = (AnimatorController)EditorGUILayout.ObjectField("Animator Controller", animatorController, typeof(AnimatorController), false);
        newExitTime = EditorGUILayout.FloatField("New Exit Time", newExitTime);
        enableExitTime = EditorGUILayout.Toggle("Enable Exit Time", enableExitTime);
        setExitTime = EditorGUILayout.Toggle("Set Exit Time", setExitTime);

        if (GUILayout.Button("Apply to All Transitions"))
        {
            if (animatorController != null)
            {
                UpdateExitTime(animatorController, enableExitTime, setExitTime, newExitTime);
            }
            else
            {
                Debug.LogWarning("Please assign an Animator Controller.");
            }
        }
    }

    private void UpdateExitTime(AnimatorController controller, bool enableExit, bool updateExitTime, float exitTime)
    {
        int modifiedCount = 0;

        foreach (var layer in controller.layers)
        {
            foreach (var state in layer.stateMachine.states)
            {
                foreach (var transition in state.state.transitions)
                {
                    transition.hasExitTime = enableExit;
                    if (updateExitTime && enableExit)
                    {
                        transition.exitTime = exitTime;
                    }
                    modifiedCount++;
                }
            }
        }

        Debug.Log($"Modified {modifiedCount} transitions in {controller.name}. Exit Time Enabled: {enableExit}, New Exit Time: {(updateExitTime ? exitTime.ToString() : "Not Set")}.");
    }
}
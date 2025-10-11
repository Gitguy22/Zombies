using UnityEditor;
using UnityEngine;

namespace BloodVFX
{
    [CustomEditor(typeof(EffectController))]
    public class EffectControllerEditor : Editor
    {

        private GUIStyle headerStyle;
        SerializedProperty prefabs;


        





        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            headerStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                normal = new GUIStyleState() { textColor = Color.yellow },
                padding = new RectOffset(0, 0, 5, 0)
            };
            prefabs = serializedObject.FindProperty("prefabs");




            EffectController myScript = (EffectController)target;

   

            EditorGUILayout.PropertyField(prefabs, new GUIContent("Prefabs"), true);
            serializedObject.ApplyModifiedProperties();


            myScript.spawnNumber = EditorGUILayout.IntField("SpawnNumber", myScript.spawnNumber);
            myScript.spawnRadius = EditorGUILayout.FloatField("SpawnRadius", myScript.spawnRadius);
            myScript.minScale = EditorGUILayout.FloatField("MinScale", myScript.minScale);
            myScript.maxScale = EditorGUILayout.FloatField("MaxScale", myScript.maxScale);
            
            
            EditorGUILayout.LabelField("ForceControl", headerStyle);
            myScript.minForce = EditorGUILayout.FloatField("MinForce", myScript.minForce);
            myScript.maxForce = EditorGUILayout.FloatField("MaxForce", myScript.maxForce);
            myScript.spread = EditorGUILayout.Slider("Spread", myScript.spread, 0f, 1f);





            if (GUI.changed)
            {
                EditorUtility.SetDirty(target);
            }
        }
    }

}



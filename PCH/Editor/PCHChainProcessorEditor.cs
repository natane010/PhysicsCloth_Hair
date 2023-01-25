using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace TK.UntiyEditor
{
    using Mono;
    [CustomEditor(typeof(PCHChainProcessor))]
    public class PCHChainProcessorEditor : Editor
    {
        PCHChainProcessor controller;

        public void OnEnable()
        {
            controller = target as PCHChainProcessor;
        }
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            EditorGUILayout.ObjectField("Root Transform",controller.transform, typeof(Transform), true);

            EditorGUILayout.PropertyField(serializedObject.FindProperty("PCHSetting"), 
                new GUIContent("Physics Setting"), true);
            serializedObject.ApplyModifiedProperties();
            EditorGUILayout.PropertyField(serializedObject.FindProperty("keyWord"), 
                new GUIContent("Keyword"), true);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("allPointTransforms"), 
                new GUIContent("Transform List"), true);
        }
    }

}


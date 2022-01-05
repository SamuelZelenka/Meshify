using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class TestWindow : EditorWindow
{
    
    [MenuItem("Window/TestWindow")]
    private static void Init()
    {
        TestWindow window = (TestWindow)GetWindow(typeof(TestWindow));
        window.Show();
    }


    private void OnGUI()
    {

        Vector3 p1 = new Vector3(0,0,0);
        Vector3 p2 = new Vector3(500,500,0);
        
        Handles.DrawLine(p1, p2);
    }
}

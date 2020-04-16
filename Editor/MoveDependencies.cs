using UnityEditor;
using System.Collections.Generic;
using UnityEngine;
using System.IO;

public class MoveDependencies : EditorWindow
{

    private static MoveDependencies instance;

    private const string _assetsPath = "Assets/";
    private string _path = "Temp/";

    private List<string> _assets;

    private Vector2 _scroll = Vector2.zero;

    public static void Init()
    {
        MoveDependencies window = (MoveDependencies) GetWindow ( typeof(MoveDependencies) );
        window.Show();
    }

    private void Awake()
    {
        if (instance == null)
            instance = this;
    }

    public static void UpdateAssets(List<string> assets)
    {
        if (assets == null || assets.Count <= 0)
        {           
            return;
        }

        if (instance != null)
            instance._assets = assets;
    }

    private void OnGUI()
    {
        EditorGUILayout.Space();

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("New Path:", GUILayout.Width(60f));
        _path = EditorGUILayout.TextField(_assetsPath + " ", _path);
        EditorGUILayout.EndHorizontal();

        

        EditorGUILayout.BeginHorizontal();
        GUI.backgroundColor = Color.red;
        if (GUILayout.Button("Move to the new location"))
        {           
            MoveAssets(_assets);
        }
        GUI.backgroundColor = Color.white;
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space();

        _scroll = EditorGUILayout.BeginScrollView(_scroll);

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Total: " + _assets.Count + " assets");
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("---------------");
        EditorGUILayout.EndHorizontal();

        foreach (string asset in _assets)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(asset);
            EditorGUILayout.EndHorizontal();
        }
        EditorGUILayout.EndScrollView();
    }

    private string FixPath (string asset)
    {       
        asset = asset.Replace("////", "/");
        asset = asset.Replace("///", "/");
        asset = asset.Replace("//", "/");

        return asset;
    }
    private void MoveAssets(List<string> assets)
    {
        for (int i = 0; i < assets.Count; i++)
        {
            string oldPath = assets[i];

            string newDir = string.Concat(_assetsPath, _path);
            newDir = Path.GetDirectoryName(newDir); // REMOVE SLASHES
            newDir = newDir.Replace('\\','/'); // FIX SLASHES

            if (!Directory.Exists(newDir))
            {
                Directory.CreateDirectory(newDir);
                Debug.Log("Created");
            }

            newDir = string.Concat(newDir, '/', Path.GetFileName(assets[i]));            
                

            string newPath = string.Concat(_assetsPath, _path, "/", Path.GetFileName(assets[i]));

            newPath = FixPath(newPath);          

            //Debug.Log(oldPath + " -> " + newPath);

            //break;
            string status = AssetDatabase.MoveAsset(oldPath, newPath);

            if (status != "")
            {
                Debug.Log(status);
               // Debug.Log(oldPath + " -> " + newPath);
            }
            else {
                assets[i] = newPath;
            }
        }
    }
}


using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using System.IO;

public class SearchReferences : EditorWindow
{
    public static SearchReferences instance;

    private  string _asset = null;
    private  Object _obj = null;

    private List<string> _referenceAssets = new List<string>();

    private Vector2 _scroll = Vector2.zero;



    [MenuItem("Tools/Dependencies/Search References")]
    public static void GetWindow()
    {
        if (instance != null) return;
        instance = InitWindow();
    }

    private static SearchReferences InitWindow()
    {
        SearchReferences window = (SearchReferences)EditorWindow.GetWindow(typeof(SearchReferences));
        window.Show();

        return window;
    }

    public static void UpdateReferences(Object obj, string asset)
    {
        GetWindow();
        instance.GetReferences(obj, asset);
    }

    private void GetReferences(Object obj, string asset)
    {
        _obj = obj;
        _asset = asset;

        GetWindow();

        instance._referenceAssets = new List<string>();

        if (_obj == null) return;

        string guid = instance.GetAssetGUID(instance.LoadAsset(_asset));
        //Debug.Log(guid);
        _scroll = Vector2.zero;

        instance.GetAllAssetsWithGUID(guid, ref instance._referenceAssets);
    }

    private void OnGUI()
    {
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Object:");
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        _obj = EditorGUILayout.ObjectField(_obj , typeof(Object), true);

        if (GUILayout.Button("Update", GUILayout.Width(50f)))
        {
            string meta = string.Concat ( AssetDatabase.GetAssetPath(_obj), ".meta");
            UpdateReferences(_obj, meta);
        }

        EditorGUILayout.EndHorizontal();

        /*EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField(_asset);
        EditorGUILayout.EndHorizontal();*/

        EditorGUILayout.Space();

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("References:");
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space();

        _scroll = EditorGUILayout.BeginScrollView(_scroll);

        foreach (string asset in _referenceAssets)
        {
            EditorGUILayout.BeginHorizontal();
            
            Object refAsset = LoadAssetByPath(asset);
            string refMeta = string.Concat(asset, ".meta");
            
            EditorGUILayout.ObjectField(refAsset, typeof(Object));
            
            if (GUILayout.Button("Refs", GUILayout.Width(40f)))
            {  
                UpdateReferences(refAsset, refMeta);
            }

            EditorGUILayout.EndHorizontal();
        }
        EditorGUILayout.EndScrollView();
    }
    private string GetAssetGUID(string metaData)
    {
        if (string.IsNullOrEmpty(_asset)) return null;

        string[] lines = metaData.Split('\n');

        string guid = "";
        foreach (string line in lines)
        {
            if (line.Contains("guid:"))
            {
                guid = line.Split(' ')[1]; // GET VALUE FORM ROW
                break;
            }
        }
        return guid.Trim();
    }

    private Object LoadAssetByPath(string assetPath)
    {
        if (string.IsNullOrEmpty(assetPath)) return null;

        return AssetDatabase.LoadAssetAtPath(assetPath, typeof(Object)) as Object;
    }

    private string LoadAsset(string assetPath)
    {

        if (string.IsNullOrEmpty(assetPath)) return null;

        string metaData = "";

        using (StreamReader metaReader = new StreamReader(assetPath))
        {
            metaData = metaReader.ReadToEnd();
        }

        return metaData;
    }
    private void GetAllAssetsWithGUID(string guid, ref List<string> AssetsUsedGUID, string path = "")
    {
        // SEARCH IN FILES
        path = (path == "") ? "Assets/" : path;

        string[] fileEntries = Directory.GetFiles(path);
        foreach (string fileName in fileEntries)
        {
            if (!IsContainerAsset(fileName)) continue;

            string prefabDataFromFile = "";
            if (!IsContainGUID(guid, fileName)) continue;

            if (AssetsUsedGUID.Contains(fileName)) continue;

            AssetsUsedGUID.Add(fileName);
        }

        // SEARCH IN DIRS RECURSIVETLY
        string[] dirsEntries = Directory.GetDirectories(path);

        foreach (string dir in dirsEntries)
        {
            GetAllAssetsWithGUID(guid, ref AssetsUsedGUID, dir);
        }
    }

    private bool IsContainGUID(string guid, string assetPath)
    {
        bool isContainsGUID = false;
        string data = LoadAsset(assetPath);
        if (data.Contains(guid))
        {
            isContainsGUID = true;
        }
        return isContainsGUID;
    }

    private bool IsContainerAsset(string fileName)
    {
        if (fileName.Contains(".meta")) return false;
       
        if (!fileName.Contains(".unity")        &&
            !fileName.Contains(".prefab")       &&
            !fileName.Contains(".controller")   &&
            !fileName.Contains(".mat"))         return false;

        return true;
    }
 }
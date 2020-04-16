using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;
using System;

public class GetDependencies : EditorWindow
{
    private class Dependencie
    {
        public string assetPath;
        public string metaPath;
        public string size;
        public UnityEngine.Object obj;        
    }

    private static string _buildReport      = "Build Report";
    private static string _endReport        = "--------";
    private static string _editorLogPath    = null;
    private static string _editorLog        = null;

    public UnityEngine.Object source = null;

    private string _searchFilter = "";

    private List<Dependencie> _foundedDependencies = new List<Dependencie>();

    private int _pageSize = 500;
    private int _currentPage = 0;

    private Vector2 _currentScroll = Vector2.zero;

    [MenuItem("Tools/Dependencies/Get Dependencies")]
    static void Init()
    {
        GetDependencies window = (GetDependencies)EditorWindow.GetWindow(typeof(GetDependencies));
        
        TryReadEditorLog(GetEditorLog()); // TRY TO READ EDITOR LOG

        window.Show();
    }

    private static string GetEditorLog()
    {

#if UNITY_EDITOR_OSX
        _editorLogPath = "~/Library/Logs/Unity/Editor.log";
        string userFolderPath = System.Environment.GetFolderPath(System.Environment.SpecialFolder.MyDocuments);
        _editorLogPath = _editorLogPath.Replace("~", userFolderPath);
        return _editorLogPath;
#endif

#if UNITY_EDITOR_WIN
        string appdatapath = System.Environment.GetFolderPath(System.Environment.SpecialFolder.LocalApplicationData);
        _editorLogPath = string.Concat(appdatapath, "\\Unity\\Editor\\Editor.log");
        return _editorLogPath;
#else
        return null;
#endif

    }

    private static void TryReadEditorLog(string pathToEditorLog)
    {
        Path.GetFullPath(pathToEditorLog);
        if (string.IsNullOrEmpty(pathToEditorLog)) return;
        if(!File.Exists(pathToEditorLog)) return;

        //string editorLog = System.IO.File.ReadAllText(pathToEditorLog);

        FileStream fs = new FileStream(pathToEditorLog, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.ReadWrite);
        StreamReader sr = new StreamReader(fs);
        if (fs != null && sr != null)
        {
            _editorLog = sr.ReadToEnd();
        }

        string[] splitString = { _buildReport };
        string[] lines = _editorLog.Split(splitString, System.StringSplitOptions.None);

        _editorLog = lines[lines.Length - 1].ToString(); // GET LAST BUILD INFO

        splitString[0] = _endReport;

        lines = _editorLog.Split(splitString, System.StringSplitOptions.None);

        _editorLog = lines[0].ToString();

        _editorLog = string.Concat(_buildReport, " ", _editorLog , " " , _endReport); // FIX SEARCH
    }

    private void OnGUI()
    {
        var style = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter };
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("<<< MAKE BUILD FIRST! >>>", style);
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space();

        EditorGUILayout.BeginHorizontal();
        if (string.IsNullOrEmpty(_editorLog))
        {
            source = EditorGUILayout.ObjectField(source, typeof(TextAsset), true);
        }
        else
        {
            EditorGUILayout.LabelField(_editorLogPath);
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Search:", GUILayout.Width(60f));
        _searchFilter = EditorGUILayout.TextField(_searchFilter);
        
        GUI.backgroundColor = Color.red;
        if (GUILayout.Button("Move >>", GUILayout.Width(60f)))
        {
            MoveDependencies.Init();
            MoveDependencies.UpdateAssets(GetListAssets());
        }
        GUI.backgroundColor = Color.white;

        EditorGUILayout.EndHorizontal();

        if (GUILayout.Button("Search!"))
        {
            if (string.IsNullOrEmpty(_editorLog) && source == null)
                ShowNotification(new GUIContent("No Log file selected for searching"));

            else if (!string.IsNullOrEmpty(_editorLog) || source.GetType() == typeof(TextAsset))
            {
                ResetPages();
                SearchDependencies();
            }
        }

        EditorGUILayout.Space();
        ListDependencies();
    }

    private void ListDependencies()
    {
        if (_foundedDependencies.Count < 1) return;

        ShowPages();

        _currentScroll = EditorGUILayout.BeginScrollView(_currentScroll);

        for (int i = _currentPage * _pageSize;  i  < (_currentPage+1)*_pageSize; i++)
        { 
            if (i >= _foundedDependencies.Count || i < 0) break;

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(_foundedDependencies[i].size, GUILayout.MaxWidth(60f));
            EditorGUILayout.ObjectField(_foundedDependencies[i].obj, typeof(UnityEngine.Object), true);
            if (GUILayout.Button("Refs", GUILayout.Width(40f)))
            { 
                SearchReferences.UpdateReferences(_foundedDependencies[i].obj, _foundedDependencies[i].metaPath);
            }
            EditorGUILayout.EndHorizontal();
        }

        EditorGUILayout.EndScrollView();
    }

    private void ResetPages()
    {

        _currentPage = 0;
        _currentScroll = Vector2.zero;
    }

    private void ShowPages() // LIMIT OUTPUT FOR EDITOR PERFORMANCE
    {
        int dependenciesCount = _foundedDependencies.Count;

        if (dependenciesCount < _pageSize) return;

        int totalPages = Mathf.CeilToInt((float) dependenciesCount / _pageSize);
       
        EditorGUILayout.BeginHorizontal();

        EditorGUILayout.LabelField("Page size", GUILayout.Width(60f));
        _pageSize = EditorGUILayout.IntField(_pageSize, GUILayout.Width(80f));
        if (_pageSize < 1) _pageSize = 1;

        if (GUILayout.Button("<<")) // PREV PAGE
            _currentPage--;

        int page = EditorGUILayout.IntField(_currentPage + 1 ,  GUILayout.Width(80f));
        _currentPage = page - 1;
        EditorGUILayout.LabelField(" / "+ totalPages.ToString(), GUILayout.Width(80f));

        if (GUILayout.Button(">>")) // NEXT PAGE
            _currentPage++;

        if (_currentPage < 1) _currentPage = 0;
        if (_currentPage >= totalPages) _currentPage = totalPages - 1;

        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space();
    }

    private void SearchDependencies()
    {
        string log = string.IsNullOrEmpty(_editorLog)? source.ToString() : _editorLog;

        string[] splitString = { _buildReport };
        string[] lines = log.Split(splitString, System.StringSplitOptions.None);

        log = lines[lines.Length - 1].ToString(); // GET LAST BUILD INFO

        splitString[0] = _endReport;

        lines = log.Split(splitString, System.StringSplitOptions.None);

        log = lines[0].ToString(); // GET LAST BUILD INFO

        lines = log.Split('\n'); // SPLITIT BY LINES

        int dependencies = 0;

        List<string> assets = new List<string>();

        foreach (var line in lines)
        {
            if (!line.Contains("Assets/")) continue;
            if (!line.Contains(_searchFilter)) continue;

            assets.Add(line);

            dependencies++;
        }

        Debug.Log("TOTAL ASSETS: " + dependencies);

        CombineAssets(assets);
    }

    private void CombineAssets(List<string> assets)
    {
        _foundedDependencies = new List<Dependencie>();

        foreach (string asset in assets)
        {
            Dependencie currentDependencie = GetDependencieFromPath(asset);
           
            if (currentDependencie == null) continue;

           _foundedDependencies.Add(currentDependencie);
        }        
    }

    private List<string> GetListAssets()
    {
        List<string> assets = new List<string>();

        foreach (Dependencie dependencie in _foundedDependencies)
            assets.Add(dependencie.assetPath);                    

        return assets;
    }

    private Dependencie GetDependencieFromPath(string assetPath)
    {      
        
        string assets = "Assets/";

        string[] split = { string.Concat(' ', assets) };

        string[] parts = assetPath.Split(split, System.StringSplitOptions.None);

        if (parts.Length < 2) return null;

        assetPath = string.Concat(assets, parts[1]);

        assetPath = assetPath.Trim();

        Dependencie dependencie = new Dependencie();

        dependencie.assetPath = assetPath;
        dependencie.metaPath = string.Concat(assetPath, ".meta");
        dependencie.obj= AssetDatabase.LoadAssetAtPath (assetPath, typeof(UnityEngine.Object)) as UnityEngine.Object;
        dependencie.size = parts[0].Split('\t')[0].Trim();

        return dependencie;
    }
}
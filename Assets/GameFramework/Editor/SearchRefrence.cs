using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System.IO;
using Cysharp.Threading.Tasks;

public class SearchRefrence : EditorWindow
{
    private static Object searchObject;
    private List<Object> noReferenceObjects = new List<Object>();
    private Dictionary<Object, List<Object>> resultDict = new (1);
    private Dictionary<Object, bool> resultFoldout = new(1);
    private Dictionary<string, string> fileContentCache = new Dictionary<string, string>(1000);
    private bool singleAssetMode = true;
    private Vector2 scrollViewPosition;
    private bool noReferenceFoldout = true;
    private bool hasReferenceFoldout = true;

    /// <summary>
    /// 查找资源引用
    /// </summary>
    [MenuItem("Assets/查找资源引用")]
    static async void Search()
    {
        if (Selection.assetGUIDs.Length > 0)
        {
            searchObject = AssetDatabase.LoadAssetAtPath<Object>(AssetDatabase.GUIDToAssetPath(Selection.assetGUIDs[0]));
        }
        SearchRefrence window = (SearchRefrence)EditorWindow.GetWindow(typeof(SearchRefrence), false, "查找资源引用", true);
        window.Show();
        await UniTask.DelayFrame(1);
        window.DoSearch();
    }

    private void OnGUI()
    {
        EditorGUILayout.HelpBox("1.本工具仅搜索的Prefab, SpriteAtlas对目标资源的引用。\n2.如果一个资源未搜索到对其的引用，并不代表该资源没有被使用，它有可能是被其他类型的资源引用，也可能是通过代码动态加载使用的。\n3.如果搜索目标是一个文件夹，将会依次搜索该文件夹下的所有资源", MessageType.Info, true);
        EditorGUILayout.BeginHorizontal();
        searchObject = EditorGUILayout.ObjectField(searchObject, typeof(Object), true);
        if (GUILayout.Button("Search", GUILayout.Width(200)))
        {
            DoSearch();
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(10);

        scrollViewPosition = EditorGUILayout.BeginScrollView(scrollViewPosition);

        EditorGUILayout.BeginVertical();

        if (noReferenceObjects.Count > 0)
        {
            //先展示没有任何引用的资源
            noReferenceFoldout = EditorGUILayout.Foldout(noReferenceFoldout, "以下资源未搜索到引用");
            if (noReferenceFoldout)
            {
                foreach (var obj in noReferenceObjects)
                {
                    EditorGUILayout.ObjectField(obj, typeof(Object), true);
                }
            }
            if (GUILayout.Button("删除所有没有搜索到引用的资源（请确认好再执行）"))
            {
                DeleteAllNoReferenceAssets();
            }
        }

        EditorGUILayout.Space(10);

        if (resultDict.Count > 0)
        {
            hasReferenceFoldout = EditorGUILayout.Foldout(hasReferenceFoldout, "以下为有引用的资源详情");
            if (hasReferenceFoldout)
            {
                if (singleAssetMode)
                {
                    foreach (var kvp in resultDict)
                    {
                        foreach (var obj in kvp.Value)
                        {
                            EditorGUILayout.ObjectField(obj, typeof(Object), true);
                        }
                    }
                }
                else
                {
                    foreach (var kvp in resultDict)
                    {
                        EditorGUILayout.ObjectField(kvp.Key, typeof(Object), true);
                        resultFoldout[kvp.Key] = EditorGUILayout.Foldout(resultFoldout[kvp.Key], kvp.Key.name);
                        if (resultFoldout[kvp.Key])
                        {
                            foreach (var obj in kvp.Value)
                            {
                                EditorGUILayout.ObjectField(obj, typeof(Object), true);
                            }
                        }
                        EditorGUILayout.Space(10);
                    }
                }
            }
        }
        EditorGUILayout.EndVertical();

        EditorGUILayout.EndScrollView();
    }

    private void DoSearch()
    {
        if (searchObject == null)
            return;

        noReferenceObjects.Clear();
        resultDict.Clear();
        resultFoldout.Clear();

        string assetPath = AssetDatabase.GetAssetPath(searchObject);
        string[] assetGuids = null;
        if (searchObject is DefaultAsset)
        {
            singleAssetMode = false;
            assetGuids = AssetDatabase.FindAssets("", new string[] { assetPath });
        }
        else
        {
            singleAssetMode = true;
            assetGuids = new string[] { AssetDatabase.AssetPathToGUID(assetPath) };
        }

        string[] prefabGuids = AssetDatabase.FindAssets("t:Prefab", new[] { "Assets" });
        string[] spriteAtlasGuids = AssetDatabase.FindAssets("t:spriteatlas", new[] { "Assets" });

        float length = prefabGuids.Length + spriteAtlasGuids.Length;
        for (int i = 0; i < prefabGuids.Length; i++)
        {
            string filePath = AssetDatabase.GUIDToAssetPath(prefabGuids[i]);
            bool isCanceled = EditorUtility.DisplayCancelableProgressBar("Checking", filePath, i / length);
            if (isCanceled)
            {
                EditorUtility.ClearProgressBar();
                return;
            }

            if (fileContentCache.TryGetValue(filePath, out var content) == false)
            {
                content = File.ReadAllText(filePath);
                fileContentCache.Add(filePath, content);
            }
            
            foreach (var assetGuid in assetGuids)
            {
                if (content.Contains(assetGuid))
                {
                    Object searchObject = AssetDatabase.LoadAssetAtPath(AssetDatabase.GUIDToAssetPath(assetGuid), typeof(Object));
                    Object fileObject = AssetDatabase.LoadAssetAtPath(filePath, typeof(Object));
                    if (resultDict.TryGetValue(searchObject, out var list) == false)
                    {
                        list = new List<Object>();
                        resultDict.Add(searchObject, list);
                    }
                    list.Add(fileObject);
                }
            }
        }

        for (int i = 0; i < spriteAtlasGuids.Length; i++)
        {
            string filePath = AssetDatabase.GUIDToAssetPath(spriteAtlasGuids[i]);
            EditorUtility.DisplayCancelableProgressBar("Checking", filePath, (i + prefabGuids.Length) / length);

            if (fileContentCache.TryGetValue(filePath, out var content) == false)
            {
                content = File.ReadAllText(filePath);
                fileContentCache.Add(filePath, content);
            }
            foreach (var assetGuid in assetGuids)
            {
                if (content.Contains(assetGuid))
                {
                    Object searchObject = AssetDatabase.LoadAssetAtPath(AssetDatabase.GUIDToAssetPath(assetGuid), typeof(Object));
                    Object fileObject = AssetDatabase.LoadAssetAtPath(filePath, typeof(Object));
                    if (resultDict.TryGetValue(searchObject, out var list) == false)
                    {
                        list = new List<Object>();
                        resultDict.Add(searchObject, list);
                    }
                    list.Add(fileObject);
                }
            }
        }

        foreach (var key in resultDict.Keys)
        {
            resultFoldout.Add(key, true);
        }

        foreach (var assetGuid in assetGuids)
        {
            Object searchObject = AssetDatabase.LoadAssetAtPath(AssetDatabase.GUIDToAssetPath(assetGuid), typeof(Object));
            if (resultDict.ContainsKey(searchObject) == false)
            {
                noReferenceObjects.Add(searchObject);
            }
        }

        EditorUtility.ClearProgressBar();
    }

    private void DeleteAllNoReferenceAssets()
    {
        bool result = EditorUtility.DisplayDialog("删除资源", "你确定这些资源没有被任何形式引用了嘛！！！", "果断删除", "容我三思");
        if (result)
        {
            foreach (var obj in noReferenceObjects)
            {
                AssetDatabase.DeleteAsset(AssetDatabase.GetAssetPath(obj));
            }
            noReferenceObjects.Clear();
        }
    }
}

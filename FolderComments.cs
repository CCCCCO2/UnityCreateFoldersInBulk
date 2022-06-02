using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace Editor.Instance.FolderComments
{
    [CanEditMultipleObjects, CustomEditor(typeof(DefaultAsset))]
    public class ProjectFolderCommentsInspector : UnityEditor.Editor
    {
        private List<string> _assetsPath = new List<string>();
        private string _comment = string.Empty;
        private bool _isCommentChanged = false;

        void OnEnable()
        {
            _assetsPath.Clear();
            for (int i = 0; i < targets.Length; ++i)
            {
                var assetPathTmp = AssetDatabase.GetAssetPath(targets[i]);
                if (!AssetDatabase.IsValidFolder(assetPathTmp))
                    continue;

                _assetsPath.Add(assetPathTmp);
            }

            if (_assetsPath.Count > 0)
                _comment = ProjectFolderAssetDataManager.instance.AssetPathToComment(_assetsPath[0]);
        }

        void OnDestroy()
        {
            if (_isCommentChanged)
            {
                ProjectFolderAssetDataManager.instance.SaveToFile();
            }
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            //只处理文件夹显示
            if (0 == _assetsPath.Count)
                return;

            bool enabled = GUI.enabled;
            GUI.enabled = true;

            DrawFolder();

            GUI.enabled = enabled;
        }

        private void DrawFolder()
        {
            EditorGUILayout.PrefixLabel("Comments");

            GUI.changed = false;
            _comment = EditorGUILayout.TextArea(_comment, GUILayout.MinHeight(48));
            if (GUI.changed)
            {
                for (int i = 0; i < _assetsPath.Count; ++i)
                    ProjectFolderAssetDataManager.instance.SetComment(_assetsPath[i], _comment);

                _isCommentChanged = true;
                EditorUtility.SetDirty(target);

                EditorApplication.RepaintProjectWindow();
            }
        }
    }

    /// <summary>
    /// 编辑器文件夹绘制使用的相关数据管理器
    /// </summary>
    [InitializeOnLoad]
    public class ProjectFolderAssetDataManager
    {
        public class ProjectFolderAssetData
        {
            //该路径仅仅作为参考(方便查看配置文本)，并无实际意义
            public string assetPath;
            public string comment;
        }

        static public ProjectFolderAssetDataManager instance
        {
            get
            {
                if (null == _instance)
                {
                    _instance = new ProjectFolderAssetDataManager();
                    _instance.LoadFromFile(_instance.SAVE_DATA_PATH);
                }
                return _instance;
            }
        }
        static private ProjectFolderAssetDataManager _instance = null;

        //保存路径
        private string SAVE_DATA_PATH
        {
            get
            {
                return Application.dataPath + "/../ProjectSettings/ProjectFolderCommentsData.txt";
            }
        }

        //文件夹注释字典
        private Dictionary<string, ProjectFolderAssetData> _folderDatas = new Dictionary<string, ProjectFolderAssetData>();

        static ProjectFolderAssetDataManager()
        {
            EditorApplication.projectWindowItemOnGUI += ProjectWindowItemOnGUI;
        }

        /// <summary>
        /// 获取文件夹注释
        /// <param name="assetPath">文件夹路径</param>
        /// <return>文件夹注释，如果没有则返回空字符串</return>
        /// </summary>
        public string AssetPathToComment(string assetPath)
        {
            var guid = AssetDatabase.AssetPathToGUID(assetPath);
            return AssetGUIDToComment(guid);
        }

        /// <summary>
        /// 获取文件夹注释
        /// <param name="guid">文件夹唯一id</param>
        /// <return>文件夹注释，如果没有则返回空字符串</return>
        /// </summary>
        public string AssetGUIDToComment(string guid)
        {
            string retValue = null;
            ProjectFolderAssetData findData = null;
            if (_folderDatas.TryGetValue(guid, out findData))
                retValue = findData.comment;
            else
                retValue = string.Empty;

            return retValue;
        }

        /// <summary>
        /// 获取文件夹数据
        /// <param name="guid">文件夹唯一id</param>
        /// <return>文件夹数据，没有则返回空</return>
        /// </summary>
        public ProjectFolderAssetData GetFolderData(string guid)
        {
            ProjectFolderAssetData findData = null;
            _folderDatas.TryGetValue(guid, out findData);
            return findData;
        }

        /// <summary>
        /// 设置文件夹注释，并自动保存到文件
        /// <param name="assetPath">文件夹路径</param>
        /// <param name="comment">文件夹注释</param>
        /// </summary>
        public void SetComment(string assetPath, string comment)
        {
            if (string.IsNullOrEmpty(assetPath))
            {
                Debug.LogError("ProjectFolderAssetDataManager SetComment erorr: assetPath is empty");
                return;
            }

            if (!assetPath.StartsWith("Assets"))
            {
                Debug.LogError("ProjectFolderAssetDataManager SetComment erorr: not Unity project path=" + assetPath);
                return;
            }

            ProjectFolderAssetData findData = null;
            var guid = AssetDatabase.AssetPathToGUID(assetPath);
            if (!_folderDatas.TryGetValue(guid, out findData))
            {
                findData = new ProjectFolderAssetData();
                findData.assetPath = assetPath;
                findData.comment = comment;
                _folderDatas.Add(guid, findData);
            }
            else
            {
                if (string.IsNullOrEmpty(comment))
                    _folderDatas.Remove(guid);
                else
                {
                    findData.assetPath = assetPath;
                    findData.comment = comment;
                }
            }
        }

        /// <summary>
        /// 保存文件夹信息到文件中
        /// <param name="path">保存路径</param>
        /// </summary>
        public void SaveToFile()
        {
            SaveToFile(SAVE_DATA_PATH);
        }
        private void SaveToFile(string path)
        {
            if (_folderDatas.Count == 0)
            {
                System.IO.File.Delete(path);
                return;
            }

            var saveStr = new System.Text.StringBuilder();
            foreach (var iter in _folderDatas)
            {
                saveStr.Append(iter.Key);
                saveStr.Append('\n');
                saveStr.Append(iter.Value.assetPath);
                saveStr.Append('\n');
                saveStr.Append(iter.Value.comment);
                saveStr.Append('\n');
            }

            if (saveStr.Length > 0)
                saveStr.Remove(saveStr.Length - 1, 1);

            var directoryIath = new System.IO.DirectoryInfo(path).Parent;
            if (!directoryIath.Exists)
            {
                directoryIath.Create();
            }

            System.IO.File.WriteAllText(path, saveStr.ToString(), System.Text.Encoding.UTF8);
        }

        /// <summary>
        /// 从文件读取文件夹信息
        /// <param name="path">保存路径</param>
        /// </summary>
		private void LoadFromFile(string path)
        {
            _folderDatas.Clear();

            if (!System.IO.File.Exists(path))
                return;

            try
            {
                using (var readStream = new System.IO.StreamReader(path))
                {
                    while (true)
                    {
                        var guid = readStream.ReadLine();
                        if (string.IsNullOrEmpty(guid))
                            break;

                        var assetPath = readStream.ReadLine();
                        if (string.IsNullOrEmpty(assetPath))
                        {
                            throw new System.Exception("ProjectFolderAssetDataManager LoadFromFile error: not found assetPath, guid=" + guid);
                        }

                        var comment = readStream.ReadLine();
                        if (string.IsNullOrEmpty(comment))
                        {
                            throw new System.Exception("ProjectFolderAssetDataManager LoadFromFile error: not found comment, name=" + assetPath);
                        }

                        var folderAssestData = new ProjectFolderAssetData();
                        folderAssestData.assetPath = assetPath;
                        folderAssestData.comment = comment;

                        _folderDatas.Add(guid, folderAssestData);
                    }
                    readStream.Close();
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError("ProjectFolderAssetDataManager LoadFromFile exception: e=" + e);
                System.IO.File.Delete(path);
            }
        }

        static private GUIStyle _guiStyleLabelTree = null;
        static private GUIStyle _guiStyleLabelNotTree = null;

        static private void ProjectWindowItemOnGUI(string guid, Rect selectionRect)
        {
            var folderData = ProjectFolderAssetDataManager.instance.GetFolderData(guid);
            if (null == folderData || string.IsNullOrEmpty(folderData.comment))
                return;

            if (null == _guiStyleLabelTree)
            {
                _guiStyleLabelTree = EditorStyles.label;
                _guiStyleLabelNotTree = EditorStyles.label;

                _guiStyleLabelTree.fontSize = 12;
                _guiStyleLabelNotTree.fontSize = 10;
            }

            var aliasContent = new GUIContent(folderData.comment);
            var isTree = IsTreeView(selectionRect);
            var labelStyle = isTree ? _guiStyleLabelTree : _guiStyleLabelNotTree;
            Vector2 labelSize = labelStyle.CalcSize(aliasContent);
            float offsetYWhenCurrentSelectAsset = 0;

            if (!isTree)
            {
                IsIconSmall(ref selectionRect);

                if (null != Selection.objects)
                {
                    if (null != System.Array.Find(Selection.objects, v => AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(v)) == guid))
                        offsetYWhenCurrentSelectAsset = -labelSize.y * 0.167f;
                }
            }

            Rect textRect = new Rect(
                selectionRect.x + Mathf.Max(0, (selectionRect.width - labelSize.x) * 0.5f),
                selectionRect.yMax + (isTree ? -labelSize.y - labelSize.y * 0.167f : labelSize.y * 0.33f - labelSize.y) + offsetYWhenCurrentSelectAsset,
                labelSize.x, labelSize.y);

            float cropWidth = selectionRect.width;

            if (isTree)
            {
                textRect.width = System.Math.Min(labelSize.x, selectionRect.width / 3);
                cropWidth = textRect.width;
                textRect.x = selectionRect.xMax - textRect.width;
                selectionRect.y = selectionRect.y;
            }

            aliasContent.text = CropText(labelStyle, aliasContent.text, cropWidth);
            EditorGUI.LabelField(textRect, aliasContent, labelStyle);
        }

        private static bool IsTreeView(Rect rect)
        {
            return rect.height <= 21f;
        }

        // https://github.com/PhannGor/unity3d-rainbow-folders/blob/master/Assets/Plugins/RainbowFolders/Editor/Scripts/RainbowFoldersBrowserIcons.cs
        private static bool IsIconSmall(ref Rect rect)
        {
            var isSmall = rect.width > rect.height;

            if (isSmall)
                rect.width = rect.height;
            else
                rect.height = rect.width;
            return isSmall;
        }

        static private System.Reflection.MethodInfo _getNumCharactersThatFitWithinWidth;

        /// <summary>
        /// UnityEditor.ObjectListArea - GetCroppedLabelText (G:1211)
        /// </summary>
        /// <param name="self"></param>
        /// <param name="text"></param>
        /// <param name="cropWidth"></param>
        /// <param name="symbol"></param>
        /// <returns></returns>
        public static string CropText(GUIStyle self, string text, in float cropWidth, string symbol = "…")
        {
            if (null == _getNumCharactersThatFitWithinWidth)
            {
                _getNumCharactersThatFitWithinWidth = typeof(GUIStyle).GetMethod("GetNumCharactersThatFitWithinWidth",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            }

            int thatFitWithinWidth = (int)_getNumCharactersThatFitWithinWidth.Invoke(self, new object[] { text, cropWidth });

            int num;
            switch (thatFitWithinWidth)
            {
                case -1:
                    return text;
                case 0:
                case 1:
                    num = 0;
                    break;
                default:
                    num = thatFitWithinWidth != text.Length ? 1 : 0;
                    break;
            }
            text = num == 0 ? text : text.Substring(0, thatFitWithinWidth - 1) + symbol;
            return text;
        }
    }
}
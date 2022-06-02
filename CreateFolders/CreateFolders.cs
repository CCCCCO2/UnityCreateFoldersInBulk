using UnityEngine;
using UnityEditor;
using System;
using System.IO;
using System.Text.RegularExpressions;
using SFB;

public class CreateFolders : EditorWindow
{
    [UnityEditor.MenuItem("编辑器扩展/创建预制文件夹目录")]
    public static void Init()
    {
        Rect wr = new Rect(0, 0, 300, 500);
        CreateFolders window = (CreateFolders)EditorWindow.GetWindow(typeof(CreateFolders), false, "创建预制文件夹目录", true);
        window.Show();
    }

    StreamWriter writer;
    StreamReader reader;
    private string directoryStruct = "";
    Vector2 scrollPosition;

    /// <summary>
    /// 窗口GUI部分
    /// </summary>
    void OnGUI()
    {
        // 直接创建预制文件夹目录
        GUILayout.Label("预制文件夹目录", EditorStyles.boldLabel);

        if (GUILayout.Button("一键创建预制文件夹目录", GUILayout.Height(40)))
        {
            GetDefaultPaths();
            GenerateFolder();
        }

        // 自定义一键创建文件夹目录
        EditorGUILayout.Space(20);
        GUILayout.Label("自定义创建", EditorStyles.boldLabel);
        GUILayout.Label("输入自定义创建目录结构：");

        scrollPosition = GUILayout.BeginScrollView(scrollPosition, false, true);
        directoryStruct = EditorGUILayout.TextArea(directoryStruct);
        GUILayout.EndScrollView();
        EditorGUILayout.Space(40);


        if (GUILayout.Button("导入默认预制配置进行修改", GUILayout.Height(30)))
        {
            GetDefaultPaths();
            ShowNotification(new GUIContent("导入默认预制成功"));
        }
        if (GUILayout.Button("追加导入 .txt 配置", GUILayout.Height(30)))
        {
            ImportTxtFile();
        }
        if (GUILayout.Button("导出 .txt 配置", GUILayout.Height(30)))
        {
            if (!string.IsNullOrWhiteSpace(directoryStruct))
            {
                ExportTxtFile();
            }
            else
            {
                ShowNotification(new GUIContent("导出目录结构不能为空"));
                Debug.LogWarning("<color=red><b>字符不能为空或符号</b></color>");
            }

        }
        if (GUILayout.Button("创建文件夹", GUILayout.Height(30)))
        {
            if (!string.IsNullOrWhiteSpace(directoryStruct))
            {
                GenerateFolder();
            }
            else
            {
                ShowNotification(new GUIContent("创建目录结构不能为空或符号"));
                Debug.LogWarning("<color=red><b>字符不能为空或符号</b></color>");
            }
        }
    }

    /// <summary>
    /// 获取默认预制文件夹目录
    /// </summary>
    private void GetDefaultPaths()
    {
        string defaultpaths = "_Temp\n" + "Editor\n" + "ImportPackages\n" + "Plugins\n" + "Scenes\n" + "Scripts\n" +
            "Imports/Audios\n" + "Imports/Characters\n" + "Imports/Effects\n" + "Imports/Objects\n" + "Imports/Others\n" +
            "Materials/Characters\n" + "Materials/Effects\n" + "Materials/Environments\n" + "Materials/Objects\n" +
            "Materials/Others\n" + "Materials/Physic\n" + "Materials/UI\n" +
            "Others/GameSettings\n" + "Others/Other\n" + "Others/Pipeline\n" + "Others/Post_Process\n" + "Others/Vedios\n" +
            "Resources/Data\n" + "Resources/GameObjects/Instances\n" + "Resources/GameObjects/LOD\n" + "Resources/Textures\n" +
            "Shaders/Import\n" + "Shaders/Include\n" + "Shaders/Generic\n" + "Shaders/URP\n" +
            "Textures/Characters\n" + "Textures/Characters\n" + "Textures/Effects\n" + "Textures/Environments\n" +
            "Textures/Objects\n" + "Textures/Others\n" + "Textures/RemderTarget\n" + "Textures/UI\n" +
            "Prefabs/Audios\n" + "Prefabs/Cameras\n" + "Prefabs/Characters\n" + "Prefabs/Colliders/Collider\n" + "Prefabs/Colliders/Trigger\n" +
            "Prefabs/Effects/Cameras\n" + "Prefabs/Effects/Flares\n" + "Prefabs/Effects/Objects\n" + "Prefabs/Effects/Others\n" + "Prefabs/Effects/Particles\n" +
            "Prefabs/Lightings/Light\n" + "Prefabs/Lightings/LightProbe\n" + "Prefabs/Lightings/ReflectionProbe\n" +
            "Prefabs/Objects/Dynamic\n" + "Prefabs/Objects/Static\n" +
            "Prefabs/Others/Navigation\n" + "Prefabs/Others/Wind\n" +
            "Prefabs/Package\n" + "Prefabs/UI\n";
        directoryStruct = defaultpaths;
    }

    /// <summary>
    /// 创建 path 路径文件夹
    /// </summary>
    /// <param name="path"></param>1
    private void CreateDirectoryInAsset(string path)
    {
        string prjPath = UnityEngine.Application.dataPath;
        Directory.CreateDirectory(prjPath + "/" + path);
    }

    /// <summary>
    /// Button：创建文件夹
    /// </summary>
    private void GenerateFolder()
    {
        string fs = directoryStruct;
        string[] fLines = Regex.Split(fs, "\n|\r|\r\n");

        for (int i = 0; i < fLines.Length; i++)
        {
            CreateDirectoryInAsset(fLines[i]);
        }
        //刷新unity资源显示
        AssetDatabase.Refresh();
        this.ShowNotification(new GUIContent("创建成功"));
        Debug.Log("<color=green><b>创建成功</b></color>");
    }

    /// <summary>
    /// 追加导入 .txt 文件
    /// </summary>
    private void ImportTxtFile()
    {
        var extensions = new[] {
        new ExtensionFilter("Text Files", "txt", "doc", "docx", "wps"),
        new ExtensionFilter("All Files", "*" ),
        };
        var paths = StandaloneFileBrowser.OpenFilePanel("Open File", "", extensions, true);

        if (paths.Length !=0)
        {
            foreach (var path in paths)
            {
                string readText = File.ReadAllText(path);
                directoryStruct += readText;
            }
            this.ShowNotification(new GUIContent("导入成功"));
        }
        else
        {
            this.ShowNotification(new GUIContent("导入取消"));
        }
    }
    /// <summary>
    /// 导出 .txt 配置文件
    /// </summary>
    private void ExportTxtFile()
    {
        var extensionList = new[] {
        new ExtensionFilter("Text", "txt"),
        new ExtensionFilter("Binary", "bin"),
        };

        var path = StandaloneFileBrowser.SaveFilePanel("Save File", "", "FoldersCreateConfig", extensionList);
        if (path != "")
        {
            File.WriteAllText(path, directoryStruct);
            this.ShowNotification(new GUIContent("导出成功"));
        }
        else
        {
            this.ShowNotification(new GUIContent("导出取消"));
        }
    }
}

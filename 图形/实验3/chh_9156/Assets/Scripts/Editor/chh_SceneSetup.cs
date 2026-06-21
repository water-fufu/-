using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using UnityEditor.SceneManagement;

/// <summary>
/// Unity Editor 工具脚本：一键自动搭建"汉诺塔"完整游戏场景
///
/// 使用方法：
///   1. 打开 Unity → 顶部菜单栏 → Tools → Setup Hanoi Game
///   2. 确认弹窗 → 所有场景对象自动生成
///   3. 点击 Play 即可运行
///
/// 注意：此脚本必须放在 Assets/Scripts/Editor/ 目录下
/// </summary>
public class chh_SceneSetup
{
    // ── 场景参数常量（与实验指导书完全一致）──
    private static readonly Vector3 BASE_POS   = new Vector3(0, -0.5f, 0);
    private static readonly Vector3 BASE_SCALE = new Vector3(8, 0.2f, 3);

    private static readonly Vector3[] PILLAR_POS = {
        new Vector3(-2.5f, 1, 0),   // A 柱（左）
        new Vector3(0, 1, 0),        // B 柱（中）
        new Vector3(2.5f, 1, 0)      // C 柱（右）
    };
    private static readonly string[] PILLAR_NAMES = {
        "chh_Pillar_Left", "chh_Pillar_Mid", "chh_Pillar_Right"
    };
    private static readonly Vector3 PILLAR_SCALE = new Vector3(0.3f, 3, 0.3f);

    // 圆盘定义：名称, Scale, 颜色, sizeLevel
    private static readonly (string name, Vector3 scale, Color color, int level)[] DISK_DEFS = {
        ("chh_Disk_1", new Vector3(1.6f, 0.15f, 1.6f), Color.blue,   1),
        ("chh_Disk_2", new Vector3(1.2f, 0.15f, 1.2f), Color.green,  2),
        ("chh_Disk_3", new Vector3(0.8f, 0.15f, 0.8f), Color.red,    3)
    };

    private static readonly Color BASE_COLOR   = new Color(139f/255f, 69f/255f, 19f/255f);   // 棕色
    private static readonly Color PILLAR_COLOR = new Color(169f/255f, 169f/255f, 169f/255f); // 金属灰

    private const string MATS_PATH   = "Assets/Materials";
    private const string PREFS_PATH  = "Assets/Prefabs";
    private const string SCENE_PATH  = "Assets/Scenes/MainScene.unity";

    // ═══════════════════════════════════════════════
    // 菜单入口
    // ═══════════════════════════════════════════════

    [MenuItem("Tools/Setup Hanoi Game")]
    static void SetupScene()
    {
        if (!EditorUtility.DisplayDialog("搭建汉诺塔场景",
                "将自动生成：底座 + 三柱 + 三圆盘预制体 + 材质 + UI + GameManager\n\n" +
                "如果已有同名对象将被替换。继续？", "开始搭建", "取消"))
            return;

        // ── 1. 清理 + 建文件夹 ──
        CleanScene();
        EnsureFolders();

        // ── 2. 创建材质 ──
        Material baseMat   = CreateMat("chh_Material_Base",   BASE_COLOR);
        Material pillarMat = CreateMat("chh_Material_Pillar", PILLAR_COLOR);
        Material[] diskMats = new Material[3];
        for (int i = 0; i < 3; i++)
            diskMats[i] = CreateMat("chh_Material_Disk_" + (i + 1), DISK_DEFS[i].color);

        // ── 3. 创建圆盘预制体（返回预制体引用用于 GM 配置）──
        GameObject[] diskPrefabs = CreateDiskPrefabs(diskMats);

        // ── 4. 创建场景物体 ──
        CreateBase(baseMat);
        chh_Pillar[] pillars = CreatePillars(pillarMat);
        GameObject gmObj = CreateGameManager(pillars, diskPrefabs);
        CreateUI(gmObj);
        SetupCamera();

        // ── 5. 保存 ──
        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene(), SCENE_PATH);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        EditorUtility.DisplayDialog("搭建完成！",
            "汉诺塔游戏场景已就绪。\n\n点击顶部 ▶ Play 按钮即可开始游戏。",
            "好的");
    }

    // ═══════════════════════════════════════════════
    // 清理
    // ═══════════════════════════════════════════════

    private static void CleanScene()
    {
        string[] names = { "chh_Base", "chh_Pillar_Left", "chh_Pillar_Mid",
                           "chh_Pillar_Right", "chh_GameManager", "Canvas", "Main Camera" };
        foreach (string n in names)
        {
            GameObject obj = GameObject.Find(n);
            if (obj != null) Object.DestroyImmediate(obj);
        }
    }

    // ═══════════════════════════════════════════════
    // 文件夹
    // ═══════════════════════════════════════════════

    private static void EnsureFolders()
    {
        if (!AssetDatabase.IsValidFolder(MATS_PATH))
            AssetDatabase.CreateFolder("Assets", "Materials");
        if (!AssetDatabase.IsValidFolder(PREFS_PATH))
            AssetDatabase.CreateFolder("Assets", "Prefabs");
        if (!AssetDatabase.IsValidFolder("Assets/Scenes"))
            AssetDatabase.CreateFolder("Assets", "Scenes");
    }

    // ═══════════════════════════════════════════════
    // 材质
    // ═══════════════════════════════════════════════

    private static Material CreateMat(string name, Color color)
    {
        string path = MATS_PATH + "/" + name + ".mat";
        Material existing = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (existing != null) AssetDatabase.DeleteAsset(path);

        Material mat = new Material(Shader.Find("Standard"));
        mat.name = name;
        mat.color = color;
        AssetDatabase.CreateAsset(mat, path);
        return mat;
    }

    // ═══════════════════════════════════════════════
    // 圆盘预制体
    // ═══════════════════════════════════════════════

    private static GameObject[] CreateDiskPrefabs(Material[] mats)
    {
        GameObject[] prefs = new GameObject[3];
        for (int i = 0; i < 3; i++)
        {
            string p = PREFS_PATH + "/" + DISK_DEFS[i].name + ".prefab";
            if (AssetDatabase.LoadAssetAtPath<GameObject>(p) != null)
                AssetDatabase.DeleteAsset(p);

            GameObject obj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            obj.name = DISK_DEFS[i].name;
            obj.transform.localScale = DISK_DEFS[i].scale;
            obj.GetComponent<Renderer>().material = mats[i];

            chh_Disk d = obj.AddComponent<chh_Disk>();
            d.sizeLevel = DISK_DEFS[i].level;

            prefs[i] = PrefabUtility.SaveAsPrefabAsset(obj, p);
            Object.DestroyImmediate(obj);
        }
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        return prefs;
    }

    // ═══════════════════════════════════════════════
    // 底座
    // ═══════════════════════════════════════════════

    private static void CreateBase(Material mat)
    {
        GameObject obj = GameObject.CreatePrimitive(PrimitiveType.Cube);
        obj.name = "chh_Base";
        obj.transform.position = BASE_POS;
        obj.transform.localScale = BASE_SCALE;
        obj.GetComponent<Renderer>().material = mat;
    }

    // ═══════════════════════════════════════════════
    // 柱子
    // ═══════════════════════════════════════════════

    private static chh_Pillar[] CreatePillars(Material mat)
    {
        chh_Pillar[] result = new chh_Pillar[3];
        for (int i = 0; i < 3; i++)
        {
            GameObject obj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            obj.name = PILLAR_NAMES[i];
            obj.transform.position = PILLAR_POS[i];
            obj.transform.localScale = PILLAR_SCALE;
            obj.GetComponent<Renderer>().material = mat;

            chh_Pillar p = obj.AddComponent<chh_Pillar>();
            p.pillarIndex = i;
            result[i] = p;
        }
        return result;
    }

    // ═══════════════════════════════════════════════
    // GameManager（用 SerializedObject 设置引用）
    // ═══════════════════════════════════════════════

    private static GameObject CreateGameManager(chh_Pillar[] pillars, GameObject[] diskPrefabs)
    {
        GameObject existing = GameObject.Find("chh_GameManager");
        if (existing != null) Object.DestroyImmediate(existing);

        GameObject gmObj = new GameObject("chh_GameManager");
        chh_GameManager gm = gmObj.AddComponent<chh_GameManager>();

        SerializedObject so = new SerializedObject(gm);

        // 柱子数组
        var pp = so.FindProperty("pillars");
        pp.arraySize = 3;
        for (int i = 0; i < 3; i++)
            pp.GetArrayElementAtIndex(i).objectReferenceValue = pillars[i];

        // 预制体数组
        var dp = so.FindProperty("diskPrefabs");
        dp.arraySize = 3;
        for (int i = 0; i < 3; i++)
            dp.GetArrayElementAtIndex(i).objectReferenceValue = diskPrefabs[i];

        so.ApplyModifiedProperties();
        return gmObj;
    }

    // ═══════════════════════════════════════════════
    // Canvas + UI
    // ═══════════════════════════════════════════════

    private static void CreateUI(GameObject gmObj)
    {
        chh_GameManager gm = gmObj.GetComponent<chh_GameManager>();

        GameObject old = GameObject.Find("Canvas");
        if (old != null) Object.DestroyImmediate(old);

        // Canvas
        GameObject canvasObj = new GameObject("Canvas");
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvasObj.AddComponent<CanvasScaler>();
        canvasObj.AddComponent<GraphicRaycaster>();

        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        // WinText — 胜利提示（金色居中大字）
        GameObject wtObj = new GameObject("WinText");
        wtObj.transform.SetParent(canvasObj.transform, false);
        Text winText = wtObj.AddComponent<Text>();
        winText.font = font;
        winText.fontSize = 36;
        winText.alignment = TextAnchor.MiddleCenter;
        winText.color = new Color(1f, 0.85f, 0f);
        winText.text = "";
        RectTransform wr = wtObj.GetComponent<RectTransform>();
        wr.anchorMin = wr.anchorMax = new Vector2(0.5f, 0.5f);
        wr.anchoredPosition = new Vector2(0, 150);
        wr.sizeDelta = new Vector2(400, 80);

        // StepText — 步数显示（左上角白色）
        GameObject stObj = new GameObject("StepText");
        stObj.transform.SetParent(canvasObj.transform, false);
        Text stepText = stObj.AddComponent<Text>();
        stepText.font = font;
        stepText.fontSize = 24;
        stepText.alignment = TextAnchor.MiddleLeft;
        stepText.color = Color.white;
        stepText.text = "步数: 0";
        RectTransform sr = stObj.GetComponent<RectTransform>();
        sr.anchorMin = sr.anchorMax = sr.pivot = new Vector2(0, 1);
        sr.anchoredPosition = new Vector2(20, -20);
        sr.sizeDelta = new Vector2(200, 40);

        // ResetButton — 重置按钮（右上角）
        GameObject btnObj = new GameObject("ResetButton");
        btnObj.transform.SetParent(canvasObj.transform, false);
        Image btnImg = btnObj.AddComponent<Image>();
        btnImg.color = new Color(0.3f, 0.3f, 0.3f);
        Button btn = btnObj.AddComponent<Button>();
        RectTransform br = btnObj.GetComponent<RectTransform>();
        br.anchorMin = br.anchorMax = br.pivot = new Vector2(1, 1);
        br.anchoredPosition = new Vector2(-20, -20);
        br.sizeDelta = new Vector2(120, 40);

        // 按钮文字
        GameObject btObj = new GameObject("Text");
        btObj.transform.SetParent(btnObj.transform, false);
        Text btnText = btObj.AddComponent<Text>();
        btnText.font = font;
        btnText.fontSize = 20;
        btnText.alignment = TextAnchor.MiddleCenter;
        btnText.color = Color.white;
        btnText.text = "重置";
        btObj.GetComponent<RectTransform>().anchorMin = Vector2.zero;
        btObj.GetComponent<RectTransform>().anchorMax = Vector2.one;
        btObj.GetComponent<RectTransform>().sizeDelta = Vector2.zero;

        // 绑定按钮点击 → GameManager.chh_ResetGame()
        // 必须用 UnityEventTools.AddVoidPersistentListener 才能持久化到场景文件
        UnityEditor.Events.UnityEventTools.AddVoidPersistentListener(btn.onClick, gm.chh_ResetGame);

        // 通过 SerializedObject 设置 UI 引用
        SerializedObject so = new SerializedObject(gm);
        so.FindProperty("winText").objectReferenceValue = winText;
        so.FindProperty("stepText").objectReferenceValue = stepText;
        so.ApplyModifiedProperties();

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
    }

    // ═══════════════════════════════════════════════
    // 摄像机
    // ═══════════════════════════════════════════════

    private static void SetupCamera()
    {
        Camera cam = Camera.main;
        if (cam == null)
        {
            GameObject camObj = new GameObject("Main Camera");
            cam = camObj.AddComponent<Camera>();
            camObj.tag = "MainCamera";
        }
        cam.transform.position = new Vector3(0, 5, -10);
        cam.transform.rotation = Quaternion.Euler(15, 0, 0);
        cam.clearFlags = CameraClearFlags.Skybox;
    }
}

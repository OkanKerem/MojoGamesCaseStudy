#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public static class PuzzleSetupEditor
{
    private const string LevelFolder = "Assets/Data/Levels";
    private const string UnitTypesFolder = "Assets/Data/UnitTypes";
    private const string PrefabFolder = "Assets/Prefabs";
    private const string ScenePath = "Assets/Scenes/SampleScene.unity";

    private struct UnitTypeLibrary
    {
        public UnitTypeData Red;
        public UnitTypeData Blue;
        public UnitTypeData Green;
        public UnitTypeData Yellow;
        public UnitTypeData Purple;
        public UnitTypeData Orange;
    }

    [MenuItem("Puzzle/Setup/Create Unit Types")]
    public static void CreateUnitTypesMenu()
    {
        CreateUnitTypes();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("Unit types created in Assets/Data/UnitTypes");
    }

    [MenuItem("Puzzle/Setup/Bake Unit Pool (16)")]
    public static void BakeUnitPool()
    {
        Unit unitPrefab = CreateUnitPrefab();
        UnitPool unitPool = Object.FindFirstObjectByType<UnitPool>();
        if (unitPool == null)
        {
            Debug.LogError("Bake Unit Pool: No UnitPool found in the open scene. Run Build Gameplay Scene first.");
            return;
        }

        Transform poolRoot = unitPool.transform.Find("PoolRoot");
        if (poolRoot == null)
        {
            GameObject poolRootObject = new GameObject("PoolRoot");
            poolRootObject.transform.SetParent(unitPool.transform, false);
            poolRoot = poolRootObject.transform;
        }

        SetSerializedField(unitPool, "poolRoot", poolRoot);
        SetSerializedField(unitPool, "unitPrefab", unitPrefab);
        SetSerializedField(unitPool, "defaultCapacity", 16);
        SetSerializedField(unitPool, "collectSceneUnits", true);
        SetSerializedField(unitPool, "sceneInstancesOnly", true);
        SetSerializedField(unitPool, "allowRuntimeExpand", false);
        SetSerializedField(unitPool, "sceneUnitsParent", poolRoot);

        int existing = poolRoot.GetComponentsInChildren<Unit>(true).Length;
        int toCreate = Mathf.Max(0, 16 - existing);
        for (int i = 0; i < toCreate; i++)
        {
            Unit unit = (Unit)PrefabUtility.InstantiatePrefab(unitPrefab, poolRoot);
            unit.gameObject.SetActive(false);
            unit.name = $"Unit_{existing + i + 1}";
        }

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Debug.Log($"Unit pool baked under PoolRoot ({existing + toCreate} total units).");
    }

    [MenuItem("Puzzle/Setup/Create Sample Levels")]
    public static void CreateSampleLevels()
    {
        EnsureFolder("Assets/Data");
        EnsureFolder(LevelFolder);
        UnitTypeLibrary types = CreateUnitTypes();

        CreateLevelAsset("Level_01", 1, 5, 5, BuildLevel01Cells(types));
        CreateLevelAsset("Level_02", 2, 5, 5, BuildLevel02Cells(types));
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        LevelData level01 = AssetDatabase.LoadAssetAtPath<LevelData>($"{LevelFolder}/Level_01.asset");
        LevelData level02 = AssetDatabase.LoadAssetAtPath<LevelData>($"{LevelFolder}/Level_02.asset");
        LogValidation(level01);
        LogValidation(level02);
        Debug.Log("Sample levels created in Assets/Data/Levels");
    }

    [MenuItem("Puzzle/Setup/Build Gameplay Scene")]
    public static void BuildGameplayScene()
    {
        CreateSampleLevels();
        Unit unitPrefab = CreateUnitPrefab();
        LevelData level01 = AssetDatabase.LoadAssetAtPath<LevelData>($"{LevelFolder}/Level_01.asset");
        LevelData level02 = AssetDatabase.LoadAssetAtPath<LevelData>($"{LevelFolder}/Level_02.asset");

        var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

        ClearGameplayObjects();

        GameObject gameSystems = new GameObject("GameSystems");
        GameManager gameManager = gameSystems.AddComponent<GameManager>();
        LevelManager levelManager = gameSystems.AddComponent<LevelManager>();
        GridManager gridManager = gameSystems.AddComponent<GridManager>();
        SlotManager slotManager = gameSystems.AddComponent<SlotManager>();
        UnitPool unitPool = gameSystems.AddComponent<UnitPool>();

        GameObject unitRoot = new GameObject("UnitRoot");
        unitRoot.transform.SetParent(gameSystems.transform, false);

        GameObject tileRoot = new GameObject("TileRoot");
        tileRoot.transform.SetParent(gameSystems.transform, false);

        GameObject barrierRoot = new GameObject("BarrierRoot");
        barrierRoot.transform.SetParent(gameSystems.transform, false);

        GameObject tilePrefab = CreateTilePrefab();
        GameObject barrierPrefab = CreateBarrierPrefab();

        GameObject slotBar = new GameObject("SlotBar");
        slotBar.transform.SetParent(gameSystems.transform, false);
        slotBar.transform.localPosition = new Vector3(0f, 0f, -4.5f);

        Transform[] slotAnchors = new Transform[SlotManager.SlotCount];
        for (int i = 0; i < SlotManager.SlotCount; i++)
        {
            GameObject anchor = new GameObject($"Slot_{i + 1}");
            anchor.transform.SetParent(slotBar.transform, false);
            anchor.transform.localPosition = new Vector3((i - 3f) * 1.1f, 0f, 0f);
            slotAnchors[i] = anchor.transform;
        }

        GameObject poolRoot = new GameObject("PoolRoot");
        poolRoot.transform.SetParent(gameSystems.transform, false);

        SetSerializedField(gridManager, "cellSize", 1.1f);
        SetSerializedField(gridManager, "gridOrigin", new Vector3(-2.2f, 0f, -2.2f));

        SetSerializedField(unitPool, "unitPrefab", unitPrefab);
        SetSerializedField(unitPool, "defaultCapacity", 16);
        SetSerializedField(unitPool, "collectSceneUnits", true);
        SetSerializedField(unitPool, "sceneInstancesOnly", true);
        SetSerializedField(unitPool, "allowRuntimeExpand", false);
        SetSerializedField(unitPool, "sceneUnitsParent", poolRoot.transform);
        SetSerializedField(unitPool, "poolRoot", poolRoot.transform);

        SetSerializedField(slotManager, "_slotAnchors", slotAnchors);

        SetSerializedField(levelManager, "_gridManager", gridManager);
        SetSerializedField(levelManager, "_unitPool", unitPool);
        SetSerializedField(levelManager, "_unitRoot", unitRoot.transform);
        SetSerializedField(levelManager, "_tileRoot", tileRoot.transform);
        SetSerializedField(levelManager, "_barrierRoot", barrierRoot.transform);
        SetSerializedField(levelManager, "_tilePrefab", tilePrefab);
        SetSerializedField(levelManager, "_barrierPrefab", barrierPrefab);

        Canvas canvas = Object.FindFirstObjectByType<Canvas>();
        if (canvas == null)
        {
            GameObject canvasObject = new GameObject("UI");
            canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObject.AddComponent<CanvasScaler>();
            canvasObject.AddComponent<GraphicRaycaster>();
        }

        UIManager uiManager = canvas.GetComponent<UIManager>();
        if (uiManager == null)
        {
            uiManager = canvas.gameObject.AddComponent<UIManager>();
        }

        TMP_Text levelText = CreateLevelTmpText(canvas.transform, "LevelText", new Vector2(0f, 220f), "Level 1", 28);
        GameObject winPanel = CreatePanel(canvas.transform, "WinPanel", "You Win!");
        GameObject failPanel = CreatePanel(canvas.transform, "FailPanel", "Level Failed");
        Button continueButton = CreateButton(winPanel.transform, "ContinueButton", "Continue", new Vector2(0f, -40f));
        Button retryButtonWin = CreateButton(winPanel.transform, "RetryButton", "Retry", new Vector2(0f, -90f));
        Button retryButtonFail = CreateButton(failPanel.transform, "RetryButton", "Retry", new Vector2(0f, -40f));

        winPanel.SetActive(false);
        failPanel.SetActive(false);

        SetSerializedField(uiManager, "_levelText", levelText);
        SetSerializedField(uiManager, "_winPanel", winPanel);
        SetSerializedField(uiManager, "_failPanel", failPanel);
        SetSerializedField(uiManager, "_continueButton", continueButton);
        SetSerializedField(uiManager, "_retryButton", retryButtonFail);
        SetSerializedField(uiManager, "_winRetryButton", retryButtonWin);

        SetSerializedField(gameManager, "_levelManager", levelManager);
        SetSerializedField(gameManager, "_slotManager", slotManager);
        SetSerializedField(gameManager, "_unitPool", unitPool);
        LevelCatalogConfig levelCatalog = GetOrCreateLevelCatalog(new[] { level01, level02 });
        SetSerializedField(gameManager, "_levelCatalog", levelCatalog);

        if (Object.FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
        {
            new GameObject("EventSystem", typeof(UnityEngine.EventSystems.EventSystem),
                typeof(UnityEngine.EventSystems.StandaloneInputModule));
        }

        Camera camera = Camera.main;
        if (camera != null)
        {
            camera.transform.position = new Vector3(2.2f, 9f, -3f);
            camera.transform.rotation = Quaternion.Euler(55f, -25f, 0f);
        }

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();

        Debug.Log("Gameplay scene setup complete.");
    }

    private static UnitTypeLibrary CreateUnitTypes()
    {
        EnsureFolder("Assets/Data");
        EnsureFolder(UnitTypesFolder);

        return new UnitTypeLibrary
        {
            Red = CreateOrLoadUnitType("red", "Red", new Color(0.9f, 0.2f, 0.2f)),
            Blue = CreateOrLoadUnitType("blue", "Blue", new Color(0.2f, 0.45f, 0.95f)),
            Green = CreateOrLoadUnitType("green", "Green", new Color(0.2f, 0.85f, 0.35f)),
            Yellow = CreateOrLoadUnitType("yellow", "Yellow", new Color(0.95f, 0.85f, 0.2f)),
            Purple = CreateOrLoadUnitType("purple", "Purple", new Color(0.7f, 0.3f, 0.9f)),
            Orange = CreateOrLoadUnitType("orange", "Orange", new Color(0.95f, 0.5f, 0.15f))
        };
    }

    private static UnitTypeData CreateOrLoadUnitType(string id, string displayName, Color trailColor)
    {
        string path = $"{UnitTypesFolder}/{id}.asset";
        UnitTypeData existing = AssetDatabase.LoadAssetAtPath<UnitTypeData>(path);
        if (existing != null)
        {
            ApplyUnitTypeFields(existing, id, displayName, trailColor);
            EditorUtility.SetDirty(existing);
            return existing;
        }

        UnitTypeData data = ScriptableObject.CreateInstance<UnitTypeData>();
        ApplyUnitTypeFields(data, id, displayName, trailColor);
        AssetDatabase.CreateAsset(data, path);
        return data;
    }

    private static void ApplyUnitTypeFields(UnitTypeData data, string id, string displayName, Color trailColor)
    {
        SerializedObject serializedObject = new SerializedObject(data);
        serializedObject.FindProperty("id").stringValue = id;
        serializedObject.FindProperty("displayName").stringValue = displayName;
        serializedObject.FindProperty("trailColor").colorValue = trailColor;
        serializedObject.ApplyModifiedPropertiesWithoutUndo();
    }

    private static List<CellData> BuildLevel01Cells(UnitTypeLibrary types)
    {
        return new List<CellData>
        {
            Cell(0, 0, types.Red),
            Cell(1, 0, types.Red),
            Cell(2, 0, types.Red),
            Cell(3, 0, types.Blue),
            Cell(4, 0, types.Blue),
            Cell(0, 1, types.Blue),
            Cell(1, 1, types.Green),
            Cell(2, 1, types.Green),
            Cell(3, 1, types.Green),
            Cell(4, 1, types.Yellow),
            Cell(0, 2, types.Yellow),
            Cell(1, 2, types.Yellow),
            Cell(2, 2, types.Purple),
            Cell(3, 2, types.Purple),
            Cell(4, 2, types.Purple),
            Cell(1, 3, types.Orange),
            Cell(2, 3, types.Orange),
            Cell(3, 3, types.Orange),
            BarrierCell(2, 4)
        };
    }

    private static List<CellData> BuildLevel02Cells(UnitTypeLibrary types)
    {
        return new List<CellData>
        {
            Cell(0, 0, types.Blue),
            Cell(1, 0, types.Blue),
            Cell(2, 0, types.Blue),
            Cell(3, 0, types.Green),
            Cell(4, 0, types.Green),
            Cell(0, 1, types.Green),
            Cell(1, 1, types.Yellow),
            Cell(2, 1, types.Yellow),
            Cell(3, 1, types.Yellow),
            Cell(4, 1, types.Red),
            Cell(0, 2, types.Red),
            Cell(1, 2, types.Red),
            Cell(2, 2, types.Purple),
            Cell(3, 2, types.Purple),
            Cell(4, 2, types.Purple),
            Cell(1, 3, types.Orange),
            Cell(2, 3, types.Orange),
            Cell(3, 3, types.Orange),
            BarrierCell(0, 4),
            BarrierCell(4, 4)
        };
    }

    private static CellData Cell(int x, int y, UnitTypeData unitType)
    {
        return new CellData
        {
            x = x,
            y = y,
            hasTile = true,
            hasUnit = true,
            unitType = unitType
        };
    }

    private static CellData BarrierCell(int x, int y)
    {
        return new CellData
        {
            x = x,
            y = y,
            hasTile = true,
            hasBarrier = true
        };
    }

    private static void CreateLevelAsset(string name, int levelNumber, int width, int height, List<CellData> cells)
    {
        string path = $"{LevelFolder}/{name}.asset";
        LevelData existing = AssetDatabase.LoadAssetAtPath<LevelData>(path);
        if (existing != null)
        {
            ApplyLevelSettings(existing, levelNumber, width, height, cells);
            EditorUtility.SetDirty(existing);
            return;
        }

        LevelData level = ScriptableObject.CreateInstance<LevelData>();
        ApplyLevelSettings(level, levelNumber, width, height, cells);
        AssetDatabase.CreateAsset(level, path);
    }

    private static void LogValidation(LevelData level)
    {
        if (level == null)
        {
            return;
        }

        LevelValidator.ValidationResult result = LevelValidator.Validate(level);
        if (result.IsValid)
        {
            Debug.Log($"Level '{level.name}' is valid.");
        }
        else
        {
            Debug.LogError($"Level '{level.name}' validation failed:\n{result.GetMessage()}");
        }
    }

    private static void ApplyLevelSettings(LevelData level, int levelNumber, int width, int height, List<CellData> cells)
    {
        level.levelNumber = levelNumber;
        level.cells = cells;
        for (int i = 0; i < cells.Count; i++)
        {
            cells[i].hasTile = true;
        }

        level.RecalculateBounds();
    }

    private static GameObject CreateTilePrefab()
    {
        EnsureFolder(PrefabFolder);
        string prefabPath = $"{PrefabFolder}/Tile.prefab";
        GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        if (existing != null)
        {
            return existing;
        }

        GameObject tileObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
        tileObject.name = "Tile";
        Object.DestroyImmediate(tileObject.GetComponent<Collider>());
        tileObject.AddComponent<TileView>();
        tileObject.transform.localScale = new Vector3(0.95f, 0.15f, 0.95f);
        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(tileObject, prefabPath);
        Object.DestroyImmediate(tileObject);
        return prefab;
    }

    private static GameObject CreateBarrierPrefab()
    {
        EnsureFolder(PrefabFolder);
        string prefabPath = $"{PrefabFolder}/Barrier.prefab";
        GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        if (existing != null)
        {
            return existing;
        }

        GameObject barrierObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
        barrierObject.name = "Barrier";
        Object.DestroyImmediate(barrierObject.GetComponent<Collider>());
        barrierObject.AddComponent<BarrierView>();
        barrierObject.transform.localScale = Vector3.one;
        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(barrierObject, prefabPath);
        Object.DestroyImmediate(barrierObject);
        return prefab;
    }

    private static Unit CreateUnitPrefab()
    {
        EnsureFolder(PrefabFolder);
        string prefabPath = $"{PrefabFolder}/Unit.prefab";

        Unit existing = AssetDatabase.LoadAssetAtPath<Unit>(prefabPath);
        if (existing != null)
        {
            return existing;
        }

        GameObject unitObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
        unitObject.name = "Unit";
        unitObject.transform.localScale = Vector3.one * 0.9f;

        Object.DestroyImmediate(unitObject.GetComponent<Rigidbody>());

        Unit unit = unitObject.AddComponent<Unit>();
        SetSerializedField(unit, "meshRenderer", unitObject.GetComponent<Renderer>());

        Unit prefab = PrefabUtility.SaveAsPrefabAsset(unitObject, prefabPath).GetComponent<Unit>();
        Object.DestroyImmediate(unitObject);
        return prefab;
    }

    private static void ClearGameplayObjects()
    {
        GameObject existingSystems = GameObject.Find("GameSystems");
        if (existingSystems != null)
        {
            Object.DestroyImmediate(existingSystems);
        }
    }

    private static TMP_Text CreateLevelTmpText(Transform parent, string name, Vector2 anchoredPosition, string content, int fontSize)
    {
        GameObject textObject = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        textObject.transform.SetParent(parent, false);
        TMP_Text text = textObject.GetComponent<TextMeshProUGUI>();
        text.text = content;
        text.fontSize = fontSize;
        text.alignment = TextAlignmentOptions.Center;
        text.color = Color.white;

        RectTransform rect = textObject.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(400f, 60f);
        rect.anchoredPosition = anchoredPosition;
        return text;
    }

    private static Text CreateUiText(Transform parent, string name, Vector2 anchoredPosition, string content, int fontSize)
    {
        GameObject textObject = new GameObject(name, typeof(RectTransform), typeof(Text));
        textObject.transform.SetParent(parent, false);
        Text text = textObject.GetComponent<Text>();
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.text = content;
        text.fontSize = fontSize;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = Color.white;

        RectTransform rect = textObject.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(400f, 60f);
        rect.anchoredPosition = anchoredPosition;
        return text;
    }

    private static GameObject CreatePanel(Transform parent, string name, string title)
    {
        GameObject panel = new GameObject(name, typeof(RectTransform), typeof(Image));
        panel.transform.SetParent(parent, false);
        Image image = panel.GetComponent<Image>();
        image.color = new Color(0f, 0f, 0f, 0.75f);

        RectTransform panelRect = panel.GetComponent<RectTransform>();
        panelRect.sizeDelta = new Vector2(320f, 180f);
        panelRect.anchoredPosition = Vector2.zero;

        CreateUiText(panel.transform, "Title", new Vector2(0f, 30f), title, 24);
        return panel;
    }

    private static Button CreateButton(Transform parent, string name, string label, Vector2 anchoredPosition)
    {
        GameObject buttonObject = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        buttonObject.transform.SetParent(parent, false);

        Image image = buttonObject.GetComponent<Image>();
        image.color = new Color(0.2f, 0.55f, 0.95f, 1f);

        RectTransform rect = buttonObject.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(180f, 40f);
        rect.anchoredPosition = anchoredPosition;

        Text text = CreateUiText(buttonObject.transform, "Label", Vector2.zero, label, 18);
        RectTransform textRect = text.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        return buttonObject.GetComponent<Button>();
    }

    private static LevelCatalogConfig GetOrCreateLevelCatalog(LevelData[] levels)
    {
        const string catalogPath = "Assets/Data/Levels/LevelCatalog.asset";
        LevelCatalogConfig catalog = AssetDatabase.LoadAssetAtPath<LevelCatalogConfig>(catalogPath);
        if (catalog == null)
        {
            catalog = ScriptableObject.CreateInstance<LevelCatalogConfig>();
            AssetDatabase.CreateAsset(catalog, catalogPath);
        }

        SerializedObject serializedCatalog = new SerializedObject(catalog);
        SerializedProperty entriesProperty = serializedCatalog.FindProperty("_entries");
        entriesProperty.ClearArray();

        for (int i = 0; i < levels.Length; i++)
        {
            if (levels[i] == null)
            {
                continue;
            }

            entriesProperty.InsertArrayElementAtIndex(entriesProperty.arraySize);
            SerializedProperty element = entriesProperty.GetArrayElementAtIndex(entriesProperty.arraySize - 1);
            element.FindPropertyRelative("levelNumber").intValue = i + 1;
            element.FindPropertyRelative("levelData").objectReferenceValue = levels[i];
            element.FindPropertyRelative("displayTextOverride").stringValue = string.Empty;
            levels[i].levelNumber = i + 1;
            EditorUtility.SetDirty(levels[i]);
        }

        serializedCatalog.ApplyModifiedPropertiesWithoutUndo();
        catalog.ApplyAutoNumbering();
        EditorUtility.SetDirty(catalog);
        return catalog;
    }

    private static void EnsureFolder(string path)
    {
        if (!AssetDatabase.IsValidFolder(path))
        {
            string parent = System.IO.Path.GetDirectoryName(path)?.Replace('\\', '/');
            string folderName = System.IO.Path.GetFileName(path);
            if (!string.IsNullOrEmpty(parent) && !AssetDatabase.IsValidFolder(parent))
            {
                EnsureFolder(parent);
            }

            AssetDatabase.CreateFolder(parent, folderName);
        }
    }

    private static void SetSerializedField(Object target, string fieldName, object value)
    {
        SerializedObject serializedObject = new SerializedObject(target);
        SerializedProperty property = serializedObject.FindProperty(fieldName);
        if (property == null)
        {
            Debug.LogWarning($"Missing serialized field '{fieldName}' on {target.name}");
            return;
        }

        switch (value)
        {
            case int intValue:
                property.intValue = intValue;
                break;
            case float floatValue:
                property.floatValue = floatValue;
                break;
            case string stringValue:
                property.stringValue = stringValue;
                break;
            case bool boolValue:
                property.boolValue = boolValue;
                break;
            case Object objectValue:
                property.objectReferenceValue = objectValue;
                break;
            case Transform[] transforms:
                property.arraySize = transforms.Length;
                for (int i = 0; i < transforms.Length; i++)
                {
                    property.GetArrayElementAtIndex(i).objectReferenceValue = transforms[i];
                }
                break;
            case LevelData[] levels:
                property.arraySize = levels.Length;
                for (int i = 0; i < levels.Length; i++)
                {
                    property.GetArrayElementAtIndex(i).objectReferenceValue = levels[i];
                }
                break;
            case Vector3 vector3Value:
                property.vector3Value = vector3Value;
                break;
            default:
                Debug.LogWarning($"Unsupported serialized field type for '{fieldName}'");
                break;
        }

        serializedObject.ApplyModifiedProperties();
    }
}
#endif

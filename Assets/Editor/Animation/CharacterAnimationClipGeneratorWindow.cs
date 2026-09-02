using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

public sealed class CharacterAnimationClipGeneratorWindow : EditorWindow
{
    private const string DefaultOutputFolder = "Assets/_Project/Animations/Characters";

    private static readonly string[] IdleSearchAliases = { "idle" };
    private static readonly string[] WalkSearchAliases = { "walk", "run", "move", "fly" };
    private static readonly string[] AttackSearchAliases = { "attack", "strike" };
    private static readonly string[] HitSearchAliases = { "hit", "hurt", "damage" };
    private static readonly string[] DeathSearchAliases = { "death", "die" };

    [SerializeField] private AnimatorOverrideController _overrideController;
    [SerializeField] private string _characterName = string.Empty;
    [SerializeField] private int _sampleRate = 12;
    [SerializeField] private DefaultAsset _outputFolder;
    [SerializeField] private CharacterAnimationDirection[] _directionOrder =
    {
        CharacterAnimationDirection.Bottom,
        CharacterAnimationDirection.Left,
        CharacterAnimationDirection.Right,
        CharacterAnimationDirection.Top
    };
    [SerializeField] private List<CharacterActionSheetSettings> _actionSettings =
        new List<CharacterActionSheetSettings>();

    private Vector2 _scrollPosition;
    private CharacterControllerScanResult _scanResult;
    private AnimatorOverrideController _scannedOverrideController;
    private List<CharacterGeneratorMessage> _validationMessages =
        new List<CharacterGeneratorMessage>();

    [MenuItem("Tools/Animation/Character Animation Clip Generator")]
    public static void Open()
    {
        CharacterAnimationClipGeneratorWindow window =
            GetWindow<CharacterAnimationClipGeneratorWindow>();
        window.titleContent = new GUIContent("Character Animations");
        window.minSize = new Vector2(560f, 650f);
        window.Show();
    }

    private void OnEnable()
    {
        titleContent = new GUIContent("Character Animations");
        minSize = new Vector2(560f, 650f);

        EnsureSerializedState();

        if (Selection.activeObject is AnimatorOverrideController selectedOverride &&
            selectedOverride != _overrideController)
        {
            _overrideController = selectedOverride;
            _characterName = selectedOverride.name;
            ApplyControllerAssetDefaults(selectedOverride);
        }

        RefreshControllerScan();
    }

    private void OnSelectionChange()
    {
        if (Selection.activeObject is AnimatorOverrideController selectedOverride &&
            selectedOverride != _overrideController)
        {
            _overrideController = selectedOverride;
            _characterName = selectedOverride.name;
            ApplyControllerAssetDefaults(selectedOverride);
            RefreshControllerScan();
            ClearValidation();
            Repaint();
        }
    }

    private void OnGUI()
    {
        EnsureSerializedState();

        EditorGUILayout.Space(8f);
        EditorGUILayout.LabelField(
            "Character Animation Clip Generator",
            new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 17
            });
        EditorGUILayout.Space(3f);
        EditorGUILayout.HelpBox(
            "Create or update frame-by-frame clips and assign them directly to an Animator Override Controller. " +
            "The Base Animator Controller is read-only and is never modified.",
            MessageType.Info);

        _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);
        EditorGUI.BeginChangeCheck();

        DrawControllerSection();
        EditorGUILayout.Space(8f);
        DrawDirectionSection();
        EditorGUILayout.Space(8f);
        DrawActionSheetsSection();
        EditorGUILayout.Space(8f);
        DrawOutputSection();

        bool fieldsChanged = EditorGUI.EndChangeCheck();
        if (fieldsChanged)
        {
            ClearValidation();
        }

        EditorGUILayout.Space(10f);
        DrawValidationMessages();
        EditorGUILayout.Space(8f);
        DrawActionButtons();
        EditorGUILayout.Space(12f);
        EditorGUILayout.EndScrollView();
    }

    private void DrawControllerSection()
    {
        EditorGUILayout.LabelField("Controller & Character", EditorStyles.boldLabel);
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);

        AnimatorOverrideController previousController = _overrideController;
        AnimatorOverrideController selectedController =
            (AnimatorOverrideController)EditorGUILayout.ObjectField(
                new GUIContent(
                    "Override Controller",
                    "The AnimatorOverrideController asset that will be updated."),
                _overrideController,
                typeof(AnimatorOverrideController),
                false);

        if (selectedController != previousController)
        {
            bool shouldAdoptControllerName =
                string.IsNullOrWhiteSpace(_characterName) ||
                (previousController != null &&
                 string.Equals(_characterName, previousController.name, StringComparison.Ordinal));

            _overrideController = selectedController;
            if (shouldAdoptControllerName && selectedController != null)
            {
                _characterName = selectedController.name;
            }

            ApplyControllerAssetDefaults(selectedController);
            RefreshControllerScan();
            GUI.changed = true;
        }

        _characterName = EditorGUILayout.TextField(
            new GUIContent(
                "Character Name",
                "Used to create CharacterName_Action_Direction.anim asset names."),
            _characterName);

        _sampleRate = EditorGUILayout.IntField(
            new GUIContent("Sample Rate", "Animation frames per second."),
            _sampleRate);

        using (new EditorGUI.DisabledScope(true))
        {
            EditorGUILayout.ObjectField(
                "Base Controller",
                _scanResult?.BaseController,
                typeof(AnimatorController),
                false);
        }

        DrawControllerDiscovery();
        EditorGUILayout.EndVertical();
    }

    private void DrawControllerDiscovery()
    {
        if (_overrideController != _scannedOverrideController || _scanResult == null)
        {
            RefreshControllerScan();
        }

        if (_overrideController == null)
        {
            EditorGUILayout.HelpBox(
                "Select an Override Controller to scan its states, Blend Trees, and placeholder clips.",
                MessageType.None);
            return;
        }

        if (_scanResult.Messages.Any(message =>
                message.Type == CharacterGeneratorMessageType.Error))
        {
            foreach (CharacterGeneratorMessage message in _scanResult.Messages)
            {
                EditorGUILayout.HelpBox(message.Text, ToUnityMessageType(message.Type));
            }

            return;
        }

        EditorGUILayout.Space(4f);
        EditorGUILayout.LabelField("Actions found in the Base Controller", EditorStyles.miniBoldLabel);

        foreach (CharacterAnimationAction action in CharacterAnimationNaming.Actions)
        {
            CharacterDiscoveredAction discovered = _scanResult.Actions[action];
            string actionName = CharacterAnimationNaming.GetActionDisplayName(action);
            string layout;

            switch (discovered.Layout)
            {
                case CharacterActionLayout.FourDirections:
                    layout = discovered.UsesBlendTree
                        ? $"Blend Tree - 4 directions - {discovered.PlaceholderCount} placeholders"
                        : $"4 direct clips - {discovered.PlaceholderCount} placeholders";
                    break;
                default:
                    layout = "Invalid";
                    break;
            }

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(actionName, GUILayout.Width(70f));
            EditorGUILayout.LabelField(layout, EditorStyles.miniLabel);
            EditorGUILayout.EndHorizontal();

            if (discovered.Layout == CharacterActionLayout.Invalid)
            {
                foreach (string issue in discovered.Issues)
                {
                    EditorGUILayout.HelpBox(actionName + ": " + issue, MessageType.Error);
                }
            }
        }
    }

    private void DrawDirectionSection()
    {
        EditorGUILayout.LabelField("Spritesheet Direction Order", EditorStyles.boldLabel);
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.HelpBox(
            "Sprite groups or rows are read from top to bottom by position. " +
            "Frames inside each group are always ordered from left to right.",
            MessageType.None);

        string[] rowLabels =
        {
            "Group 1 (top)",
            "Group 2",
            "Group 3",
            "Group 4 (bottom)"
        };

        for (int index = 0; index < _directionOrder.Length; index++)
        {
            _directionOrder[index] =
                (CharacterAnimationDirection)EditorGUILayout.EnumPopup(
                    rowLabels[index],
                    _directionOrder[index]);
        }

        if (_directionOrder.Distinct().Count() != CharacterAnimationNaming.Directions.Length)
        {
            EditorGUILayout.HelpBox(
                "Bottom, Left, Right, and Top must each appear exactly once.",
                MessageType.Error);
        }

        EditorGUILayout.EndVertical();
    }

    private void DrawActionSheetsSection()
    {
        EditorGUILayout.LabelField("Spritesheet theo action", EditorStyles.boldLabel);

        foreach (CharacterActionSheetSettings settings in _actionSettings)
        {
            DrawActionSheet(settings);
            EditorGUILayout.Space(3f);
        }
    }

    private void DrawActionSheet(CharacterActionSheetSettings settings)
    {
        string actionName = CharacterAnimationNaming.GetActionDisplayName(settings.Action);

        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.BeginHorizontal();
        settings.Expanded = EditorGUILayout.Foldout(
            settings.Expanded,
            actionName,
            true,
            EditorStyles.foldoutHeader);
        GUILayout.FlexibleSpace();
        EditorGUILayout.LabelField(
            CharacterAnimationNaming.ShouldLoop(settings.Action) ? "Loop: On" : "Loop: Off",
            EditorStyles.miniLabel,
            GUILayout.Width(65f));
        EditorGUILayout.EndHorizontal();

        if (!settings.Expanded)
        {
            EditorGUILayout.EndVertical();
            return;
        }

        settings.SpriteSheet = (Texture2D)EditorGUILayout.ObjectField(
            new GUIContent(
                "Spritesheet",
                "A sliced Sprite Multiple texture. The source file name is not used to identify the action."),
            settings.SpriteSheet,
            typeof(Texture2D),
            false);

        int spriteCount = CharacterAnimationClipGenerator.GetSpriteCount(settings.SpriteSheet);
        if (settings.SpriteSheet != null)
        {
            EditorGUILayout.LabelField(
                $"Detected: {spriteCount} Sprites",
                EditorStyles.miniLabel);
        }

        CharacterFrameSplitMode previousMode = settings.SplitMode;
        int selectedMode = GUILayout.Toolbar(
            (int)settings.SplitMode,
            new[] { "Auto Equal", "Manual Split" });
        settings.SplitMode = (CharacterFrameSplitMode)selectedMode;

        if (previousMode != settings.SplitMode &&
            settings.SplitMode == CharacterFrameSplitMode.ManualSplit &&
            spriteCount > 0 &&
            spriteCount % CharacterAnimationNaming.Directions.Length == 0)
        {
            int equalCount = spriteCount / CharacterAnimationNaming.Directions.Length;
            foreach (CharacterAnimationDirection direction in CharacterAnimationNaming.Directions)
            {
                settings.SetManualFrameCount(direction, equalCount);
            }
        }

        if (settings.SplitMode == CharacterFrameSplitMode.AutoEqual)
        {
            if (spriteCount > 0 && spriteCount % CharacterAnimationNaming.Directions.Length == 0)
            {
                EditorGUILayout.LabelField(
                    $"Result: {spriteCount / CharacterAnimationNaming.Directions.Length} frames per direction",
                    EditorStyles.miniLabel);
            }
            else if (settings.SpriteSheet != null)
            {
                EditorGUILayout.HelpBox(
                    $"{spriteCount} Sprites cannot be divided evenly by four.",
                    MessageType.Error);
            }
        }
        else
        {
            DrawManualFrameCounts(settings, spriteCount);
        }

        EditorGUILayout.EndVertical();
    }

    private static void DrawManualFrameCounts(
        CharacterActionSheetSettings settings,
        int spriteCount)
    {
        EditorGUILayout.Space(2f);
        EditorGUILayout.LabelField("Frame count for each direction", EditorStyles.miniBoldLabel);

        EditorGUILayout.BeginHorizontal();
        settings.BottomFrames = DrawCompactIntField("Bottom", settings.BottomFrames);
        settings.LeftFrames = DrawCompactIntField("Left", settings.LeftFrames);
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        settings.RightFrames = DrawCompactIntField("Right", settings.RightFrames);
        settings.TopFrames = DrawCompactIntField("Top", settings.TopFrames);
        EditorGUILayout.EndHorizontal();

        int total = settings.BottomFrames +
                    settings.LeftFrames +
                    settings.RightFrames +
                    settings.TopFrames;

        EditorGUILayout.LabelField(
            $"Manual Split total: {total} / {spriteCount} Sprites",
            EditorStyles.miniLabel);
    }

    private static int DrawCompactIntField(string label, int value)
    {
        EditorGUILayout.LabelField(label, GUILayout.Width(48f));
        int result = EditorGUILayout.IntField(value);
        GUILayout.Space(8f);
        return result;
    }

    private void DrawOutputSection()
    {
        EditorGUILayout.LabelField("Output", EditorStyles.boldLabel);
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);

        EditorGUILayout.BeginHorizontal();
        _outputFolder = (DefaultAsset)EditorGUILayout.ObjectField(
            new GUIContent("Clip Output Folder", "The folder must be inside Assets."),
            _outputFolder,
            typeof(DefaultAsset),
            false);

        if (GUILayout.Button("Browse...", GUILayout.Width(70f)))
        {
            ChooseOutputFolder();
            GUI.changed = true;
        }

        EditorGUILayout.EndHorizontal();

        string outputPath = AssetDatabase.GetAssetPath(_outputFolder);
        if (_outputFolder != null && !AssetDatabase.IsValidFolder(outputPath))
        {
            EditorGUILayout.HelpBox("The selected asset is not a folder.", MessageType.Error);
        }

        string safeName = CharacterAnimationClipGenerator.MakeSafeAssetName(_characterName);
        if (!string.IsNullOrEmpty(safeName))
        {
            EditorGUILayout.LabelField(
                $"Example: {safeName}_Idle_Bottom.anim",
                EditorStyles.miniLabel);
        }

        EditorGUILayout.EndVertical();
    }

    private void DrawValidationMessages()
    {
        if (_validationMessages == null || _validationMessages.Count == 0)
        {
            return;
        }

        EditorGUILayout.LabelField("Validation", EditorStyles.boldLabel);
        foreach (CharacterGeneratorMessage message in _validationMessages)
        {
            EditorGUILayout.HelpBox(message.Text, ToUnityMessageType(message.Type));
        }
    }

    private void DrawActionButtons()
    {
        EditorGUILayout.BeginHorizontal();

        if (GUILayout.Button("Validate", GUILayout.Height(34f)))
        {
            RunValidation();
        }

        if (GUILayout.Button("Generate", GUILayout.Height(34f)))
        {
            RunGenerate();
        }

        EditorGUILayout.EndHorizontal();
    }

    private void RunValidation()
    {
        RefreshControllerScan();
        CharacterAnimationGenerationPlan plan =
            CharacterAnimationClipGenerator.BuildPlan(CreateRequest());
        _validationMessages = plan.Messages.ToList();
        Repaint();
    }

    private void RunGenerate()
    {
        RefreshControllerScan();
        CharacterAnimationGenerationPlan plan =
            CharacterAnimationClipGenerator.BuildPlan(CreateRequest());
        _validationMessages = plan.Messages.ToList();

        if (plan.HasErrors)
        {
            EditorUtility.DisplayDialog(
                "Generation Blocked",
                "Validation errors remain. No clips or mappings were changed.",
                "Close");
            Repaint();
            return;
        }

        try
        {
            CharacterAnimationGenerationResult result =
                CharacterAnimationClipGenerator.Generate(plan);

            _validationMessages.Add(new CharacterGeneratorMessage(
                CharacterGeneratorMessageType.Info,
                $"Completed: created {result.CreatedClipCount} clips, updated {result.UpdatedClipCount} clips, " +
                $"and applied {result.AppliedOverrideCount} overrides."));

            EditorUtility.DisplayDialog(
                "Generation Complete",
                $"New clips: {result.CreatedClipCount}\n" +
                $"Updated clips: {result.UpdatedClipCount}\n" +
                $"Applied overrides: {result.AppliedOverrideCount}",
                "Close");

            Selection.activeObject = _overrideController;
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            _validationMessages.Add(new CharacterGeneratorMessage(
                CharacterGeneratorMessageType.Error,
                "Generation failed: " + exception.Message));

            EditorUtility.DisplayDialog(
                "Generation Failed",
                exception.Message,
                "Close");
        }

        Repaint();
    }

    private CharacterAnimationGenerationRequest CreateRequest()
    {
        return new CharacterAnimationGenerationRequest
        {
            OverrideController = _overrideController,
            CharacterName = _characterName,
            SampleRate = _sampleRate,
            OutputFolder = AssetDatabase.GetAssetPath(_outputFolder),
            DirectionOrder = _directionOrder.ToArray(),
            ActionSettings = _actionSettings.ToArray()
        };
    }

    private void ChooseOutputFolder()
    {
        string projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."))
            .Replace('\\', '/')
            .TrimEnd('/');

        string currentAssetPath = AssetDatabase.GetAssetPath(_outputFolder);
        string currentAbsolutePath = string.IsNullOrEmpty(currentAssetPath)
            ? Application.dataPath
            : Path.GetFullPath(Path.Combine(projectRoot, currentAssetPath)).Replace('\\', '/');

        string selectedPath = EditorUtility.OpenFolderPanel(
            "Select AnimationClip Output Folder",
            currentAbsolutePath,
            string.Empty);

        if (string.IsNullOrEmpty(selectedPath))
        {
            return;
        }

        selectedPath = Path.GetFullPath(selectedPath).Replace('\\', '/').TrimEnd('/');
        string assetsRoot = projectRoot + "/Assets";
        bool insideAssets =
            string.Equals(selectedPath, assetsRoot, StringComparison.OrdinalIgnoreCase) ||
            selectedPath.StartsWith(assetsRoot + "/", StringComparison.OrdinalIgnoreCase);

        if (!insideAssets)
        {
            EditorUtility.DisplayDialog(
                "Invalid Folder",
                "The output folder must be inside the project's Assets folder.",
                "Close");
            return;
        }

        string relativePath = "Assets" + selectedPath.Substring(assetsRoot.Length);
        AssetDatabase.Refresh();
        DefaultAsset folderAsset = AssetDatabase.LoadAssetAtPath<DefaultAsset>(relativePath);
        if (folderAsset == null || !AssetDatabase.IsValidFolder(relativePath))
        {
            EditorUtility.DisplayDialog(
                "Invalid Folder",
                "Unity could not resolve the selected folder.",
                "Close");
            return;
        }

        _outputFolder = folderAsset;
    }

    private void ApplyControllerAssetDefaults(AnimatorOverrideController controller)
    {
        foreach (CharacterActionSheetSettings settings in _actionSettings)
        {
            settings.SpriteSheet = null;
        }

        if (controller == null)
        {
            return;
        }

        string controllerPath = AssetDatabase.GetAssetPath(controller);
        string controllerFolderPath = Path.GetDirectoryName(controllerPath);
        if (controllerFolderPath != null)
        {
            controllerFolderPath = controllerFolderPath.Replace('\\', '/');
        }

        if (!string.IsNullOrEmpty(controllerFolderPath) && AssetDatabase.IsValidFolder(controllerFolderPath))
        {
            DefaultAsset controllerFolder = AssetDatabase.LoadAssetAtPath<DefaultAsset>(controllerFolderPath);
            if (controllerFolder != null)
            {
                _outputFolder = controllerFolder;
            }
        }

        string[] texturePaths = AssetDatabase.FindAssets("t:Texture2D", new[] { "Assets" })
            .Select(AssetDatabase.GUIDToAssetPath)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        foreach (CharacterActionSheetSettings settings in _actionSettings)
        {
            settings.SpriteSheet = FindBestSpriteSheet(controller.name, settings.Action, texturePaths);
        }
    }

    private static Texture2D FindBestSpriteSheet(
        string characterName,
        CharacterAnimationAction action,
        IEnumerable<string> texturePaths)
    {
        string bestPath = null;
        int bestScore = int.MinValue;
        string characterKey = NormalizeSearchValue(characterName);
        string[] actionAliases = GetActionSearchAliases(action);

        foreach (string texturePath in texturePaths)
        {
            int score = GetSpriteSheetMatchScore(characterKey, actionAliases, texturePath);
            if (score > bestScore ||
                score == bestScore && bestPath != null &&
                string.Compare(texturePath, bestPath, StringComparison.OrdinalIgnoreCase) < 0)
            {
                bestPath = texturePath;
                bestScore = score;
            }
        }

        if (bestScore == int.MinValue || string.IsNullOrEmpty(bestPath))
        {
            return null;
        }

        return AssetDatabase.LoadAssetAtPath<Texture2D>(bestPath);
    }

    private static int GetSpriteSheetMatchScore(
        string characterKey,
        IEnumerable<string> actionAliases,
        string texturePath)
    {
        string fileKey = NormalizeSearchValue(Path.GetFileNameWithoutExtension(texturePath));
        string pathKey = NormalizeSearchValue(texturePath);
        if (string.IsNullOrEmpty(characterKey) || string.IsNullOrEmpty(fileKey) ||
            !pathKey.Contains(characterKey) || fileKey.Contains("hitbox"))
        {
            return int.MinValue;
        }

        int bestScore = int.MinValue;
        foreach (string alias in actionAliases)
        {
            int actionIndex = fileKey.LastIndexOf(alias, StringComparison.Ordinal);
            if (actionIndex <= 0)
            {
                continue;
            }

            string suffix = fileKey.Substring(actionIndex + alias.Length);
            if (suffix.Length > 0 && !suffix.All(char.IsDigit))
            {
                continue;
            }

            int score = 100;
            if (fileKey == characterKey + alias)
            {
                score = 500;
            }
            else if (fileKey.StartsWith(characterKey, StringComparison.Ordinal))
            {
                score = 300;
            }

            string folderPath = Path.GetDirectoryName(texturePath);
            string folderName = folderPath != null ? Path.GetFileName(folderPath) : string.Empty;
            string folderKey = NormalizeSearchValue(folderName);
            if (folderKey == characterKey)
            {
                score += 100;
            }

            bestScore = Mathf.Max(bestScore, score);
        }

        return bestScore;
    }

    private static string[] GetActionSearchAliases(CharacterAnimationAction action)
    {
        switch (action)
        {
            case CharacterAnimationAction.Idle:
                return IdleSearchAliases;
            case CharacterAnimationAction.Walk:
                return WalkSearchAliases;
            case CharacterAnimationAction.Attack:
                return AttackSearchAliases;
            case CharacterAnimationAction.Hit:
                return HitSearchAliases;
            case CharacterAnimationAction.Death:
                return DeathSearchAliases;
            default:
                return Array.Empty<string>();
        }
    }

    private static string NormalizeSearchValue(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        return new string(value
            .Where(char.IsLetterOrDigit)
            .Select(char.ToLowerInvariant)
            .ToArray());
    }

    private void RefreshControllerScan()
    {
        _scannedOverrideController = _overrideController;
        _scanResult = CharacterAnimatorPlaceholderScanner.Scan(_overrideController);
    }

    private void ClearValidation()
    {
        _validationMessages = new List<CharacterGeneratorMessage>();
    }

    private void EnsureSerializedState()
    {
        if (_directionOrder == null ||
            _directionOrder.Length != CharacterAnimationNaming.Directions.Length)
        {
            _directionOrder = CharacterAnimationNaming.Directions.ToArray();
        }

        List<CharacterActionSheetSettings> sourceSettings;
        if (_actionSettings != null)
        {
            sourceSettings = _actionSettings;
        }
        else
        {
            sourceSettings = new List<CharacterActionSheetSettings>();
        }

        Dictionary<CharacterAnimationAction, CharacterActionSheetSettings> existingSettings =
            sourceSettings
            .Where(settings => settings != null)
            .GroupBy(settings => settings.Action)
            .ToDictionary(group => group.Key, group => group.First());

        _actionSettings = CharacterAnimationNaming.Actions
            .Select(action =>
            {
                if (existingSettings.TryGetValue(action, out CharacterActionSheetSettings settings))
                {
                    return settings;
                }

                return new CharacterActionSheetSettings
                {
                    Action = action
                };
            })
            .ToList();

        if (_outputFolder == null)
        {
            DefaultAsset defaultOutputFolder = AssetDatabase.LoadAssetAtPath<DefaultAsset>(DefaultOutputFolder);
            if (defaultOutputFolder != null)
            {
                _outputFolder = defaultOutputFolder;
            }
            else
            {
                _outputFolder = AssetDatabase.LoadAssetAtPath<DefaultAsset>("Assets");
            }
        }
    }

    private static MessageType ToUnityMessageType(CharacterGeneratorMessageType type)
    {
        switch (type)
        {
            case CharacterGeneratorMessageType.Info:
                return MessageType.Info;
            case CharacterGeneratorMessageType.Warning:
                return MessageType.Warning;
            case CharacterGeneratorMessageType.Error:
                return MessageType.Error;
            default:
                return MessageType.None;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.U2D.Sprites;
using UnityEditorInternal;
using UnityEngine;

public sealed class SpriteSheetSlice : EditorWindow
{
    private enum SliceMode
    {
        Automatic,
        GridByCellCount,
        GridByCellSize
    }

    private enum ExistingSliceHandling
    {
        SmartReplace,
        SafeKeepExisting
    }

    [Serializable]
    private sealed class SliceSnapshot
    {
        public string AssetPath;
        public SpriteRect[] SpriteRects;
        public SpriteNameFileIdPair[] NameFileIdPairs;
    }

    private sealed class PreparedTexture
    {
        public Texture2D Texture;
        public string AssetPath;
        public SliceSnapshot Snapshot;
        public SpriteRect[] FinalSpriteRects;
        public SpriteNameFileIdPair[] FinalNameFileIdPairs;
    }

    private readonly struct PixelBuffer
    {
        public readonly int Width;
        public readonly int Height;
        public readonly Color32[] Pixels;

        public PixelBuffer(Texture2D texture)
        {
            Width = texture.width;
            Height = texture.height;
            Pixels = texture.GetPixels32();
        }

        public bool HasVisiblePixel(RectInt rect)
        {
            int minX = Mathf.Clamp(rect.xMin, 0, Width);
            int maxX = Mathf.Clamp(rect.xMax, 0, Width);
            int minY = Mathf.Clamp(rect.yMin, 0, Height);
            int maxY = Mathf.Clamp(rect.yMax, 0, Height);

            for (int y = minY; y < maxY; y++)
            {
                int rowOffset = y * Width;
                for (int x = minX; x < maxX; x++)
                {
                    if (Pixels[rowOffset + x].a > 0)
                    {
                        return true;
                    }
                }
            }

            return false;
        }
    }

    [SerializeField] private List<Texture2D> _textures = new List<Texture2D>();
    [SerializeField] private SliceMode _sliceMode = SliceMode.Automatic;
    [SerializeField] private ExistingSliceHandling _existingSliceHandling =
        ExistingSliceHandling.SafeKeepExisting;
    [SerializeField] private Vector2 _normalizedPivot = new Vector2(0.5f, 0.5f);
    [SerializeField] private Vector2Int _cellCount = new Vector2Int(4, 4);
    [SerializeField] private Vector2Int _cellSize = new Vector2Int(32, 32);
    [SerializeField] private Vector2Int _offset = Vector2Int.zero;
    [SerializeField] private Vector2Int _padding = Vector2Int.zero;
    [SerializeField] private bool _keepEmptyRects;
    [SerializeField] private int _automaticMinimumSize = 4;
    [SerializeField] private int _automaticExtrude;

    private Vector2 _scrollPosition;
    private readonly List<string> _messages = new List<string>();
    private MessageType _messageType = MessageType.None;

    [MenuItem("Tools/Sprite/Sprite Sheet Auto Slicer")]
    public static void Open()
    {
        SpriteSheetSlice window = GetWindow<SpriteSheetSlice>();
        window.titleContent = new GUIContent("Sprite Slicer");
        window.minSize = new Vector2(520f, 600f);
        window.Show();
    }

    private void OnEnable()
    {
        titleContent = new GUIContent("Sprite Slicer");
        minSize = new Vector2(520f, 600f);

        if (_textures == null)
        {
            _textures = new List<Texture2D>();
        }

        RemoveMissingAndDuplicateTextures();
        if (_textures.Count == 0)
        {
            AddObjects(Selection.objects);
        }
    }

    private void OnGUI()
    {
        _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);

        EditorGUILayout.Space(8f);
        EditorGUILayout.LabelField("Batch Sprite Sheet Slicer", HeaderStyle());
        EditorGUILayout.Space(3f);
        EditorGUILayout.HelpBox(
            "Apply one slicing configuration to multiple Sprite Multiple textures. " +
            "Texture pixels and unrelated import settings are not modified.",
            MessageType.Info);

        DrawTextureSelection();
        EditorGUILayout.Space(8f);
        EditorGUI.BeginChangeCheck();
        DrawSliceSettings();
        if (EditorGUI.EndChangeCheck())
        {
            ClearMessages();
        }
        EditorGUILayout.Space(8f);
        DrawMessages();
        EditorGUILayout.Space(8f);
        DrawActions();
        EditorGUILayout.Space(12f);

        EditorGUILayout.EndScrollView();
    }

    private void DrawTextureSelection()
    {
        EditorGUILayout.LabelField("Sprite Sheets", EditorStyles.boldLabel);
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);

        Rect dropArea = GUILayoutUtility.GetRect(
            0f,
            52f,
            GUILayout.ExpandWidth(true));
        GUI.Box(
            dropArea,
            "Drag Texture2D or Sprite assets here",
            EditorStyles.helpBox);

        Event currentEvent = Event.current;
        if ((currentEvent.type == EventType.DragUpdated ||
             currentEvent.type == EventType.DragPerform) &&
            dropArea.Contains(currentEvent.mousePosition))
        {
            DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
            if (currentEvent.type == EventType.DragPerform)
            {
                DragAndDrop.AcceptDrag();
                AddObjects(DragAndDrop.objectReferences);
                currentEvent.Use();
            }
        }

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Add Project Selection"))
        {
            AddObjects(Selection.objects);
        }

        if (GUILayout.Button("Remove Missing"))
        {
            RemoveMissingAndDuplicateTextures();
            ClearMessages();
        }

        if (GUILayout.Button("Clear"))
        {
            _textures.Clear();
            ClearMessages();
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(3f);

        bool textureListChanged = false;
        for (int index = 0; index < _textures.Count; index++)
        {
            EditorGUILayout.BeginHorizontal();
            Texture2D nextTexture = (Texture2D)EditorGUILayout.ObjectField(
                _textures[index],
                typeof(Texture2D),
                false);

            if (nextTexture != _textures[index])
            {
                _textures[index] = nextTexture;
                textureListChanged = true;
            }

            if (GUILayout.Button("Remove", GUILayout.Width(70f)))
            {
                _textures.RemoveAt(index);
                textureListChanged = true;
                EditorGUILayout.EndHorizontal();
                break;
            }

            EditorGUILayout.EndHorizontal();
        }

        if (textureListChanged)
        {
            RemoveMissingAndDuplicateTextures();
            ClearMessages();
        }

        EditorGUILayout.LabelField(
            $"{_textures.Count} unique texture(s) selected",
            EditorStyles.miniLabel);

        EditorGUILayout.EndVertical();
    }

    private void DrawSliceSettings()
    {
        EditorGUILayout.LabelField("Slice Settings", EditorStyles.boldLabel);
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);

        _sliceMode = (SliceMode)EditorGUILayout.EnumPopup("Type", _sliceMode);
        _existingSliceHandling = (ExistingSliceHandling)EditorGUILayout.EnumPopup(
            new GUIContent(
                "Existing Slices",
                "Smart Replace creates the requested layout and preserves IDs for exact rectangle matches. " +
                "Safe Keep Existing never removes existing rectangles."),
            _existingSliceHandling);

        EditorGUILayout.Space(3f);
        DrawModeSpecificSettings();
        EditorGUILayout.Space(3f);

        _normalizedPivot.x = EditorGUILayout.Slider(
            new GUIContent("Normalized Pivot X", "Zero is left and one is right."),
            _normalizedPivot.x,
            0f,
            1f);
        _normalizedPivot.y = EditorGUILayout.Slider(
            new GUIContent("Normalized Pivot Y", "Zero is bottom and one is top."),
            _normalizedPivot.y,
            0f,
            1f);

        _normalizedPivot = new Vector2(
            Mathf.Clamp01(_normalizedPivot.x),
            Mathf.Clamp01(_normalizedPivot.y));

        EditorGUILayout.HelpBox(
            _existingSliceHandling == ExistingSliceHandling.SmartReplace
                ? "Smart Replace removes unmatched old rectangles and their Sprite IDs. Exact rectangle matches keep their existing Sprite ID, name, border, and reference identity."
                : "Safe Keep Existing keeps every existing rectangle, updates its pivot, and only adds generated rectangles that do not overlap existing slices.",
            MessageType.None);

        EditorGUILayout.EndVertical();
    }

    private void DrawModeSpecificSettings()
    {
        switch (_sliceMode)
        {
            case SliceMode.Automatic:
                _automaticMinimumSize = EditorGUILayout.IntField(
                    new GUIContent(
                        "Minimum Sprite Size",
                        "Connected regions smaller than this value are ignored by automatic slicing."),
                    _automaticMinimumSize);
                _automaticExtrude = EditorGUILayout.IntField(
                    new GUIContent(
                        "Extrude",
                        "Additional pixels included around automatically detected regions."),
                    _automaticExtrude);
                break;

            case SliceMode.GridByCellCount:
                _cellCount = EditorGUILayout.Vector2IntField(
                    new GUIContent("Columns and Rows", "X is columns and Y is rows."),
                    _cellCount);
                DrawGridSharedSettings();
                break;

            case SliceMode.GridByCellSize:
                _cellSize = EditorGUILayout.Vector2IntField(
                    new GUIContent("Cell Size", "Cell width and height in pixels."),
                    _cellSize);
                DrawGridSharedSettings();
                break;

            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    private void DrawGridSharedSettings()
    {
        _offset = EditorGUILayout.Vector2IntField(
            new GUIContent(
                "Offset",
                "Pixel offset from the top-left corner before the first cell."),
            _offset);
        _padding = EditorGUILayout.Vector2IntField(
            new GUIContent(
                "Padding",
                "Horizontal and vertical pixel spacing between cells."),
            _padding);
        _keepEmptyRects = EditorGUILayout.Toggle(
            new GUIContent(
                "Keep Empty Rects",
                "Create a Sprite rect even when the cell is fully transparent."),
            _keepEmptyRects);
    }

    private void DrawMessages()
    {
        if (_messages.Count == 0)
        {
            return;
        }

        EditorGUILayout.LabelField("Status", EditorStyles.boldLabel);
        foreach (string message in _messages)
        {
            EditorGUILayout.HelpBox(message, _messageType);
        }
    }

    private void DrawActions()
    {
        EditorGUILayout.BeginHorizontal();

        if (GUILayout.Button("Validate", GUILayout.Height(34f)))
        {
            ValidateOnly();
        }

        if (GUILayout.Button("Slice All", GUILayout.Height(34f)))
        {
            SliceAll();
        }

        EditorGUILayout.EndHorizontal();
    }

    private void ValidateOnly()
    {
        List<string> errors = ValidateConfigurationAndTextures();
        _messages.Clear();

        if (errors.Count > 0)
        {
            _messageType = MessageType.Error;
            _messages.AddRange(errors);
            return;
        }

        try
        {
            List<PreparedTexture> preparedTextures = PrepareAllTextures();
            int finalRectCount = preparedTextures.Sum(item => item.FinalSpriteRects.Length);
            _messageType = MessageType.Info;
            _messages.Add(
                $"Validation passed for {preparedTextures.Count} texture(s) and {finalRectCount} final Sprite rects. " +
                "No assets were changed.");
        }
        catch (OperationCanceledException)
        {
            _messageType = MessageType.Warning;
            _messages.Add("Validation was canceled. No assets were changed.");
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            _messageType = MessageType.Error;
            _messages.Add("Validation failed: " + exception.Message);
        }
        finally
        {
            EditorUtility.ClearProgressBar();
        }
    }

    private void SliceAll()
    {
        _messages.Clear();
        List<string> errors = ValidateConfigurationAndTextures();
        if (errors.Count > 0)
        {
            _messageType = MessageType.Error;
            _messages.AddRange(errors);
            return;
        }

        List<PreparedTexture> preparedTextures;
        try
        {
            preparedTextures = PrepareAllTextures();
        }
        catch (OperationCanceledException)
        {
            _messageType = MessageType.Warning;
            _messages.Add("Slicing was canceled before any assets were changed.");
            return;
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            _messageType = MessageType.Error;
            _messages.Add("Could not prepare slicing data: " + exception.Message);
            return;
        }
        finally
        {
            EditorUtility.ClearProgressBar();
        }

        int existingRectCount = preparedTextures.Sum(item => item.Snapshot.SpriteRects.Length);
        int finalRectCount = preparedTextures.Sum(item => item.FinalSpriteRects.Length);
        string replacementWarning =
            _existingSliceHandling == ExistingSliceHandling.SmartReplace &&
            existingRectCount > 0
                ? "\n\nSmart Replace may remove old rectangles, Sprite IDs, and associated metadata that do not exactly match the new layout."
                : string.Empty;

        bool confirmed = EditorUtility.DisplayDialog(
            "Confirm Batch Slicing",
            $"Textures: {preparedTextures.Count}\n" +
            $"Existing Sprite rects: {existingRectCount}\n" +
            $"Final Sprite rects: {finalRectCount}" +
            replacementWarning +
            "\n\nTexture pixels and unrelated importer settings will not be changed.",
            "Slice",
            "Cancel");

        if (!confirmed)
        {
            _messageType = MessageType.Warning;
            _messages.Add("Slicing was canceled. No assets were changed.");
            return;
        }

        List<PreparedTexture> touchedTextures = new List<PreparedTexture>();
        try
        {
            for (int index = 0; index < preparedTextures.Count; index++)
            {
                PreparedTexture item = preparedTextures[index];
                bool canceled = EditorUtility.DisplayCancelableProgressBar(
                    "Batch Sprite Sheet Slicer",
                    $"Slicing {item.Texture.name} ({index + 1}/{preparedTextures.Count})",
                    index / (float)preparedTextures.Count);

                if (canceled)
                {
                    throw new OperationCanceledException(
                        "Slicing was canceled. Previous changes will be restored.");
                }

                touchedTextures.Add(item);
                ApplySpriteMetadata(
                    item.AssetPath,
                    item.FinalSpriteRects,
                    item.FinalNameFileIdPairs);
            }

            AssetDatabase.SaveAssets();
            _messageType = MessageType.Info;
            _messages.Add(
                $"Sliced {preparedTextures.Count} texture(s) and created {finalRectCount} Sprite rects.");
        }
        catch (Exception exception)
        {
            RestoreSnapshots(touchedTextures);
            Debug.LogException(exception);
            _messageType = MessageType.Error;
            _messages.Add(
                "Batch slicing failed. The tool attempted to restore all touched Sprite metadata. " +
                exception.Message);
        }
        finally
        {
            EditorUtility.ClearProgressBar();
        }
    }

    private List<PreparedTexture> PrepareAllTextures()
    {
        List<Texture2D> textures = BuildUniqueTextureList();
        List<PreparedTexture> preparedTextures = new List<PreparedTexture>(textures.Count);

        for (int index = 0; index < textures.Count; index++)
        {
            Texture2D texture = textures[index];
            bool canceled = EditorUtility.DisplayCancelableProgressBar(
                "Batch Sprite Sheet Slicer",
                $"Preparing {texture.name} ({index + 1}/{textures.Count})",
                index / (float)textures.Count);

            if (canceled)
            {
                throw new OperationCanceledException();
            }

            string assetPath = AssetDatabase.GetAssetPath(texture);
            SliceSnapshot snapshot = CaptureSnapshot(assetPath);
            List<Rect> generatedRects = GenerateRects(texture);
            ValidateGeneratedRects(texture, generatedRects);

            if (generatedRects.Count == 0)
            {
                throw new InvalidOperationException(
                    $"'{assetPath}' produced no Sprite rectangles with the current settings.");
            }

            SpriteRect[] finalSpriteRects = BuildFinalSpriteRects(
                texture,
                snapshot.SpriteRects,
                generatedRects);

            if (finalSpriteRects.Length == 0)
            {
                throw new InvalidOperationException(
                    $"'{assetPath}' has no final Sprite rectangles after applying the selected existing-slice behavior.");
            }

            preparedTextures.Add(new PreparedTexture
            {
                Texture = texture,
                AssetPath = assetPath,
                Snapshot = snapshot,
                FinalSpriteRects = finalSpriteRects,
                FinalNameFileIdPairs = BuildNameFileIdPairs(finalSpriteRects)
            });
        }

        return preparedTextures;
    }

    private static void ValidateGeneratedRects(
        Texture2D texture,
        IReadOnlyCollection<Rect> generatedRects)
    {
        foreach (Rect rect in generatedRects)
        {
            bool insideTexture =
                rect.width > 0f &&
                rect.height > 0f &&
                rect.xMin >= 0f &&
                rect.yMin >= 0f &&
                rect.xMax <= texture.width &&
                rect.yMax <= texture.height;

            if (!insideTexture)
            {
                throw new InvalidOperationException(
                    $"Generated rect {rect} is outside texture '{AssetDatabase.GetAssetPath(texture)}'.");
            }
        }
    }

    private List<Rect> GenerateRects(Texture2D texture)
    {
        switch (_sliceMode)
        {
            case SliceMode.Automatic:
                return GenerateAutomaticRects(texture);
            case SliceMode.GridByCellCount:
                return GenerateGridByCellCountRects(texture);
            case SliceMode.GridByCellSize:
                return GenerateGridByCellSizeRects(texture);
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    private List<Rect> GenerateAutomaticRects(Texture2D sourceTexture)
    {
        Texture2D readableTexture = CreateReadableCopy(sourceTexture);
        try
        {
            Rect[] generated = InternalSpriteUtility.GenerateAutomaticSpriteRectangles(
                readableTexture,
                _automaticMinimumSize,
                _automaticExtrude);

            return SortRects(generated);
        }
        finally
        {
            DestroyImmediate(readableTexture);
        }
    }

    private List<Rect> GenerateGridByCellCountRects(Texture2D texture)
    {
        int columns = _cellCount.x;
        int rows = _cellCount.y;
        int usableWidth = texture.width - _offset.x - _padding.x * (columns - 1);
        int usableHeight = texture.height - _offset.y - _padding.y * (rows - 1);

        if (usableWidth <= 0 || usableHeight <= 0)
        {
            throw new InvalidOperationException(
                $"Grid settings do not fit inside '{AssetDatabase.GetAssetPath(texture)}'.");
        }

        if (usableWidth % columns != 0 || usableHeight % rows != 0)
        {
            throw new InvalidOperationException(
                $"Grid By Cell Count does not divide '{AssetDatabase.GetAssetPath(texture)}' into whole-pixel cells. " +
                "Adjust count, offset, or padding.");
        }

        Vector2Int calculatedCellSize = new Vector2Int(
            usableWidth / columns,
            usableHeight / rows);

        return GenerateGridRects(
            texture,
            calculatedCellSize,
            columns,
            rows);
    }

    private List<Rect> GenerateGridByCellSizeRects(Texture2D texture)
    {
        int horizontalStep = _cellSize.x + _padding.x;
        int verticalStep = _cellSize.y + _padding.y;
        int columns = 0;
        int rows = 0;

        for (int x = _offset.x; x + _cellSize.x <= texture.width; x += horizontalStep)
        {
            columns++;
        }

        for (int top = _offset.y; top + _cellSize.y <= texture.height; top += verticalStep)
        {
            rows++;
        }

        if (columns <= 0 || rows <= 0)
        {
            throw new InvalidOperationException(
                $"Cell Size settings do not fit inside '{AssetDatabase.GetAssetPath(texture)}'.");
        }

        return GenerateGridRects(
            texture,
            _cellSize,
            columns,
            rows);
    }

    private List<Rect> GenerateGridRects(
        Texture2D texture,
        Vector2Int cellSize,
        int columns,
        int rows)
    {
        Texture2D readableTexture = null;
        PixelBuffer pixelBuffer = default;

        if (!_keepEmptyRects)
        {
            readableTexture = CreateReadableCopy(texture);
            pixelBuffer = new PixelBuffer(readableTexture);
        }

        try
        {
            List<Rect> rects = new List<Rect>(columns * rows);
            for (int row = 0; row < rows; row++)
            {
                int top = _offset.y + row * (cellSize.y + _padding.y);
                int y = texture.height - top - cellSize.y;

                for (int column = 0; column < columns; column++)
                {
                    int x = _offset.x + column * (cellSize.x + _padding.x);
                    RectInt rect = new RectInt(x, y, cellSize.x, cellSize.y);

                    if (_keepEmptyRects || pixelBuffer.HasVisiblePixel(rect))
                    {
                        rects.Add(new Rect(rect.x, rect.y, rect.width, rect.height));
                    }
                }
            }

            return rects;
        }
        finally
        {
            if (readableTexture != null)
            {
                DestroyImmediate(readableTexture);
            }
        }
    }

    private SpriteRect[] BuildFinalSpriteRects(
        Texture2D texture,
        IReadOnlyList<SpriteRect> existingRects,
        IReadOnlyList<Rect> generatedRects)
    {
        if (_existingSliceHandling == ExistingSliceHandling.SafeKeepExisting)
        {
            return BuildSafeSpriteRects(texture, existingRects, generatedRects);
        }

        return BuildSmartReplacementSpriteRects(texture, existingRects, generatedRects);
    }

    private SpriteRect[] BuildSmartReplacementSpriteRects(
        Texture2D texture,
        IReadOnlyList<SpriteRect> existingRects,
        IReadOnlyList<Rect> generatedRects)
    {
        bool[] usedExisting = new bool[existingRects.Count];
        HashSet<string> usedNames = new HashSet<string>(StringComparer.Ordinal);
        List<SpriteRect> result = new List<SpriteRect>(generatedRects.Count);

        for (int index = 0; index < generatedRects.Count; index++)
        {
            Rect generatedRect = generatedRects[index];
            int matchIndex = FindExactRectMatch(
                existingRects,
                usedExisting,
                generatedRect);

            SpriteRect spriteRect;
            if (matchIndex >= 0)
            {
                usedExisting[matchIndex] = true;
                spriteRect = CloneSpriteRect(existingRects[matchIndex]);
                spriteRect.rect = generatedRect;
                spriteRect.pivot = _normalizedPivot;
                spriteRect.alignment = SpriteAlignment.Custom;
            }
            else
            {
                spriteRect = CreateNewSpriteRect(
                    texture,
                    generatedRect,
                    index,
                    usedNames);
            }

            spriteRect.name = MakeUniqueName(spriteRect.name, usedNames);
            usedNames.Add(spriteRect.name);
            result.Add(spriteRect);
        }

        return result.ToArray();
    }

    private SpriteRect[] BuildSafeSpriteRects(
        Texture2D texture,
        IReadOnlyList<SpriteRect> existingRects,
        IReadOnlyList<Rect> generatedRects)
    {
        HashSet<string> usedNames = new HashSet<string>(
            existingRects.Select(rect => rect.name),
            StringComparer.Ordinal);
        List<SpriteRect> result = existingRects
            .Select(CloneSpriteRect)
            .ToList();

        foreach (SpriteRect existing in result)
        {
            existing.pivot = _normalizedPivot;
            existing.alignment = SpriteAlignment.Custom;
        }

        for (int index = 0; index < generatedRects.Count; index++)
        {
            Rect generatedRect = generatedRects[index];
            int exactIndex = FindExactRectMatch(result, null, generatedRect);
            if (exactIndex >= 0)
            {
                continue;
            }

            bool overlapsExisting = result.Any(existing =>
                RectsOverlap(existing.rect, generatedRect));
            if (overlapsExisting)
            {
                continue;
            }

            SpriteRect spriteRect = CreateNewSpriteRect(
                texture,
                generatedRect,
                index,
                usedNames);
            usedNames.Add(spriteRect.name);
            result.Add(spriteRect);
        }

        return result
            .OrderByDescending(rect => rect.rect.yMax)
            .ThenBy(rect => rect.rect.xMin)
            .ToArray();
    }

    private SpriteRect CreateNewSpriteRect(
        Texture2D texture,
        Rect rect,
        int index,
        ISet<string> usedNames)
    {
        string proposedName = $"{texture.name}_{index}";
        string uniqueName = MakeUniqueName(proposedName, usedNames);

        return new SpriteRect
        {
            name = uniqueName,
            rect = rect,
            pivot = _normalizedPivot,
            alignment = SpriteAlignment.Custom,
            border = Vector4.zero,
            spriteID = GUID.Generate()
        };
    }

    private List<string> ValidateConfigurationAndTextures()
    {
        List<string> errors = new List<string>();
        List<Texture2D> textures = BuildUniqueTextureList();

        if (textures.Count == 0)
        {
            errors.Add("Select at least one Texture2D or Sprite asset.");
        }

        if (_normalizedPivot.x < 0f ||
            _normalizedPivot.x > 1f ||
            _normalizedPivot.y < 0f ||
            _normalizedPivot.y > 1f)
        {
            errors.Add("Normalized Pivot values must be between zero and one.");
        }

        if (_sliceMode == SliceMode.Automatic)
        {
            if (_automaticMinimumSize <= 0)
            {
                errors.Add("Minimum Sprite Size must be greater than zero.");
            }

            if (_automaticExtrude < 0)
            {
                errors.Add("Extrude cannot be negative.");
            }
        }
        else
        {
            if (_offset.x < 0 || _offset.y < 0)
            {
                errors.Add("Grid Offset values cannot be negative.");
            }

            if (_padding.x < 0 || _padding.y < 0)
            {
                errors.Add("Grid Padding values cannot be negative.");
            }

            if (_sliceMode == SliceMode.GridByCellCount &&
                (_cellCount.x <= 0 || _cellCount.y <= 0))
            {
                errors.Add("Grid column and row counts must be greater than zero.");
            }

            if (_sliceMode == SliceMode.GridByCellSize &&
                (_cellSize.x <= 0 || _cellSize.y <= 0))
            {
                errors.Add("Cell Size values must be greater than zero.");
            }
        }

        foreach (Texture2D texture in textures)
        {
            string path = AssetDatabase.GetAssetPath(texture);
            if (string.IsNullOrEmpty(path) ||
                !path.StartsWith("Assets/", StringComparison.Ordinal))
            {
                errors.Add(
                    $"'{texture.name}' must be a saved texture asset inside the project's Assets folder.");
                continue;
            }

            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null)
            {
                errors.Add($"'{path}' does not use a TextureImporter.");
                continue;
            }

            if (importer.textureType != TextureImporterType.Sprite ||
                importer.spriteImportMode != SpriteImportMode.Multiple)
            {
                errors.Add(
                    $"'{path}' must use Texture Type Sprite and Sprite Mode Multiple. " +
                    "The tool will not change these import settings automatically.");
            }

            if (!AssetDatabase.IsOpenForEdit(path, out string editMessage))
            {
                errors.Add($"'{path}' is not editable: {editMessage}");
            }
        }

        return errors;
    }

    private static SliceSnapshot CaptureSnapshot(string assetPath)
    {
        ISpriteEditorDataProvider provider = GetDataProvider(assetPath);
        SpriteRect[] spriteRects = provider
            .GetSpriteRects()
            .Select(CloneSpriteRect)
            .ToArray();
        ISpriteNameFileIdDataProvider nameProvider =
            provider.GetDataProvider<ISpriteNameFileIdDataProvider>();
        SpriteNameFileIdPair[] nameFileIdPairs = nameProvider != null
            ? nameProvider.GetNameFileIdPairs().ToArray()
            : BuildNameFileIdPairs(spriteRects);

        return new SliceSnapshot
        {
            AssetPath = assetPath,
            SpriteRects = spriteRects,
            NameFileIdPairs = nameFileIdPairs
        };
    }

    private static void ApplySpriteMetadata(
        string assetPath,
        SpriteRect[] spriteRects,
        SpriteNameFileIdPair[] nameFileIdPairs)
    {
        ISpriteEditorDataProvider provider = GetDataProvider(assetPath);
        provider.SetSpriteRects(spriteRects.Select(CloneSpriteRect).ToArray());

        ISpriteNameFileIdDataProvider nameProvider =
            provider.GetDataProvider<ISpriteNameFileIdDataProvider>();
        if (nameProvider != null)
        {
            nameProvider.SetNameFileIdPairs(nameFileIdPairs);
        }

        provider.Apply();

        TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
        if (importer == null)
        {
            throw new InvalidOperationException(
                $"TextureImporter could not be reloaded for '{assetPath}'.");
        }

        importer.SaveAndReimport();
    }

    private static void RestoreSnapshots(IEnumerable<PreparedTexture> touchedTextures)
    {
        foreach (PreparedTexture item in touchedTextures.Reverse())
        {
            try
            {
                ApplySpriteMetadata(
                    item.Snapshot.AssetPath,
                    item.Snapshot.SpriteRects,
                    item.Snapshot.NameFileIdPairs);
            }
            catch (Exception restoreException)
            {
                Debug.LogError(
                    $"Failed to restore Sprite metadata for '{item.AssetPath}': " +
                    restoreException);
            }
        }

        AssetDatabase.SaveAssets();
    }

    private static ISpriteEditorDataProvider GetDataProvider(string assetPath)
    {
        TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
        if (importer == null)
        {
            throw new InvalidOperationException(
                $"TextureImporter was not found for '{assetPath}'.");
        }

        SpriteDataProviderFactories factories = new SpriteDataProviderFactories();
        factories.Init();
        ISpriteEditorDataProvider provider =
            factories.GetSpriteEditorDataProviderFromObject(importer);

        if (provider == null)
        {
            throw new InvalidOperationException(
                $"Sprite data provider was not found for '{assetPath}'.");
        }

        provider.InitSpriteEditorDataProvider();
        return provider;
    }

    private static Texture2D CreateReadableCopy(Texture2D source)
    {
        RenderTexture previous = RenderTexture.active;
        RenderTexture temporary = RenderTexture.GetTemporary(
            source.width,
            source.height,
            0,
            RenderTextureFormat.ARGB32,
            RenderTextureReadWrite.Default);

        try
        {
            Graphics.Blit(source, temporary);
            RenderTexture.active = temporary;

            Texture2D readable = new Texture2D(
                source.width,
                source.height,
                TextureFormat.RGBA32,
                false,
                false);
            readable.ReadPixels(
                new Rect(0f, 0f, source.width, source.height),
                0,
                0,
                false);
            readable.Apply(false, false);
            return readable;
        }
        finally
        {
            RenderTexture.active = previous;
            RenderTexture.ReleaseTemporary(temporary);
        }
    }

    private static SpriteRect CloneSpriteRect(SpriteRect source)
    {
        return new SpriteRect
        {
            name = source.name,
            rect = source.rect,
            pivot = source.pivot,
            alignment = source.alignment,
            border = source.border,
            spriteID = source.spriteID
        };
    }

    private static SpriteNameFileIdPair[] BuildNameFileIdPairs(
        IEnumerable<SpriteRect> spriteRects)
    {
        return spriteRects
            .Select(rect => new SpriteNameFileIdPair(rect.name, rect.spriteID))
            .ToArray();
    }

    private static int FindExactRectMatch(
        IReadOnlyList<SpriteRect> existingRects,
        IReadOnlyList<bool> usedExisting,
        Rect generatedRect)
    {
        for (int index = 0; index < existingRects.Count; index++)
        {
            if (usedExisting != null && usedExisting[index])
            {
                continue;
            }

            if (RectsMatch(existingRects[index].rect, generatedRect))
            {
                return index;
            }
        }

        return -1;
    }

    private static bool RectsMatch(Rect left, Rect right)
    {
        const float tolerance = 0.01f;
        return Mathf.Abs(left.x - right.x) <= tolerance &&
               Mathf.Abs(left.y - right.y) <= tolerance &&
               Mathf.Abs(left.width - right.width) <= tolerance &&
               Mathf.Abs(left.height - right.height) <= tolerance;
    }

    private static bool RectsOverlap(Rect left, Rect right)
    {
        const float tolerance = 0.01f;
        Rect insetLeft = new Rect(
            left.x + tolerance,
            left.y + tolerance,
            Mathf.Max(0f, left.width - tolerance * 2f),
            Mathf.Max(0f, left.height - tolerance * 2f));
        Rect insetRight = new Rect(
            right.x + tolerance,
            right.y + tolerance,
            Mathf.Max(0f, right.width - tolerance * 2f),
            Mathf.Max(0f, right.height - tolerance * 2f));
        return insetLeft.Overlaps(insetRight);
    }

    private static List<Rect> SortRects(IEnumerable<Rect> rects)
    {
        return rects
            .Where(rect => rect.width > 0f && rect.height > 0f)
            .OrderByDescending(rect => rect.yMax)
            .ThenBy(rect => rect.xMin)
            .ToList();
    }

    private static string MakeUniqueName(string proposedName, ISet<string> usedNames)
    {
        string baseName = string.IsNullOrWhiteSpace(proposedName)
            ? "Sprite"
            : proposedName;
        string candidate = baseName;
        int suffix = 1;

        while (usedNames.Contains(candidate))
        {
            candidate = $"{baseName}_{suffix}";
            suffix++;
        }

        return candidate;
    }

    private List<Texture2D> BuildUniqueTextureList()
    {
        List<Texture2D> sourceTextures;
        if (_textures != null)
        {
            sourceTextures = _textures;
        }
        else
        {
            sourceTextures = new List<Texture2D>();
        }

        return sourceTextures
            .Where(texture => texture != null)
            .Distinct()
            .ToList();
    }

    private void AddObjects(IEnumerable<UnityEngine.Object> objects)
    {
        if (_textures == null)
        {
            _textures = new List<Texture2D>();
        }

        foreach (UnityEngine.Object selectedObject in objects)
        {
            Texture2D texture = ResolveTexture(selectedObject);
            if (texture != null && !_textures.Contains(texture))
            {
                _textures.Add(texture);
            }
        }

        RemoveMissingAndDuplicateTextures();
        ClearMessages();
        Repaint();
    }

    private static Texture2D ResolveTexture(UnityEngine.Object selectedObject)
    {
        if (selectedObject is Texture2D texture)
        {
            return texture;
        }

        if (selectedObject is Sprite sprite)
        {
            string spritePath = AssetDatabase.GetAssetPath(sprite);
            return AssetDatabase.LoadAssetAtPath<Texture2D>(spritePath);
        }

        return null;
    }

    private void RemoveMissingAndDuplicateTextures()
    {
        _textures = BuildUniqueTextureList();
    }

    private void ClearMessages()
    {
        _messages.Clear();
        _messageType = MessageType.None;
    }

    private static GUIStyle HeaderStyle()
    {
        return new GUIStyle(EditorStyles.boldLabel)
        {
            fontSize = 17
        };
    }
}

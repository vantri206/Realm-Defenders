using System.IO;
using UnityEditor;
using UnityEngine;

public static class CombatStagePreviewExporter
{
    private const int PreviewWidth = 1024;
    private const int PreviewHeight = 576;
    private const float BoundsPadding = 0.5f;
    private static readonly Vector3 PreviewOffset = new Vector3(10000f, 10000f, 0f);

    [MenuItem("Tools/Realm Defenders/Combat Stage/Generate Selected Map Preview")]
    private static void GenerateSelectedMapPreview()
    {
        bool generatedAny = false;
        Object[] selectedObjects = Selection.objects;

        for (int i = 0; i < selectedObjects.Length; i++)
        {
            Object selectedObject = selectedObjects[i];
            CombatStageDefinition definition = selectedObject as CombatStageDefinition;
            if (definition != null)
            {
                generatedAny |= GenerateMapPreview(definition);
                continue;
            }

            GameObject selectedGameObject = selectedObject as GameObject;
            if (selectedGameObject != null && selectedGameObject.TryGetComponent(out CombatStageAuthoring authoring))
            {
                generatedAny |= GenerateMapPreview(authoring, authoring.OutputDefinition);
            }
        }

        if (!generatedAny)
        {
            Debug.LogWarning("[CombatStagePreviewExporter] Select a CombatStageDefinition asset or CombatStageAuthoring GameObject to generate a map preview.");
        }
    }

    [MenuItem("Tools/Realm Defenders/Combat Stage/Generate Selected Map Preview", true)]
    private static bool CanGenerateSelectedMapPreview()
    {
        Object[] selectedObjects = Selection.objects;
        for (int i = 0; i < selectedObjects.Length; i++)
        {
            Object selectedObject = selectedObjects[i];
            if (selectedObject is CombatStageDefinition)
            {
                return true;
            }

            GameObject selectedGameObject = selectedObject as GameObject;
            if (selectedGameObject != null && selectedGameObject.GetComponent<CombatStageAuthoring>() != null)
            {
                return true;
            }
        }

        return false;
    }

    public static bool GenerateMapPreview(CombatStageAuthoring authoring, CombatStageDefinition definition)
    {
        if (authoring == null)
        {
            Debug.LogError("[CombatStagePreviewExporter] CombatStageAuthoring is required to generate map preview.");
            return false;
        }

        return GenerateMapPreview(authoring.MapView, definition, authoring.StageId);
    }

    public static bool GenerateMapPreview(CombatStageDefinition definition)
    {
        if (definition == null)
        {
            Debug.LogError("[CombatStagePreviewExporter] CombatStageDefinition is required to generate map preview.");
            return false;
        }

        return GenerateMapPreview(definition.MapPrefab, definition, definition.StageId);
    }

    private static bool GenerateMapPreview(CombatMapView mapView, CombatStageDefinition definition, string stageId)
    {
        if (mapView == null)
        {
            Debug.LogError("[CombatStagePreviewExporter] CombatMapView is required to generate map preview.", definition);
            return false;
        }

        if (definition == null)
        {
            Debug.LogError("[CombatStagePreviewExporter] CombatStageDefinition is required to save map preview.", mapView);
            return false;
        }

        string previewPath = GetPreviewPath(definition, stageId);
        if (string.IsNullOrWhiteSpace(previewPath))
        {
            return false;
        }

        GameObject previewRoot = null;
        Camera previewCamera = null;
        RenderTexture renderTexture = null;
        RenderTexture previousRenderTexture = RenderTexture.active;

        try
        {
            previewRoot = Object.Instantiate(mapView.gameObject);
            previewRoot.name = mapView.name + "_PreviewCapture";
            previewRoot.hideFlags = HideFlags.HideAndDontSave;
            previewRoot.transform.position += PreviewOffset;
            previewRoot.SetActive(true);

            if (!TryGetRendererBounds(previewRoot, out Bounds bounds))
            {
                Debug.LogError("[CombatStagePreviewExporter] Map preview needs at least one Renderer under CombatMapView.", mapView);
                return false;
            }

            GameObject cameraObject = new GameObject("MapPreviewCamera");
            cameraObject.hideFlags = HideFlags.HideAndDontSave;
            previewCamera = cameraObject.AddComponent<Camera>();
            SetupCamera(previewCamera, bounds);

            renderTexture = new RenderTexture(PreviewWidth, PreviewHeight, 24, RenderTextureFormat.ARGB32);
            renderTexture.Create();
            previewCamera.targetTexture = renderTexture;
            previewCamera.Render();

            RenderTexture.active = renderTexture;
            Texture2D texture = new Texture2D(PreviewWidth, PreviewHeight, TextureFormat.RGBA32, false);
            texture.ReadPixels(new Rect(0f, 0f, PreviewWidth, PreviewHeight), 0, 0);
            texture.Apply();

            byte[] pngBytes = texture.EncodeToPNG();
            Object.DestroyImmediate(texture);

            File.WriteAllBytes(previewPath, pngBytes);
            AssetDatabase.ImportAsset(previewPath, ImportAssetOptions.ForceUpdate);
            ConfigureSpriteImporter(previewPath);

            Sprite previewSprite = AssetDatabase.LoadAssetAtPath<Sprite>(previewPath);
            if (previewSprite == null)
            {
                Debug.LogError($"[CombatStagePreviewExporter] Could not load generated preview sprite at '{previewPath}'.", definition);
                return false;
            }

            Undo.RecordObject(definition, "Assign Map Preview");
            definition.SetMapPreview(previewSprite);
            EditorUtility.SetDirty(definition);
            AssetDatabase.SaveAssets();
            Debug.Log($"[CombatStagePreviewExporter] Generated map preview '{previewPath}'.", definition);
            return true;
        }
        finally
        {
            RenderTexture.active = previousRenderTexture;

            if (previewCamera != null)
            {
                previewCamera.targetTexture = null;
                Object.DestroyImmediate(previewCamera.gameObject);
            }

            if (renderTexture != null)
            {
                renderTexture.Release();
                Object.DestroyImmediate(renderTexture);
            }

            if (previewRoot != null)
            {
                Object.DestroyImmediate(previewRoot);
            }
        }
    }

    private static void SetupCamera(Camera previewCamera, Bounds bounds)
    {
        previewCamera.clearFlags = CameraClearFlags.SolidColor;
        previewCamera.backgroundColor = new Color(0f, 0f, 0f, 0f);
        previewCamera.orthographic = true;
        previewCamera.aspect = (float)PreviewWidth / PreviewHeight;
        previewCamera.nearClipPlane = 0.01f;
        previewCamera.farClipPlane = 100f;
        previewCamera.cullingMask = -1;
        previewCamera.transform.position = new Vector3(bounds.center.x, bounds.center.y, bounds.min.z - 10f);
        previewCamera.transform.rotation = Quaternion.identity;

        float paddedHeight = bounds.extents.y + BoundsPadding;
        float paddedWidth = bounds.extents.x + BoundsPadding;
        previewCamera.orthographicSize = Mathf.Max(paddedHeight, paddedWidth / previewCamera.aspect);
    }

    private static bool TryGetRendererBounds(GameObject root, out Bounds bounds)
    {
        Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
        bounds = new Bounds(root.transform.position, Vector3.zero);
        bool hasBounds = false;

        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer renderer = renderers[i];
            if (renderer == null || !renderer.enabled)
            {
                continue;
            }

            if (!hasBounds)
            {
                bounds = renderer.bounds;
                hasBounds = true;
            }
            else
            {
                bounds.Encapsulate(renderer.bounds);
            }
        }

        return hasBounds;
    }

    private static string GetPreviewPath(CombatStageDefinition definition, string stageId)
    {
        if (definition.MapPreview != null)
        {
            string existingPath = AssetDatabase.GetAssetPath(definition.MapPreview);
            if (!string.IsNullOrWhiteSpace(existingPath))
            {
                return existingPath;
            }
        }

        string definitionPath = AssetDatabase.GetAssetPath(definition);
        if (string.IsNullOrWhiteSpace(definitionPath))
        {
            Debug.LogError("[CombatStagePreviewExporter] Stage definition asset must be saved before generating map preview.", definition);
            return null;
        }

        string directory = Path.GetDirectoryName(definitionPath);
        if (!string.IsNullOrEmpty(directory))
        {
            directory = directory.Replace('\\', '/');
        }

        return $"{directory}/{GetSafeFileName(stageId)}_MapPreview.png";
    }

    private static void ConfigureSpriteImporter(string assetPath)
    {
        TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
        if (importer == null)
        {
            return;
        }

        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.alphaIsTransparency = true;
        importer.mipmapEnabled = false;
        importer.SaveAndReimport();
    }

    private static string GetSafeFileName(string value)
    {
        string fileName = string.IsNullOrWhiteSpace(value) ? "CombatStage" : value.Trim();
        foreach (char invalidCharacter in Path.GetInvalidFileNameChars())
        {
            fileName = fileName.Replace(invalidCharacter, '_');
        }

        return fileName;
    }
}

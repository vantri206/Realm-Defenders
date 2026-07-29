using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

internal enum CharacterAnimationAction
{
    Idle,
    Walk,
    Attack,
    Hit,
    Death
}

internal enum CharacterAnimationDirection
{
    Bottom,
    Left,
    Right,
    Top
}

internal enum CharacterFrameSplitMode
{
    AutoEqual,
    ManualSplit
}

internal enum CharacterGeneratorMessageType
{
    Info,
    Warning,
    Error
}

internal enum CharacterActionLayout
{
    Invalid,
    FourDirections
}

[Serializable]
internal sealed class CharacterActionSheetSettings
{
    public CharacterAnimationAction Action;
    public Texture2D SpriteSheet;
    public CharacterFrameSplitMode SplitMode = CharacterFrameSplitMode.AutoEqual;
    public int BottomFrames = 1;
    public int LeftFrames = 1;
    public int RightFrames = 1;
    public int TopFrames = 1;
    public bool Expanded = true;

    public int GetManualFrameCount(CharacterAnimationDirection direction)
    {
        switch (direction)
        {
            case CharacterAnimationDirection.Bottom:
                return BottomFrames;
            case CharacterAnimationDirection.Left:
                return LeftFrames;
            case CharacterAnimationDirection.Right:
                return RightFrames;
            case CharacterAnimationDirection.Top:
                return TopFrames;
            default:
                throw new ArgumentOutOfRangeException(nameof(direction), direction, null);
        }
    }

    public void SetManualFrameCount(CharacterAnimationDirection direction, int value)
    {
        switch (direction)
        {
            case CharacterAnimationDirection.Bottom:
                BottomFrames = value;
                break;
            case CharacterAnimationDirection.Left:
                LeftFrames = value;
                break;
            case CharacterAnimationDirection.Right:
                RightFrames = value;
                break;
            case CharacterAnimationDirection.Top:
                TopFrames = value;
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(direction), direction, null);
        }
    }
}

internal sealed class CharacterGeneratorMessage
{
    public CharacterGeneratorMessageType Type { get; }
    public string Text { get; }

    public CharacterGeneratorMessage(CharacterGeneratorMessageType type, string text)
    {
        Type = type;
        Text = text;
    }
}

internal sealed class CharacterDiscoveredAction
{
    public CharacterAnimationAction Action;
    public CharacterActionLayout Layout;
    public bool UsesBlendTree;
    public readonly Dictionary<CharacterAnimationDirection, List<AnimationClip>> DirectionalPlaceholders =
        new Dictionary<CharacterAnimationDirection, List<AnimationClip>>();
    public readonly List<string> Issues = new List<string>();

    public IEnumerable<AnimationClip> AllPlaceholders =>
        DirectionalPlaceholders.Values.SelectMany(clips => clips).Distinct();

    public int PlaceholderCount => AllPlaceholders.Count();
}

internal sealed class CharacterControllerScanResult
{
    public AnimatorController BaseController;
    public readonly Dictionary<CharacterAnimationAction, CharacterDiscoveredAction> Actions =
        new Dictionary<CharacterAnimationAction, CharacterDiscoveredAction>();
    public readonly List<CharacterGeneratorMessage> Messages = new List<CharacterGeneratorMessage>();
}

internal sealed class CharacterAnimationGenerationRequest
{
    public AnimatorOverrideController OverrideController;
    public string CharacterName;
    public int SampleRate;
    public string OutputFolder;
    public IReadOnlyList<CharacterAnimationDirection> DirectionOrder;
    public IReadOnlyList<CharacterActionSheetSettings> ActionSettings;
}

internal sealed class CharacterAnimationClipPlan
{
    public CharacterAnimationAction Action;
    public CharacterAnimationDirection? Direction;
    public string ClipName;
    public string AssetPath;
    public bool Loop;
    public EditorCurveBinding SpriteBinding;
    public readonly List<Sprite> Sprites = new List<Sprite>();
    public readonly List<AnimationClip> Placeholders = new List<AnimationClip>();
}

internal sealed class CharacterAnimationGenerationPlan
{
    public AnimatorOverrideController OverrideController;
    public AnimatorController BaseController;
    public int SampleRate;
    public readonly List<CharacterAnimationClipPlan> Clips = new List<CharacterAnimationClipPlan>();
    public readonly List<CharacterGeneratorMessage> Messages = new List<CharacterGeneratorMessage>();

    public bool HasErrors => Messages.Any(message => message.Type == CharacterGeneratorMessageType.Error);
    public int ManagedPlaceholderCount => Clips.SelectMany(clip => clip.Placeholders).Distinct().Count();
}

internal sealed class CharacterAnimationGenerationResult
{
    public int CreatedClipCount;
    public int UpdatedClipCount;
    public int AppliedOverrideCount;
    public readonly List<AnimationClip> GeneratedClips = new List<AnimationClip>();
}

internal static class CharacterAnimationNaming
{
    public static readonly CharacterAnimationAction[] Actions =
    {
        CharacterAnimationAction.Idle,
        CharacterAnimationAction.Walk,
        CharacterAnimationAction.Attack,
        CharacterAnimationAction.Hit,
        CharacterAnimationAction.Death
    };

    public static readonly CharacterAnimationDirection[] Directions =
    {
        CharacterAnimationDirection.Bottom,
        CharacterAnimationDirection.Left,
        CharacterAnimationDirection.Right,
        CharacterAnimationDirection.Top
    };

    public static string GetActionDisplayName(CharacterAnimationAction action)
    {
        switch (action)
        {
            case CharacterAnimationAction.Idle:
                return "Idle";
            case CharacterAnimationAction.Walk:
                return "Walk";
            case CharacterAnimationAction.Attack:
                return "Attack";
            case CharacterAnimationAction.Hit:
                return "Hit";
            case CharacterAnimationAction.Death:
                return "Death";
            default:
                throw new ArgumentOutOfRangeException(nameof(action), action, null);
        }
    }

    public static string GetDirectionDisplayName(CharacterAnimationDirection direction)
    {
        switch (direction)
        {
            case CharacterAnimationDirection.Bottom:
                return "Bottom";
            case CharacterAnimationDirection.Left:
                return "Left";
            case CharacterAnimationDirection.Right:
                return "Right";
            case CharacterAnimationDirection.Top:
                return "Top";
            default:
                throw new ArgumentOutOfRangeException(nameof(direction), direction, null);
        }
    }

    public static bool ShouldLoop(CharacterAnimationAction action)
    {
        return action == CharacterAnimationAction.Idle || action == CharacterAnimationAction.Walk;
    }
}

internal static class CharacterAnimatorPlaceholderScanner
{
    private sealed class MotionCandidate
    {
        public CharacterAnimationAction Action;
        public CharacterAnimationDirection? Direction;
        public AnimationClip Clip;
        public bool FromBlendTree;
        public string Source;
    }

    private static readonly Regex CamelCaseBoundary =
        new Regex("([a-z0-9])([A-Z])", RegexOptions.Compiled);

    private static readonly Regex TokenSeparator =
        new Regex("[^a-z0-9]+", RegexOptions.Compiled);

    public static CharacterControllerScanResult Scan(AnimatorOverrideController overrideController)
    {
        CharacterControllerScanResult result = new CharacterControllerScanResult();

        foreach (CharacterAnimationAction action in CharacterAnimationNaming.Actions)
        {
            result.Actions[action] = new CharacterDiscoveredAction
            {
                Action = action,
                Layout = CharacterActionLayout.Invalid
            };
        }

        if (overrideController == null)
        {
            result.Messages.Add(new CharacterGeneratorMessage(
                CharacterGeneratorMessageType.Error,
                "No Animator Override Controller is selected."));
            return result;
        }

        RuntimeAnimatorController runtimeController = overrideController.runtimeAnimatorController;
        if (runtimeController is AnimatorOverrideController)
        {
            result.Messages.Add(new CharacterGeneratorMessage(
                CharacterGeneratorMessageType.Error,
                "Nested Override Controllers are not supported. Select an asset that references a Base Animator Controller directly."));
            return result;
        }

        AnimatorController animatorController = runtimeController as AnimatorController;
        if (animatorController == null)
        {
            result.Messages.Add(new CharacterGeneratorMessage(
                CharacterGeneratorMessageType.Error,
                "The Override Controller does not reference a valid Animator Controller."));
            return result;
        }

        result.BaseController = animatorController;

        List<MotionCandidate> candidates = new List<MotionCandidate>();
        HashSet<AnimatorStateMachine> visitedStateMachines = new HashSet<AnimatorStateMachine>();

        foreach (AnimatorControllerLayer layer in animatorController.layers)
        {
            ScanStateMachine(
                layer.stateMachine,
                layer.name,
                null,
                candidates,
                visitedStateMachines);
        }

        foreach (CharacterAnimationAction action in CharacterAnimationNaming.Actions)
        {
            BuildDiscoveredAction(result.Actions[action], candidates.Where(candidate => candidate.Action == action));
        }

        return result;
    }

    private static void ScanStateMachine(
        AnimatorStateMachine stateMachine,
        string hierarchy,
        CharacterAnimationAction? inheritedAction,
        ICollection<MotionCandidate> candidates,
        ISet<AnimatorStateMachine> visited)
    {
        if (stateMachine == null || !visited.Add(stateMachine))
        {
            return;
        }

        CharacterAnimationAction? stateMachineAction =
            TryInferAction(stateMachine.name) ?? inheritedAction;

        foreach (ChildAnimatorState childState in stateMachine.states)
        {
            AnimatorState state = childState.state;
            if (state == null || state.motion == null)
            {
                continue;
            }

            string statePath = string.IsNullOrEmpty(hierarchy)
                ? state.name
                : hierarchy + "/" + state.name;

            CharacterAnimationAction? action =
                TryInferAction(state.name) ??
                stateMachineAction ??
                TryInferAction(state.motion.name);

            AnimationClip directClip = state.motion as AnimationClip;
            if (directClip != null)
            {
                CharacterAnimationAction? directAction = action ?? TryInferAction(directClip.name);
                if (directAction.HasValue)
                {
                    candidates.Add(new MotionCandidate
                    {
                        Action = directAction.Value,
                        Direction = TryInferDirection(state.name) ?? TryInferDirection(directClip.name),
                        Clip = directClip,
                        FromBlendTree = false,
                        Source = statePath
                    });
                }

                continue;
            }

            BlendTree blendTree = state.motion as BlendTree;
            if (blendTree != null)
            {
                HashSet<BlendTree> visitedTrees = new HashSet<BlendTree>();
                ScanBlendTree(
                    blendTree,
                    action,
                    null,
                    statePath,
                    candidates,
                    visitedTrees);
            }
        }

        foreach (ChildAnimatorStateMachine childStateMachine in stateMachine.stateMachines)
        {
            string childHierarchy = string.IsNullOrEmpty(hierarchy)
                ? childStateMachine.stateMachine.name
                : hierarchy + "/" + childStateMachine.stateMachine.name;

            ScanStateMachine(
                childStateMachine.stateMachine,
                childHierarchy,
                TryInferAction(childStateMachine.stateMachine.name) ?? stateMachineAction,
                candidates,
                visited);
        }
    }

    private static void ScanBlendTree(
        BlendTree blendTree,
        CharacterAnimationAction? inheritedAction,
        CharacterAnimationDirection? inheritedDirection,
        string statePath,
        ICollection<MotionCandidate> candidates,
        ISet<BlendTree> visitedTrees)
    {
        if (blendTree == null || !visitedTrees.Add(blendTree))
        {
            return;
        }

        CharacterAnimationAction? treeAction = inheritedAction ?? TryInferAction(blendTree.name);
        bool directionalTree = IsDirectionalBlendTree(blendTree.blendType);

        foreach (ChildMotion child in blendTree.children)
        {
            if (child.motion == null)
            {
                continue;
            }

            CharacterAnimationDirection? childDirection = inheritedDirection;
            if (directionalTree)
            {
                childDirection = TryInferDirection(child.position) ?? childDirection;
            }

            AnimationClip childClip = child.motion as AnimationClip;
            if (childClip != null)
            {
                CharacterAnimationAction? childAction = treeAction ?? TryInferAction(childClip.name);
                if (childAction.HasValue)
                {
                    candidates.Add(new MotionCandidate
                    {
                        Action = childAction.Value,
                        Direction = childDirection ?? TryInferDirection(childClip.name),
                        Clip = childClip,
                        FromBlendTree = true,
                        Source = statePath + "/" + blendTree.name
                    });
                }

                continue;
            }

            BlendTree nestedTree = child.motion as BlendTree;
            if (nestedTree != null)
            {
                ScanBlendTree(
                    nestedTree,
                    treeAction,
                    childDirection,
                    statePath + "/" + blendTree.name,
                    candidates,
                    visitedTrees);
            }
        }
    }

    private static void BuildDiscoveredAction(
        CharacterDiscoveredAction discovered,
        IEnumerable<MotionCandidate> actionCandidates)
    {
        List<MotionCandidate> candidates = actionCandidates
            .Where(candidate => candidate.Clip != null)
            .ToList();

        if (candidates.Count == 0)
        {
            discovered.Issues.Add(
                $"No placeholder was found for action {CharacterAnimationNaming.GetActionDisplayName(discovered.Action)}.");
            return;
        }

        List<MotionCandidate> uniqueCandidates = new List<MotionCandidate>();
        foreach (IGrouping<AnimationClip, MotionCandidate> clipGroup in candidates.GroupBy(candidate => candidate.Clip))
        {
            if (clipGroup.Select(candidate => candidate.Source).Distinct().Count() > 1)
            {
                discovered.Issues.Add(
                    $"Source clip '{clipGroup.Key.name}' is used by multiple states or trees; overriding it could affect animations outside this action.");
                continue;
            }

            CharacterAnimationDirection?[] directions = clipGroup
                .Where(candidate => candidate.Direction.HasValue)
                .Select(candidate => candidate.Direction)
                .Distinct()
                .ToArray();

            if (directions.Length > 1)
            {
                discovered.Issues.Add(
                    $"Source clip '{clipGroup.Key.name}' appears in multiple directions within the same action.");
                continue;
            }

            MotionCandidate first = clipGroup.First();
            first.Direction = directions.Length == 1 ? directions[0] : null;
            first.FromBlendTree = clipGroup.Any(candidate => candidate.FromBlendTree);
            uniqueCandidates.Add(first);
        }

        bool hasBlendTreeCandidates = uniqueCandidates.Any(candidate => candidate.FromBlendTree);
        bool hasDirectCandidates = uniqueCandidates.Any(candidate => !candidate.FromBlendTree);
        discovered.UsesBlendTree = hasBlendTreeCandidates;

        if (hasBlendTreeCandidates && hasDirectCandidates)
        {
            discovered.Issues.Add(
                "The action uses both a Blend Tree and direct AnimationClip states, so a safe mapping cannot be determined.");
            return;
        }

        if (!hasBlendTreeCandidates && uniqueCandidates.Count == 1)
        {
            discovered.Issues.Add(
                $"Only one direct AnimationClip ('{uniqueCandidates[0].Clip.name}') was found. " +
                "This version requires four directional placeholders or four direct directional states.");
            return;
        }

        foreach (MotionCandidate candidate in uniqueCandidates)
        {
            if (!candidate.Direction.HasValue)
            {
                discovered.Issues.Add(
                    $"Could not determine the direction of placeholder '{candidate.Clip.name}' at {candidate.Source}.");
                continue;
            }

            if (!discovered.DirectionalPlaceholders.TryGetValue(
                    candidate.Direction.Value,
                    out List<AnimationClip> clips))
            {
                clips = new List<AnimationClip>();
                discovered.DirectionalPlaceholders[candidate.Direction.Value] = clips;
            }

            clips.Add(candidate.Clip);
        }

        foreach (CharacterAnimationDirection direction in CharacterAnimationNaming.Directions)
        {
            if (!discovered.DirectionalPlaceholders.TryGetValue(direction, out List<AnimationClip> clips) ||
                clips.Count == 0)
            {
                discovered.Issues.Add(
                    $"The {CharacterAnimationNaming.GetDirectionDisplayName(direction)} placeholder is missing.");
            }
            else if (clips.Count > 1)
            {
                discovered.Issues.Add(
                    $"Multiple placeholders were found for {CharacterAnimationNaming.GetDirectionDisplayName(direction)}. " +
                    "Keep exactly one placeholder to avoid overriding skill or effect animations.");
            }
        }

        if (discovered.Issues.Count == 0)
        {
            discovered.Layout = CharacterActionLayout.FourDirections;
        }
    }

    private static bool IsDirectionalBlendTree(BlendTreeType blendTreeType)
    {
        return blendTreeType == BlendTreeType.SimpleDirectional2D ||
               blendTreeType == BlendTreeType.FreeformDirectional2D ||
               blendTreeType == BlendTreeType.FreeformCartesian2D;
    }

    private static CharacterAnimationAction? TryInferAction(params string[] names)
    {
        foreach (string name in names)
        {
            HashSet<string> tokens = Tokenize(name);
            if (tokens.Contains("idle"))
            {
                return CharacterAnimationAction.Idle;
            }

            if (tokens.Overlaps(new[] { "walk", "walking", "run", "running", "move", "movement", "locomotion" }))
            {
                return CharacterAnimationAction.Walk;
            }

            if (tokens.Overlaps(new[] { "attack", "attacking", "strike" }))
            {
                return CharacterAnimationAction.Attack;
            }

            if (tokens.Overlaps(new[] { "hit", "hurt", "damage", "damaged" }))
            {
                return CharacterAnimationAction.Hit;
            }

            if (tokens.Overlaps(new[] { "death", "die", "dying", "dead" }))
            {
                return CharacterAnimationAction.Death;
            }
        }

        return null;
    }

    private static CharacterAnimationDirection? TryInferDirection(params string[] names)
    {
        foreach (string name in names)
        {
            HashSet<string> tokens = Tokenize(name);
            if (tokens.Overlaps(new[] { "bottom", "down", "south", "front" }))
            {
                return CharacterAnimationDirection.Bottom;
            }

            if (tokens.Overlaps(new[] { "left", "west" }))
            {
                return CharacterAnimationDirection.Left;
            }

            if (tokens.Overlaps(new[] { "right", "east" }))
            {
                return CharacterAnimationDirection.Right;
            }

            if (tokens.Overlaps(new[] { "top", "up", "north", "back" }))
            {
                return CharacterAnimationDirection.Top;
            }
        }

        return null;
    }

    private static CharacterAnimationDirection? TryInferDirection(Vector2 position)
    {
        const float epsilon = 0.0001f;
        if (Mathf.Abs(position.x) <= epsilon && Mathf.Abs(position.y) <= epsilon)
        {
            return null;
        }

        if (Mathf.Abs(position.x) > epsilon && Mathf.Abs(position.y) > epsilon)
        {
            return null;
        }

        if (Mathf.Abs(position.x) > epsilon)
        {
            return position.x < 0f
                ? CharacterAnimationDirection.Left
                : CharacterAnimationDirection.Right;
        }

        return position.y < 0f
            ? CharacterAnimationDirection.Bottom
            : CharacterAnimationDirection.Top;
    }

    private static HashSet<string> Tokenize(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return new HashSet<string>();
        }

        string expanded = CamelCaseBoundary.Replace(value, "$1 $2").ToLowerInvariant();
        return new HashSet<string>(
            TokenSeparator.Split(expanded).Where(token => !string.IsNullOrEmpty(token)));
    }
}

internal static class CharacterAnimationClipGenerator
{
    public static CharacterAnimationGenerationPlan BuildPlan(CharacterAnimationGenerationRequest request)
    {
        CharacterAnimationGenerationPlan plan = new CharacterAnimationGenerationPlan();

        if (request == null)
        {
            plan.Messages.Add(Error("The generation request is invalid."));
            return plan;
        }

        plan.OverrideController = request.OverrideController;
        plan.SampleRate = request.SampleRate;

        if (request.OverrideController == null)
        {
            plan.Messages.Add(Error("No Animator Override Controller is selected."));
        }

        if (request.SampleRate <= 0)
        {
            plan.Messages.Add(Error("Sample Rate must be greater than zero."));
        }

        string safeCharacterName = MakeSafeAssetName(request.CharacterName);
        if (string.IsNullOrEmpty(safeCharacterName))
        {
            plan.Messages.Add(Error("Character Name cannot be empty."));
        }
        else if (!string.Equals(
                     safeCharacterName,
                     request.CharacterName?.Trim(),
                     StringComparison.Ordinal))
        {
            plan.Messages.Add(Warning(
                $"Character Name contains unsupported characters. Generated assets will use '{safeCharacterName}'."));
        }

        string outputFolder = NormalizeAssetPath(request.OutputFolder);
        if (string.IsNullOrEmpty(outputFolder) ||
            !outputFolder.StartsWith("Assets", StringComparison.Ordinal) ||
            !AssetDatabase.IsValidFolder(outputFolder))
        {
            plan.Messages.Add(Error("The output folder must be a valid folder inside Assets."));
        }
        else if (!AssetDatabase.IsOpenForEdit(outputFolder, out string folderEditMessage))
        {
            plan.Messages.Add(Error(
                $"The output folder '{outputFolder}' is not writable: {folderEditMessage}"));
        }

        bool validDirectionOrder = ValidateDirectionOrder(request.DirectionOrder, plan.Messages);

        CharacterControllerScanResult scan =
            CharacterAnimatorPlaceholderScanner.Scan(request.OverrideController);
        plan.BaseController = scan.BaseController;
        plan.Messages.AddRange(scan.Messages);

        List<KeyValuePair<AnimationClip, AnimationClip>> currentOverrides =
            new List<KeyValuePair<AnimationClip, AnimationClip>>();
        HashSet<AnimationClip> overrideKeys = new HashSet<AnimationClip>();

        if (request.OverrideController != null)
        {
            string overridePath = AssetDatabase.GetAssetPath(request.OverrideController);
            if (string.IsNullOrEmpty(overridePath))
            {
                plan.Messages.Add(Error("The Animator Override Controller must be a saved project asset."));
            }
            else if (!AssetDatabase.IsOpenForEdit(overridePath, out string editMessage))
            {
                plan.Messages.Add(Error(
                    $"The Override Controller '{overridePath}' cannot be updated: {editMessage}"));
            }

            request.OverrideController.GetOverrides(currentOverrides);
            overrideKeys = new HashSet<AnimationClip>(
                currentOverrides.Select(pair => pair.Key).Where(clip => clip != null));
        }

        Dictionary<CharacterAnimationAction, CharacterActionSheetSettings> settingsByAction =
            (request.ActionSettings ?? Array.Empty<CharacterActionSheetSettings>())
            .Where(settings => settings != null)
            .GroupBy(settings => settings.Action)
            .ToDictionary(group => group.Key, group => group.First());

        Dictionary<AnimationClip, string> placeholderOwners =
            new Dictionary<AnimationClip, string>();

        foreach (CharacterAnimationAction action in CharacterAnimationNaming.Actions)
        {
            CharacterDiscoveredAction discovered = scan.Actions[action];
            string actionName = CharacterAnimationNaming.GetActionDisplayName(action);

            if (discovered.Layout == CharacterActionLayout.Invalid)
            {
                foreach (string issue in discovered.Issues)
                {
                    plan.Messages.Add(Error($"{actionName}: {issue}"));
                }
            }

            if (!settingsByAction.TryGetValue(action, out CharacterActionSheetSettings settings))
            {
                plan.Messages.Add(Error($"Spritesheet settings are missing for action {actionName}."));
                continue;
            }

            if (!TryLoadSprites(settings.SpriteSheet, action, plan.Messages, out List<Sprite> sprites))
            {
                continue;
            }

            AddSpriteConsistencyValidation(action, sprites, plan.Messages);

            if (discovered.Layout != CharacterActionLayout.FourDirections || !validDirectionOrder)
            {
                continue;
            }

            if (!TrySplitDirectionalSprites(
                    settings,
                    sprites,
                    request.DirectionOrder,
                    plan.Messages,
                    out Dictionary<CharacterAnimationDirection, List<Sprite>> splitSprites))
            {
                continue;
            }

            foreach (CharacterAnimationDirection direction in CharacterAnimationNaming.Directions)
            {
                AddClipPlan(
                    plan,
                    action,
                    direction,
                    safeCharacterName,
                    outputFolder,
                    splitSprites[direction],
                    discovered.DirectionalPlaceholders[direction],
                    overrideKeys,
                    placeholderOwners);
            }
        }

        ValidateOutputCollisions(plan, overrideKeys, currentOverrides);

        if (!plan.HasErrors)
        {
            plan.Messages.Add(new CharacterGeneratorMessage(
                CharacterGeneratorMessageType.Info,
                $"Valid: {plan.Clips.Count} clips and {plan.ManagedPlaceholderCount} mappings will be created or updated."));
        }

        return plan;
    }

    public static CharacterAnimationGenerationResult Generate(CharacterAnimationGenerationPlan plan)
    {
        if (plan == null)
        {
            throw new ArgumentNullException(nameof(plan));
        }

        if (plan.HasErrors)
        {
            throw new InvalidOperationException("Generation cannot run while validation errors remain.");
        }

        CharacterAnimationGenerationResult result = new CharacterAnimationGenerationResult();
        Dictionary<CharacterAnimationClipPlan, AnimationClip> generatedByPlan =
            new Dictionary<CharacterAnimationClipPlan, AnimationClip>();

        Undo.IncrementCurrentGroup();
        int undoGroup = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Generate Character Animation Clips");

        try
        {
            foreach (CharacterAnimationClipPlan clipPlan in plan.Clips)
            {
                AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(clipPlan.AssetPath);
                if (clip == null)
                {
                    clip = new AnimationClip
                    {
                        name = clipPlan.ClipName
                    };
                    AssetDatabase.CreateAsset(clip, clipPlan.AssetPath);
                    Undo.RegisterCreatedObjectUndo(clip, $"Create {clipPlan.ClipName}");
                    result.CreatedClipCount++;
                }
                else
                {
                    Undo.RecordObject(clip, $"Update {clipPlan.ClipName}");
                    result.UpdatedClipCount++;
                }

                WriteClipContent(clip, clipPlan, plan.SampleRate);
                generatedByPlan[clipPlan] = clip;
                result.GeneratedClips.Add(clip);
            }

            List<KeyValuePair<AnimationClip, AnimationClip>> managedOverrides =
                new List<KeyValuePair<AnimationClip, AnimationClip>>();

            foreach (CharacterAnimationClipPlan clipPlan in plan.Clips)
            {
                AnimationClip generatedClip = generatedByPlan[clipPlan];
                foreach (AnimationClip placeholder in clipPlan.Placeholders)
                {
                    managedOverrides.Add(
                        new KeyValuePair<AnimationClip, AnimationClip>(placeholder, generatedClip));
                }
            }

            Undo.RecordObject(plan.OverrideController, "Update Character Animation Overrides");
            plan.OverrideController.ApplyOverrides(managedOverrides);
            EditorUtility.SetDirty(plan.OverrideController);
            result.AppliedOverrideCount = managedOverrides.Count;

            AssetDatabase.SaveAssets();
            Undo.CollapseUndoOperations(undoGroup);
            return result;
        }
        catch
        {
            Undo.RevertAllDownToGroup(undoGroup);
            AssetDatabase.SaveAssets();
            throw;
        }
    }

    public static int GetSpriteCount(Texture2D texture)
    {
        if (texture == null)
        {
            return 0;
        }

        string path = AssetDatabase.GetAssetPath(texture);
        if (string.IsNullOrEmpty(path))
        {
            return 0;
        }

        return AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().Count();
    }

    public static string MakeSafeAssetName(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        HashSet<char> invalidCharacters = new HashSet<char>(Path.GetInvalidFileNameChars())
        {
            '/',
            '\\',
            ':'
        };

        char[] characters = value.Trim()
            .Select(character =>
                invalidCharacters.Contains(character) || char.IsWhiteSpace(character) ? '_' : character)
            .ToArray();

        string safeName = new string(characters);
        while (safeName.Contains("__"))
        {
            safeName = safeName.Replace("__", "_");
        }

        return safeName.Trim('_', '.');
    }

    private static void AddClipPlan(
        CharacterAnimationGenerationPlan plan,
        CharacterAnimationAction action,
        CharacterAnimationDirection? direction,
        string safeCharacterName,
        string outputFolder,
        IReadOnlyCollection<Sprite> sprites,
        IEnumerable<AnimationClip> placeholders,
        ISet<AnimationClip> overrideKeys,
        IDictionary<AnimationClip, string> placeholderOwners)
    {
        if (string.IsNullOrEmpty(safeCharacterName) ||
            string.IsNullOrEmpty(outputFolder) ||
            !AssetDatabase.IsValidFolder(outputFolder))
        {
            return;
        }

        string actionName = CharacterAnimationNaming.GetActionDisplayName(action);
        string slotName = direction.HasValue
            ? actionName + "/" + CharacterAnimationNaming.GetDirectionDisplayName(direction.Value)
            : actionName;

        List<AnimationClip> uniquePlaceholders = placeholders
            .Where(clip => clip != null)
            .Distinct()
            .ToList();

        foreach (AnimationClip placeholder in uniquePlaceholders)
        {
            if (!overrideKeys.Contains(placeholder))
            {
                plan.Messages.Add(Error(
                    $"{slotName}: placeholder '{placeholder.name}' is not a key in the Override Controller."));
            }

            if (placeholderOwners.TryGetValue(placeholder, out string existingOwner) &&
                !string.Equals(existingOwner, slotName, StringComparison.Ordinal))
            {
                plan.Messages.Add(Error(
                    $"Placeholder '{placeholder.name}' is assigned to both {existingOwner} and {slotName}."));
            }
            else
            {
                placeholderOwners[placeholder] = slotName;
            }
        }

        if (!TryResolveSpriteBinding(
                uniquePlaceholders,
                slotName,
                plan.Messages,
                out EditorCurveBinding spriteBinding))
        {
            return;
        }

        string clipName = safeCharacterName + "_" + actionName;
        if (direction.HasValue)
        {
            clipName += "_" + CharacterAnimationNaming.GetDirectionDisplayName(direction.Value);
        }

        CharacterAnimationClipPlan clipPlan = new CharacterAnimationClipPlan
        {
            Action = action,
            Direction = direction,
            ClipName = clipName,
            AssetPath = outputFolder.TrimEnd('/') + "/" + clipName + ".anim",
            Loop = CharacterAnimationNaming.ShouldLoop(action),
            SpriteBinding = spriteBinding
        };

        clipPlan.Sprites.AddRange(sprites);
        clipPlan.Placeholders.AddRange(uniquePlaceholders);
        plan.Clips.Add(clipPlan);
    }

    private static bool TryResolveSpriteBinding(
        IReadOnlyCollection<AnimationClip> placeholders,
        string slotName,
        ICollection<CharacterGeneratorMessage> messages,
        out EditorCurveBinding resolvedBinding)
    {
        EditorCurveBinding defaultBinding =
            EditorCurveBinding.PPtrCurve(string.Empty, typeof(SpriteRenderer), "m_Sprite");
        resolvedBinding = defaultBinding;
        bool hasResolvedBinding = false;
        bool valid = true;

        foreach (AnimationClip placeholder in placeholders)
        {
            EditorCurveBinding[] spriteBindings = AnimationUtility
                .GetObjectReferenceCurveBindings(placeholder)
                .Where(binding =>
                    binding.type == typeof(SpriteRenderer) &&
                    string.Equals(binding.propertyName, "m_Sprite", StringComparison.Ordinal))
                .ToArray();

            if (spriteBindings.Length > 1)
            {
                messages.Add(Error(
                    $"{slotName}: placeholder '{placeholder.name}' has multiple SpriteRenderer.m_Sprite bindings."));
                valid = false;
                continue;
            }

            EditorCurveBinding candidateBinding = spriteBindings.Length == 1
                ? spriteBindings[0]
                : defaultBinding;

            if (spriteBindings.Length == 0)
            {
                messages.Add(Warning(
                    $"{slotName}: placeholder '{placeholder.name}' has no sprite binding. A root SpriteRenderer binding will be used."));
            }

            if (!hasResolvedBinding)
            {
                resolvedBinding = candidateBinding;
                hasResolvedBinding = true;
                continue;
            }

            if (!BindingsMatch(resolvedBinding, candidateBinding))
            {
                messages.Add(Error(
                    $"{slotName}: the placeholders use different SpriteRenderer bindings."));
                valid = false;
            }
        }

        return valid;
    }

    private static bool BindingsMatch(EditorCurveBinding left, EditorCurveBinding right)
    {
        return left.type == right.type &&
               string.Equals(left.path, right.path, StringComparison.Ordinal) &&
               string.Equals(left.propertyName, right.propertyName, StringComparison.Ordinal);
    }

    private static bool TryLoadSprites(
        Texture2D texture,
        CharacterAnimationAction action,
        ICollection<CharacterGeneratorMessage> messages,
        out List<Sprite> sprites)
    {
        sprites = new List<Sprite>();
        string actionName = CharacterAnimationNaming.GetActionDisplayName(action);

        if (texture == null)
        {
            messages.Add(Error($"No spritesheet is selected for action {actionName}."));
            return false;
        }

        string texturePath = AssetDatabase.GetAssetPath(texture);
        TextureImporter importer = AssetImporter.GetAtPath(texturePath) as TextureImporter;
        if (importer == null ||
            importer.textureType != TextureImporterType.Sprite ||
            importer.spriteImportMode != SpriteImportMode.Multiple)
        {
            messages.Add(Error(
                $"{actionName}: the texture must be imported as Sprite (2D and UI) with Sprite Mode set to Multiple."));
            return false;
        }

        sprites = AssetDatabase.LoadAllAssetsAtPath(texturePath)
            .OfType<Sprite>()
            .Where(sprite => string.Equals(
                AssetDatabase.GetAssetPath(sprite),
                texturePath,
                StringComparison.Ordinal))
            .Distinct()
            .ToList();

        if (sprites.Count == 0)
        {
            messages.Add(Error($"{actionName}: the spritesheet is not sliced or contains no Sprite sub-assets."));
            return false;
        }

        return true;
    }

    private static void AddSpriteConsistencyValidation(
        CharacterAnimationAction action,
        IReadOnlyList<Sprite> sprites,
        ICollection<CharacterGeneratorMessage> messages)
    {
        if (sprites.Count == 0)
        {
            return;
        }

        string actionName = CharacterAnimationNaming.GetActionDisplayName(action);
        Vector2 expectedSize = sprites[0].rect.size;
        Vector2 expectedNormalizedPivot = GetNormalizedPivot(sprites[0]);

        bool inconsistentSize = sprites.Any(sprite =>
            Mathf.Abs(sprite.rect.width - expectedSize.x) > 0.01f ||
            Mathf.Abs(sprite.rect.height - expectedSize.y) > 0.01f);

        bool inconsistentPivot = sprites.Any(sprite =>
            Vector2.Distance(GetNormalizedPivot(sprite), expectedNormalizedPivot) > 0.001f);

        if (inconsistentSize)
        {
            messages.Add(Error(
                $"{actionName}: Sprite rect sizes are inconsistent. Slice the texture again using equal frame sizes."));
        }

        if (inconsistentPivot)
        {
            messages.Add(Error(
                $"{actionName}: Sprite pivots are inconsistent."));
        }
    }

    private static Vector2 GetNormalizedPivot(Sprite sprite)
    {
        Rect rect = sprite.rect;
        return new Vector2(
            rect.width <= 0f ? 0f : sprite.pivot.x / rect.width,
            rect.height <= 0f ? 0f : sprite.pivot.y / rect.height);
    }

    private static bool TrySplitDirectionalSprites(
        CharacterActionSheetSettings settings,
        IReadOnlyList<Sprite> sprites,
        IReadOnlyList<CharacterAnimationDirection> directionOrder,
        ICollection<CharacterGeneratorMessage> messages,
        out Dictionary<CharacterAnimationDirection, List<Sprite>> splitSprites)
    {
        splitSprites = new Dictionary<CharacterAnimationDirection, List<Sprite>>();
        string actionName = CharacterAnimationNaming.GetActionDisplayName(settings.Action);
        Dictionary<CharacterAnimationDirection, int> frameCounts =
            new Dictionary<CharacterAnimationDirection, int>();

        if (settings.SplitMode == CharacterFrameSplitMode.AutoEqual)
        {
            if (sprites.Count % CharacterAnimationNaming.Directions.Length != 0)
            {
                messages.Add(Error(
                    $"{actionName}: {sprites.Count} Sprites cannot be divided evenly by four in Auto Equal mode."));
                return false;
            }

            int equalCount = sprites.Count / CharacterAnimationNaming.Directions.Length;
            if (equalCount <= 0)
            {
                messages.Add(Error($"{actionName}: every direction must contain at least one frame."));
                return false;
            }

            foreach (CharacterAnimationDirection direction in CharacterAnimationNaming.Directions)
            {
                frameCounts[direction] = equalCount;
            }
        }
        else
        {
            int totalManualFrames = 0;
            foreach (CharacterAnimationDirection direction in CharacterAnimationNaming.Directions)
            {
                int count = settings.GetManualFrameCount(direction);
                frameCounts[direction] = count;
                totalManualFrames += count;

                if (count <= 0)
                {
                    messages.Add(Error(
                        $"{actionName}: the Manual Split count for {CharacterAnimationNaming.GetDirectionDisplayName(direction)} must be greater than zero."));
                }
            }

            if (totalManualFrames != sprites.Count)
            {
                messages.Add(Error(
                    $"{actionName}: the Manual Split total is {totalManualFrames}, but the spritesheet contains {sprites.Count} Sprites."));
            }

            if (messages.Any(message =>
                    message.Type == CharacterGeneratorMessageType.Error &&
                    message.Text.StartsWith(actionName + ":", StringComparison.Ordinal)))
            {
                return false;
            }
        }

        List<Sprite> verticalOrder = sprites
            .OrderByDescending(sprite => sprite.rect.center.y)
            .ThenBy(sprite => GetLocalFileId(sprite))
            .ToList();

        int offset = 0;
        bool ambiguousRows = false;
        for (int groupIndex = 0; groupIndex < directionOrder.Count; groupIndex++)
        {
            CharacterAnimationDirection direction = directionOrder[groupIndex];
            int count = frameCounts[direction];
            List<Sprite> row = verticalOrder
                .Skip(offset)
                .Take(count)
                .OrderBy(sprite => sprite.rect.center.x)
                .ThenByDescending(sprite => sprite.rect.center.y)
                .ThenBy(sprite => GetLocalFileId(sprite))
                .ToList();

            splitSprites[direction] = row;
            offset += count;

            if (groupIndex < directionOrder.Count - 1 &&
                offset > 0 &&
                offset < verticalOrder.Count &&
                Mathf.Abs(
                    verticalOrder[offset - 1].rect.center.y -
                    verticalOrder[offset].rect.center.y) <= 0.01f)
            {
                ambiguousRows = true;
            }
        }

        if (ambiguousRows)
        {
            messages.Add(Warning(
                $"{actionName}: adjacent sprite groups share the same Y coordinate. Review frame order before using the generated clips."));
        }

        return splitSprites.Count == CharacterAnimationNaming.Directions.Length &&
               splitSprites.All(pair => pair.Value.Count == frameCounts[pair.Key]);
    }

    private static long GetLocalFileId(Sprite sprite)
    {
        return AssetDatabase.TryGetGUIDAndLocalFileIdentifier(
            sprite,
            out string unusedGuid,
            out long localId)
            ? localId
            : sprite.GetInstanceID();
    }

    private static bool ValidateDirectionOrder(
        IReadOnlyList<CharacterAnimationDirection> directionOrder,
        ICollection<CharacterGeneratorMessage> messages)
    {
        HashSet<CharacterAnimationDirection> expectedDirections =
            new HashSet<CharacterAnimationDirection>(CharacterAnimationNaming.Directions);

        if (directionOrder == null ||
            directionOrder.Count != CharacterAnimationNaming.Directions.Length ||
            !expectedDirections.SetEquals(directionOrder))
        {
            messages.Add(Error(
                "Direction order must contain Bottom, Left, Right, and Top exactly once."));
            return false;
        }

        return true;
    }

    private static void ValidateOutputCollisions(
        CharacterAnimationGenerationPlan plan,
        ISet<AnimationClip> basePlaceholderClips,
        IReadOnlyCollection<KeyValuePair<AnimationClip, AnimationClip>> currentOverrides)
    {
        HashSet<string> outputPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        HashSet<AnimationClip> managedKeys = new HashSet<AnimationClip>(
            plan.Clips.SelectMany(clip => clip.Placeholders).Where(clip => clip != null));
        HashSet<AnimationClip> unmanagedOverrideValues = new HashSet<AnimationClip>(
            currentOverrides
                .Where(pair =>
                    pair.Key != null &&
                    !managedKeys.Contains(pair.Key) &&
                    pair.Value != null &&
                    pair.Value != pair.Key)
                .Select(pair => pair.Value));

        foreach (CharacterAnimationClipPlan clipPlan in plan.Clips)
        {
            if (!outputPaths.Add(clipPlan.AssetPath))
            {
                plan.Messages.Add(Error($"Duplicate output path: {clipPlan.AssetPath}"));
                continue;
            }

            UnityEngine.Object existingAsset = AssetDatabase.LoadMainAssetAtPath(clipPlan.AssetPath);
            if (existingAsset != null && !(existingAsset is AnimationClip))
            {
                plan.Messages.Add(Error(
                    $"A non-AnimationClip asset already exists at '{clipPlan.AssetPath}'."));
                continue;
            }

            AnimationClip existingClip = existingAsset as AnimationClip;
            if (existingClip == null)
            {
                string projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
                string physicalPath = Path.GetFullPath(
                    Path.Combine(projectRoot, clipPlan.AssetPath.Replace('/', Path.DirectorySeparatorChar)));

                if (File.Exists(physicalPath))
                {
                    plan.Messages.Add(Error(
                        $"A file exists at '{clipPlan.AssetPath}', but Unity cannot load it as an AnimationClip."));
                    continue;
                }

                string existingGuid = AssetDatabase.AssetPathToGUID(clipPlan.AssetPath);
                if (!string.IsNullOrEmpty(existingGuid))
                {
                    plan.Messages.Add(Error(
                        $"An output asset exists at '{clipPlan.AssetPath}', but it cannot be loaded for updating."));
                }

                continue;
            }

            if (basePlaceholderClips.Contains(existingClip))
            {
                plan.Messages.Add(Error(
                    $"Output '{clipPlan.AssetPath}' is a source clip used by the Base Controller. Generation is blocked to protect the Base Controller."));
                continue;
            }

            if (unmanagedOverrideValues.Contains(existingClip))
            {
                plan.Messages.Add(Error(
                    $"Output '{clipPlan.AssetPath}' is used by an override outside the five managed actions. " +
                    "Generation is blocked to preserve mappings outside this tool's scope."));
                continue;
            }

            if (!AssetDatabase.IsOpenForEdit(clipPlan.AssetPath, out string editMessage))
            {
                plan.Messages.Add(Error(
                    $"Clip '{clipPlan.AssetPath}' cannot be updated: {editMessage}"));
            }
        }
    }

    private static void WriteClipContent(
        AnimationClip clip,
        CharacterAnimationClipPlan clipPlan,
        int sampleRate)
    {
        clip.ClearCurves();
        clip.name = clipPlan.ClipName;
        clip.frameRate = sampleRate;

        ObjectReferenceKeyframe[] keyframes = clipPlan.Sprites
            .Select((sprite, index) => new ObjectReferenceKeyframe
            {
                time = index / (float)sampleRate,
                value = sprite
            })
            .ToArray();

        AnimationUtility.SetObjectReferenceCurve(clip, clipPlan.SpriteBinding, keyframes);

        AnimationClipSettings clipSettings = AnimationUtility.GetAnimationClipSettings(clip);
        clipSettings.startTime = 0f;
        clipSettings.stopTime = clipPlan.Sprites.Count / (float)sampleRate;
        clipSettings.loopTime = clipPlan.Loop;
        AnimationUtility.SetAnimationClipSettings(clip, clipSettings);

        EditorUtility.SetDirty(clip);
    }

    private static string NormalizeAssetPath(string path)
    {
        return string.IsNullOrWhiteSpace(path)
            ? string.Empty
            : path.Trim().Replace('\\', '/').TrimEnd('/');
    }

    private static CharacterGeneratorMessage Error(string text)
    {
        return new CharacterGeneratorMessage(CharacterGeneratorMessageType.Error, text);
    }

    private static CharacterGeneratorMessage Warning(string text)
    {
        return new CharacterGeneratorMessage(CharacterGeneratorMessageType.Warning, text);
    }
}

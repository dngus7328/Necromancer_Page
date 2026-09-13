using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(RibelController))]
public class RibelControllerEditor : Editor
{
    private enum PreviewMotion
    {
        Idle,
        Walk,
        Summon,
        Hit,
        Death
    }

    private enum PreviewDirection
    {
        Down,
        DownRight,
        Right,
        UpRight,
        Up
    }

    private PreviewMotion previewMotion =
        PreviewMotion.Idle;

    private PreviewDirection previewDirection =
        PreviewDirection.Down;

    private bool previewPlaying =
        true;

    private float previewSpeed =
        1f;

    private int previewFrame =
        0;

    private double lastPreviewTime;

    private SerializedProperty idleFrames;
    private SerializedProperty idleColumns;
    private SerializedProperty idleFrameRate;

    private SerializedProperty walkFrames;
    private SerializedProperty walkColumns;
    private SerializedProperty walkFrameRate;

    private SerializedProperty summonFrames;
    private SerializedProperty summonColumns;
    private SerializedProperty summonFrameRate;

    private SerializedProperty hitFrames;
    private SerializedProperty hitColumns;
    private SerializedProperty hitFrameRate;

    private SerializedProperty deathFrames;
    private SerializedProperty deathFrameRate;

    private void OnEnable()
    {
        idleFrames =
            serializedObject.FindProperty(
                "spriteFrames"
            );

        idleColumns =
            serializedObject.FindProperty(
                "idleColumns"
            );

        idleFrameRate =
            serializedObject.FindProperty(
                "idleFrameRate"
            );

        walkFrames =
            serializedObject.FindProperty(
                "walkFrames"
            );

        walkColumns =
            serializedObject.FindProperty(
                "walkColumns"
            );

        walkFrameRate =
            serializedObject.FindProperty(
                "walkFrameRate"
            );

        summonFrames =
            serializedObject.FindProperty(
                "summonFrames"
            );

        summonColumns =
            serializedObject.FindProperty(
                "summonColumns"
            );

        summonFrameRate =
            serializedObject.FindProperty(
                "summonFrameRate"
            );

        hitFrames =
            serializedObject.FindProperty(
                "hitFrames"
            );

        hitColumns =
            serializedObject.FindProperty(
                "hitColumns"
            );

        hitFrameRate =
            serializedObject.FindProperty(
                "hitFrameRate"
            );

        deathFrames =
            serializedObject.FindProperty(
                "deathFrames"
            );

        deathFrameRate =
            serializedObject.FindProperty(
                "deathFrameRate"
            );

        lastPreviewTime =
            EditorApplication.timeSinceStartup;
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        DrawDefaultInspector();

        EditorGUILayout.Space(12);

        DrawSeparator();

        EditorGUILayout.Space(6);

        EditorGUILayout.LabelField(
            "Animation Preview",
            EditorStyles.boldLabel
        );

        EditorGUILayout.Space(4);

        DrawMotionSelector();

        if (previewMotion !=
            PreviewMotion.Death)
        {
            DrawDirectionSelector();
        }

        DrawPlaybackControls();

        EditorGUILayout.Space(6);

        UpdatePreviewAnimation();

        DrawPreview();

        serializedObject.ApplyModifiedProperties();
    }

    private void DrawMotionSelector()
    {
        PreviewMotion newMotion =
            (PreviewMotion)
            EditorGUILayout.EnumPopup(
                "Motion",
                previewMotion
            );

        if (newMotion !=
            previewMotion)
        {
            previewMotion =
                newMotion;

            RestartPreview();
        }
    }

    private void DrawDirectionSelector()
    {
        PreviewDirection newDirection =
            (PreviewDirection)
            EditorGUILayout.EnumPopup(
                "Direction",
                previewDirection
            );

        if (newDirection !=
            previewDirection)
        {
            previewDirection =
                newDirection;

            RestartPreview();
        }
    }

    private void DrawPlaybackControls()
    {
        EditorGUILayout.Space(4);

        EditorGUILayout.BeginHorizontal();

        bool newPlaying =
            GUILayout.Toggle(
                previewPlaying,
                previewPlaying
                    ? "¡á Pause"
                    : "¢º Play",
                "Button"
            );

        if (newPlaying !=
            previewPlaying)
        {
            previewPlaying =
                newPlaying;

            lastPreviewTime =
                EditorApplication.timeSinceStartup;
        }

        if (GUILayout.Button(
                "Restart"
            ))
        {
            RestartPreview();
        }

        EditorGUILayout.EndHorizontal();

        previewSpeed =
            EditorGUILayout.Slider(
                "Preview Speed",
                previewSpeed,
                0.1f,
                3f
            );

        int columns =
            GetCurrentColumns();

        if (!previewPlaying &&
            columns > 0)
        {
            previewFrame =
                EditorGUILayout.IntSlider(
                    "Frame",
                    previewFrame,
                    0,
                    Mathf.Max(
                        0,
                        columns - 1
                    )
                );
        }
    }

    private void UpdatePreviewAnimation()
    {
        if (!previewPlaying)
        {
            return;
        }

        int columns =
            GetCurrentColumns();

        if (columns <= 0)
        {
            previewFrame =
                0;

            return;
        }

        float frameRate =
            Mathf.Max(
                0.01f,
                GetCurrentFrameRate()
            );

        double now =
            EditorApplication.timeSinceStartup;

        double delta =
            now -
            lastPreviewTime;

        double frameDuration =
            1.0 /
            (
                frameRate *
                previewSpeed
            );

        if (delta <
            frameDuration)
        {
            return;
        }

        int frameAdvance =
            Mathf.Max(
                1,
                Mathf.FloorToInt(
                    (float)(
                        delta /
                        frameDuration
                    )
                )
            );

        previewFrame +=
            frameAdvance;

        previewFrame %=
            columns;

        lastPreviewTime =
            now;

        Repaint();
    }

    private void DrawPreview()
    {
        Sprite previewSprite =
            GetPreviewSprite();

        Rect previewRect =
            GUILayoutUtility.GetRect(
                180f,
                260f,
                GUILayout.ExpandWidth(
                    true
                )
            );

        EditorGUI.DrawRect(
            previewRect,
            new Color(
                0.08f,
                0.08f,
                0.08f,
                1f
            )
        );

        if (previewSprite == null)
        {
            GUI.Label(
                previewRect,
                "Preview sprite ¾øÀ½",
                GetCenteredLabelStyle()
            );

            return;
        }

        Texture2D texture =
            previewSprite.texture;

        if (texture == null)
        {
            return;
        }

        Rect textureRect =
            previewSprite.textureRect;

        Rect uv =
            new Rect(
                textureRect.x /
                texture.width,

                textureRect.y /
                texture.height,

                textureRect.width /
                texture.width,

                textureRect.height /
                texture.height
            );

        float spriteAspect =
            textureRect.width /
            Mathf.Max(
                1f,
                textureRect.height
            );

        Rect drawRect =
            FitRect(
                previewRect,
                spriteAspect
            );

        GUI.DrawTextureWithTexCoords(
            drawRect,
            texture,
            uv,
            true
        );

        DrawPreviewInfo(
            previewRect
        );
    }

    private Sprite GetPreviewSprite()
    {
        SerializedProperty frames =
            GetCurrentFramesProperty();

        if (frames == null ||
            frames.arraySize <= 0)
        {
            return null;
        }

        if (previewMotion ==
            PreviewMotion.Death)
        {
            int deathIndex =
                Mathf.Clamp(
                    previewFrame,
                    0,
                    frames.arraySize - 1
                );

            return frames
                .GetArrayElementAtIndex(
                    deathIndex
                )
                .objectReferenceValue
                as Sprite;
        }

        int columns =
            GetCurrentColumns();

        if (columns <= 0)
        {
            return null;
        }

        int row =
            GetDirectionRow(
                previewDirection
            );

        int spriteIndex =
            row *
            columns +
            previewFrame;

        if (spriteIndex < 0 ||
            spriteIndex >=
            frames.arraySize)
        {
            return null;
        }

        return frames
            .GetArrayElementAtIndex(
                spriteIndex
            )
            .objectReferenceValue
            as Sprite;
    }

    private int GetDirectionRow(
        PreviewDirection direction)
    {
        switch (direction)
        {
            case PreviewDirection.Down:
                return 0;

            case PreviewDirection.DownRight:
                return 1;

            case PreviewDirection.Right:
                return 2;

            case PreviewDirection.UpRight:
                return 3;

            case PreviewDirection.Up:
                return 4;
        }

        return 0;
    }

    private SerializedProperty GetCurrentFramesProperty()
    {
        switch (previewMotion)
        {
            case PreviewMotion.Walk:
                return walkFrames;

            case PreviewMotion.Summon:
                return summonFrames;

            case PreviewMotion.Hit:
                return hitFrames;

            case PreviewMotion.Death:
                return deathFrames;

            default:
                return idleFrames;
        }
    }

    private int GetCurrentColumns()
    {
        switch (previewMotion)
        {
            case PreviewMotion.Walk:
                return Mathf.Max(
                    1,
                    walkColumns.intValue
                );

            case PreviewMotion.Summon:
                return Mathf.Max(
                    1,
                    summonColumns.intValue
                );

            case PreviewMotion.Hit:
                return Mathf.Max(
                    1,
                    hitColumns.intValue
                );

            case PreviewMotion.Death:
                return Mathf.Max(
                    1,
                    deathFrames != null
                        ? deathFrames.arraySize
                        : 1
                );

            default:
                return Mathf.Max(
                    1,
                    idleColumns.intValue
                );
        }
    }

    private float GetCurrentFrameRate()
    {
        switch (previewMotion)
        {
            case PreviewMotion.Walk:
                return walkFrameRate.floatValue;

            case PreviewMotion.Summon:
                return summonFrameRate.floatValue;

            case PreviewMotion.Hit:
                return hitFrameRate.floatValue;

            case PreviewMotion.Death:
                return deathFrameRate.floatValue;

            default:
                return idleFrameRate.floatValue;
        }
    }

    private void RestartPreview()
    {
        previewFrame =
            0;

        lastPreviewTime =
            EditorApplication.timeSinceStartup;

        Repaint();
    }

    private Rect FitRect(
        Rect area,
        float aspect)
    {
        float padding =
            18f;

        Rect inner =
            new Rect(
                area.x +
                padding,

                area.y +
                padding,

                area.width -
                padding *
                2f,

                area.height -
                padding *
                2f
            );

        float targetWidth =
            inner.width;

        float targetHeight =
            targetWidth /
            aspect;

        if (targetHeight >
            inner.height)
        {
            targetHeight =
                inner.height;

            targetWidth =
                targetHeight *
                aspect;
        }

        return new Rect(
            inner.center.x -
            targetWidth *
            0.5f,

            inner.center.y -
            targetHeight *
            0.5f,

            targetWidth,
            targetHeight
        );
    }

    private void DrawPreviewInfo(
        Rect previewRect)
    {
        string directionText =
            previewMotion ==
            PreviewMotion.Death
                ? "-"
                : previewDirection.ToString();

        string text =
            $"{previewMotion} | " +
            $"{directionText} | " +
            $"Frame {previewFrame + 1}" +
            $"/{GetCurrentColumns()}";

        Rect labelRect =
            new Rect(
                previewRect.x,
                previewRect.yMax - 22f,
                previewRect.width,
                20f
            );

        GUI.Label(
            labelRect,
            text,
            GetCenteredLabelStyle()
        );
    }

    private void DrawSeparator()
    {
        Rect rect =
            EditorGUILayout.GetControlRect(
                false,
                1f
            );

        EditorGUI.DrawRect(
            rect,
            new Color(
                0.35f,
                0.35f,
                0.35f,
                1f
            )
        );
    }

    private GUIStyle GetCenteredLabelStyle()
    {
        GUIStyle style =
            new GUIStyle(
                EditorStyles.miniLabel
            );

        style.alignment =
            TextAnchor.MiddleCenter;

        return style;
    }

    public override bool RequiresConstantRepaint()
    {
        return previewPlaying;
    }
}
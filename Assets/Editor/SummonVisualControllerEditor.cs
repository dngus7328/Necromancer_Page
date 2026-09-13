using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(SummonVisualController))]
public class SummonVisualControllerEditor : Editor
{
    private enum PreviewMotion
    {
        Idle,
        Walk,
        Attack,
        Hit,
        Death
    }

    private PreviewMotion previewMotion =
        PreviewMotion.Idle;

    private int previewDirectionIndex = 0;

    private bool previewPlaying = true;

    private float previewSpeedMultiplier = 1f;

    private int manualFrame = 0;

    private double previewStartTime;

    private void OnEnable()
    {
        previewStartTime =
            EditorApplication.timeSinceStartup;
    }

    public override bool RequiresConstantRepaint()
    {
        return previewPlaying;
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        DrawDefaultInspector();

        EditorGUILayout.Space(12f);
        EditorGUILayout.LabelField(
            "애니메이션 미리보기",
            EditorStyles.boldLabel
        );

        EditorGUILayout.HelpBox(
            "이 미리보기는 에디터용이야.\n" +
            "실제 게임 재생 전에도 Idle / Walk / Attack / Hit / Death를 확인할 수 있어.",
            MessageType.Info
        );

        previewMotion =
            (PreviewMotion)EditorGUILayout.EnumPopup(
                "미리보기 모션",
                previewMotion
            );

        bool isDirectional =
            previewMotion != PreviewMotion.Hit;

        int directionCount =
            GetDirectionCount();

        if (isDirectional)
        {
            string[] directionLabels =
                GetDirectionLabels();

            if (directionLabels.Length > 0)
            {
                previewDirectionIndex =
                    EditorGUILayout.Popup(
                        "방향",
                        Mathf.Clamp(
                            previewDirectionIndex,
                            0,
                            directionLabels.Length - 1
                        ),
                        directionLabels
                    );
            }
        }
        else
        {
            EditorGUILayout.LabelField(
                "방향",
                "HIT는 공통 프레임 사용"
            );

            previewDirectionIndex =
                0;
        }

        previewPlaying =
            EditorGUILayout.Toggle(
                "자동 재생",
                previewPlaying
            );

        previewSpeedMultiplier =
            EditorGUILayout.Slider(
                "미리보기 배속",
                previewSpeedMultiplier,
                0.1f,
                3f
            );

        if (GUILayout.Button("처음부터 재생"))
        {
            previewStartTime =
                EditorApplication.timeSinceStartup;
        }

        SerializedProperty framesProperty;
        int columns;
        float frameRate;
        bool valid =
            TryGetPreviewSource(
                out framesProperty,
                out columns,
                out frameRate
            );

        if (!valid ||
            framesProperty == null)
        {
            EditorGUILayout.HelpBox(
                "현재 모션의 프레임 데이터를 찾을 수 없어.",
                MessageType.Warning
            );

            serializedObject.ApplyModifiedProperties();
            return;
        }

        int availableFrames =
            framesProperty.arraySize;

        if (availableFrames <= 0)
        {
            EditorGUILayout.HelpBox(
                "이 모션에 등록된 스프라이트가 없어.",
                MessageType.Warning
            );

            serializedObject.ApplyModifiedProperties();
            return;
        }

        int maxFrameCount =
            GetPreviewFrameCount(
                availableFrames,
                columns,
                isDirectional,
                directionCount
            );

        if (maxFrameCount <= 0)
        {
            EditorGUILayout.HelpBox(
                "현재 설정 기준으로 프레임 계산이 불가능해.",
                MessageType.Warning
            );

            serializedObject.ApplyModifiedProperties();
            return;
        }

        if (!previewPlaying)
        {
            manualFrame =
                EditorGUILayout.IntSlider(
                    "프레임",
                    manualFrame,
                    0,
                    maxFrameCount - 1
                );
        }

        int currentFrame =
            GetCurrentFrameIndex(
                maxFrameCount,
                frameRate
            );

        Sprite previewSprite =
            GetPreviewSprite(
                framesProperty,
                columns,
                currentFrame,
                isDirectional,
                directionCount
            );

        EditorGUILayout.Space(8f);

        DrawPreviewInfo(
            availableFrames,
            columns,
            frameRate,
            maxFrameCount,
            currentFrame,
            directionCount,
            isDirectional
        );

        EditorGUILayout.Space(6f);

        DrawSpritePreview(
            previewSprite,
            180f
        );

        serializedObject.ApplyModifiedProperties();
    }

    private int GetDirectionCount()
    {
        SerializedProperty directionModeProp =
            serializedObject.FindProperty(
                "directionMode"
            );

        if (directionModeProp == null)
        {
            return 3;
        }

        // 0 = ThreeDirections
        // 1 = FiveDirections
        return directionModeProp.enumValueIndex == 1
            ? 5
            : 3;
    }

    private string[] GetDirectionLabels()
    {
        int directionCount =
            GetDirectionCount();

        if (directionCount == 5)
        {
            return new string[]
            {
                "아래",
                "우하",
                "오른쪽",
                "우상",
                "위"
            };
        }

        return new string[]
        {
            "아래",
            "오른쪽",
            "위"
        };
    }

    private bool TryGetPreviewSource(
        out SerializedProperty framesProperty,
        out int columns,
        out float frameRate
    )
    {
        framesProperty = null;
        columns = 1;
        frameRate = 1f;

        switch (previewMotion)
        {
            case PreviewMotion.Idle:
                framesProperty =
                    serializedObject.FindProperty(
                        "idleFrames"
                    );
                columns =
                    GetIntProperty(
                        "idleColumns",
                        1
                    );
                frameRate =
                    GetFloatProperty(
                        "idleFrameRate",
                        1f
                    );
                return true;

            case PreviewMotion.Walk:
                framesProperty =
                    serializedObject.FindProperty(
                        "walkFrames"
                    );
                columns =
                    GetIntProperty(
                        "walkColumns",
                        1
                    );
                frameRate =
                    GetFloatProperty(
                        "walkFrameRate",
                        1f
                    );
                return true;

            case PreviewMotion.Attack:
                framesProperty =
                    serializedObject.FindProperty(
                        "attackFrames"
                    );
                columns =
                    GetIntProperty(
                        "attackColumns",
                        1
                    );
                frameRate =
                    GetFloatProperty(
                        "attackFrameRate",
                        1f
                    );
                return true;

            case PreviewMotion.Hit:
                framesProperty =
                    serializedObject.FindProperty(
                        "hitFrames"
                    );
                columns = 1;
                frameRate =
                    GetFloatProperty(
                        "hitFrameRate",
                        1f
                    );
                return true;

            case PreviewMotion.Death:
                framesProperty =
                    serializedObject.FindProperty(
                        "deathFrames"
                    );
                columns =
                    GetIntProperty(
                        "deathColumns",
                        1
                    );
                frameRate =
                    GetFloatProperty(
                        "deathFrameRate",
                        1f
                    );
                return true;
        }

        return false;
    }

    private int GetPreviewFrameCount(
        int availableFrames,
        int columns,
        bool isDirectional,
        int directionCount
    )
    {
        if (!isDirectional)
        {
            return availableFrames;
        }

        if (columns <= 0)
        {
            return 0;
        }

        int required =
            columns * directionCount;

        if (availableFrames < required)
        {
            return Mathf.Max(
                0,
                availableFrames / directionCount
            );
        }

        return columns;
    }

    private int GetCurrentFrameIndex(
        int maxFrameCount,
        float frameRate
    )
    {
        if (maxFrameCount <= 0)
        {
            return 0;
        }

        if (!previewPlaying)
        {
            return Mathf.Clamp(
                manualFrame,
                0,
                maxFrameCount - 1
            );
        }

        double elapsed =
            EditorApplication.timeSinceStartup -
            previewStartTime;

        float effectiveRate =
            Mathf.Max(
                0.01f,
                frameRate * previewSpeedMultiplier
            );

        int frame =
            Mathf.FloorToInt(
                (float)elapsed * effectiveRate
            );

        frame %= maxFrameCount;

        if (frame < 0)
        {
            frame = 0;
        }

        return frame;
    }

    private Sprite GetPreviewSprite(
        SerializedProperty framesProperty,
        int columns,
        int currentFrame,
        bool isDirectional,
        int directionCount
    )
    {
        if (framesProperty == null ||
            framesProperty.arraySize <= 0)
        {
            return null;
        }

        int index = 0;

        if (!isDirectional)
        {
            index =
                Mathf.Clamp(
                    currentFrame,
                    0,
                    framesProperty.arraySize - 1
                );
        }
        else
        {
            int row =
                Mathf.Clamp(
                    previewDirectionIndex,
                    0,
                    directionCount - 1
                );

            index =
                row * columns +
                Mathf.Clamp(
                    currentFrame,
                    0,
                    columns - 1
                );

            if (index >= framesProperty.arraySize)
            {
                return null;
            }
        }

        SerializedProperty element =
            framesProperty.GetArrayElementAtIndex(
                index
            );

        return element.objectReferenceValue as Sprite;
    }

    private void DrawPreviewInfo(
        int availableFrames,
        int columns,
        float frameRate,
        int maxFrameCount,
        int currentFrame,
        int directionCount,
        bool isDirectional
    )
    {
        EditorGUILayout.BeginVertical("box");

        EditorGUILayout.LabelField(
            "현재 정보",
            EditorStyles.boldLabel
        );

        EditorGUILayout.LabelField(
            "등록된 총 스프라이트 수",
            availableFrames.ToString()
        );

        if (isDirectional)
        {
            EditorGUILayout.LabelField(
                "방향 수",
                directionCount.ToString()
            );

            EditorGUILayout.LabelField(
                "방향당 프레임 수",
                columns.ToString()
            );
        }
        else
        {
            EditorGUILayout.LabelField(
                "방향 수",
                "공통"
            );

            EditorGUILayout.LabelField(
                "프레임 수",
                availableFrames.ToString()
            );
        }

        EditorGUILayout.LabelField(
            "재생 속도",
            frameRate.ToString("0.##") + " fps"
        );

        EditorGUILayout.LabelField(
            "현재 프레임",
            (currentFrame + 1).ToString() +
            " / " +
            maxFrameCount.ToString()
        );

        EditorGUILayout.EndVertical();
    }

    private void DrawSpritePreview(
        Sprite sprite,
        float size
    )
    {
        Rect rect =
            GUILayoutUtility.GetRect(
                size,
                size,
                GUILayout.ExpandWidth(true)
            );

        EditorGUI.DrawRect(
            rect,
            new Color(
                0.16f,
                0.16f,
                0.16f,
                1f
            )
        );

        if (sprite == null)
        {
            GUIStyle centered =
                new GUIStyle(
                    EditorStyles.whiteLabel
                );

            centered.alignment =
                TextAnchor.MiddleCenter;

            EditorGUI.LabelField(
                rect,
                "미리볼 Sprite 없음",
                centered
            );

            return;
        }

        Texture2D texture =
            sprite.texture;

        Rect textureRect =
            sprite.textureRect;

        Rect uv =
            new Rect(
                textureRect.x / texture.width,
                textureRect.y / texture.height,
                textureRect.width / texture.width,
                textureRect.height / texture.height
            );

        float spriteWidth =
            textureRect.width;

        float spriteHeight =
            textureRect.height;

        if (spriteWidth <= 0f ||
            spriteHeight <= 0f)
        {
            return;
        }

        float aspect =
            spriteWidth / spriteHeight;

        float drawWidth =
            rect.width;

        float drawHeight =
            drawWidth / aspect;

        if (drawHeight > rect.height)
        {
            drawHeight =
                rect.height;

            drawWidth =
                drawHeight * aspect;
        }

        Rect drawRect =
            new Rect(
                rect.x + (rect.width - drawWidth) * 0.5f,
                rect.y + (rect.height - drawHeight) * 0.5f,
                drawWidth,
                drawHeight
            );

        GUI.DrawTextureWithTexCoords(
            drawRect,
            texture,
            uv,
            true
        );
    }

    private int GetIntProperty(
        string propertyName,
        int fallback
    )
    {
        SerializedProperty prop =
            serializedObject.FindProperty(
                propertyName
            );

        if (prop == null)
        {
            return fallback;
        }

        return Mathf.Max(
            1,
            prop.intValue
        );
    }

    private float GetFloatProperty(
        string propertyName,
        float fallback
    )
    {
        SerializedProperty prop =
            serializedObject.FindProperty(
                propertyName
            );

        if (prop == null)
        {
            return fallback;
        }

        return Mathf.Max(
            0.01f,
            prop.floatValue
        );
    }
}
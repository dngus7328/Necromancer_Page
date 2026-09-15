using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(AttackEffectEmitter))]
public class AttackEffectEmitterEditor : Editor
{
    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        DrawDefaultInspector();

        serializedObject.ApplyModifiedProperties();

        EditorGUILayout.Space(12f);

        DrawSeparator();

        EditorGUILayout.Space(8f);

        EditorGUILayout.LabelField(
            "공격 이펙트 미리보기",
            EditorStyles.boldLabel
        );

        EditorGUILayout.HelpBox(
            "같은 모양을 유지한 채 Enemy / Summon 색상만 바꿔서 확인할 수 있습니다.\n" +
            "아래 미리보기는 현재 Inspector 값이 바로 반영됩니다.",
            MessageType.Info
        );

        AttackEffectEmitter emitter =
            (AttackEffectEmitter)target;

        DrawPreview(emitter);

        EditorGUILayout.Space(8f);

        using (new EditorGUI.DisabledScope(!Application.isPlaying))
        {
            if (GUILayout.Button("플레이 중 미리보기 이펙트 생성"))
            {
                emitter.PlayPreviewEffect();
            }
        }
    }

    // =========================================================
    // 구분선
    // =========================================================

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

    // =========================================================
    // 미리보기
    // =========================================================

    private void DrawPreview(
        AttackEffectEmitter emitter)
    {
        Rect rect =
            GUILayoutUtility.GetRect(
                260f,
                260f,
                GUILayout.ExpandWidth(true)
            );

        EditorGUI.DrawRect(
            rect,
            new Color(
                0.12f,
                0.12f,
                0.12f,
                1f
            )
        );

        DrawBorder(rect);

        Vector2 center =
            rect.center;

        float maxExtentX =
            Mathf.Max(
                1f,
                emitter.SpawnDistance +
                emitter.EffectWorldSize.x * 0.5f +
                0.6f
            );

        float maxExtentY =
            Mathf.Max(
                1f,
                emitter.SpawnDistance +
                emitter.EffectWorldSize.y * 0.5f +
                0.6f
            );

        float scaleX =
            (rect.width - 50f) /
            (maxExtentX * 2f);

        float scaleY =
            (rect.height - 70f) /
            (maxExtentY * 2f);

        float scale =
            Mathf.Min(scaleX, scaleY);

        scale =
            Mathf.Clamp(scale, 30f, 120f);

        // =====================================================
        // 본체 표시
        // =====================================================

        float ownerSize = 26f;

        Rect ownerRect =
            new Rect(
                center.x - ownerSize * 0.5f,
                center.y - ownerSize * 0.5f,
                ownerSize,
                ownerSize
            );

        EditorGUI.DrawRect(
            ownerRect,
            new Color(
                0.78f,
                0.78f,
                0.78f,
                1f
            )
        );

        GUIStyle ownerStyle =
            new GUIStyle(EditorStyles.boldLabel);

        ownerStyle.alignment =
            TextAnchor.MiddleCenter;

        ownerStyle.normal.textColor =
            Color.black;

        GUI.Label(
            ownerRect,
            "본체",
            ownerStyle
        );

        // =====================================================
        // 방향 계산
        // =====================================================

        Vector2 direction =
            emitter.GetPreviewDirectionVector();

        Vector2 screenDirection =
            new Vector2(
                direction.x,
                -direction.y
            );

        Vector2 effectCenter =
            center +
            screenDirection *
            emitter.SpawnDistance *
            scale;

        Vector2 effectSize =
            emitter.EffectWorldSize *
            scale;

        // =====================================================
        // 방향선
        // =====================================================

        Handles.BeginGUI();

        Handles.color =
            new Color(
                1f,
                0.85f,
                0.2f,
                1f
            );

        Handles.DrawAAPolyLine(
            2.5f,
            center,
            effectCenter
        );

        Handles.EndGUI();

        // =====================================================
        // 이펙트 미리보기
        // =====================================================

        Texture2D texture =
            emitter.GetPreviewTexture();

        if (texture != null)
        {
            Rect effectRect =
                new Rect(
                    effectCenter.x - effectSize.x * 0.5f,
                    effectCenter.y - effectSize.y * 0.5f,
                    effectSize.x,
                    effectSize.y
                );

            Matrix4x4 oldMatrix =
                GUI.matrix;

            GUIUtility.RotateAroundPivot(
                emitter.GetPreviewGuiAngle(),
                effectCenter
            );

            Color oldColor =
                GUI.color;

            GUI.color =
                emitter.GetPreviewColor();

            GUI.DrawTexture(
                effectRect,
                texture,
                ScaleMode.StretchToFill,
                true
            );

            GUI.color =
                oldColor;

            GUI.matrix =
                oldMatrix;
        }

        // =====================================================
        // 하단 정보
        // =====================================================

        Rect infoRect =
            new Rect(
                rect.x + 8f,
                rect.yMax - 46f,
                rect.width - 16f,
                40f
            );

        GUIStyle infoStyle =
            new GUIStyle(EditorStyles.miniLabel);

        infoStyle.normal.textColor =
            new Color(
                0.9f,
                0.9f,
                0.9f,
                1f
            );

        string ownerName =
            emitter.CurrentPreviewOwner ==
            AttackEffectEmitter.OwnerType.Enemy
                ? "Enemy"
                : "Summon";

        string dirName =
            emitter.CurrentPreviewDirection.ToString();

        GUI.Label(
            infoRect,
            $"Preview : {ownerName} / {dirName}\n" +
            $"Size : {emitter.EffectWorldSize.x:0.00} × {emitter.EffectWorldSize.y:0.00}   " +
            $"Distance : {emitter.SpawnDistance:0.00}   " +
            $"Life : {emitter.Lifetime:0.00}",
            infoStyle
        );
    }

    private void DrawBorder(Rect rect)
    {
        Handles.BeginGUI();

        Handles.color =
            new Color(
                0.4f,
                0.4f,
                0.4f,
                1f
            );

        Handles.DrawAAPolyLine(
            2f,
            new Vector3(rect.xMin, rect.yMin),
            new Vector3(rect.xMax, rect.yMin),
            new Vector3(rect.xMax, rect.yMax),
            new Vector3(rect.xMin, rect.yMax),
            new Vector3(rect.xMin, rect.yMin)
        );

        Handles.EndGUI();
    }
}
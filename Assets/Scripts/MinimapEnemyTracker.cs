using System.Collections.Generic;
using UnityEngine;

public class MinimapEnemyTracker : MonoBehaviour
{
    [Header("미니맵 영역")]
    [SerializeField] private RectTransform minimapRect;

    [SerializeField] private RectTransform unitRoot;

    [Header("점 Prefab")]
    [SerializeField] private GameObject ribelDotPrefab;
    [SerializeField] private GameObject summonDotPrefab;
    [SerializeField] private GameObject enemyDotPrefab;

    [Header("리벨")]
    [SerializeField] private Transform ribel;

    [Header("미니맵 월드 표시 범위")]
    [Tooltip("리벨 기준 미니맵 전체 가로 표시 범위")]
    [SerializeField] private float worldWidth = 28f;

    [Tooltip("리벨 기준 미니맵 전체 세로 표시 범위")]
    [SerializeField] private float worldHeight = 18f;

    [Header("갱신")]
    [Tooltip("새 유닛 생성/사망 확인 주기")]
    [SerializeField] private float refreshInterval = 0.1f;

    private RectTransform ribelDot;

    private float refreshTimer;

    private readonly Dictionary<SummonUnitBase, RectTransform> summonDots =
        new Dictionary<SummonUnitBase, RectTransform>();

    private readonly Dictionary<EnemyUnitBase, RectTransform> enemyDots =
        new Dictionary<EnemyUnitBase, RectTransform>();

    private void Start()
    {
        FindRibel();

        CreateRibelDot();

        RefreshAllUnits();
    }

    private void Update()
    {
        refreshTimer -= Time.deltaTime;

        if (refreshTimer <= 0f)
        {
            refreshTimer = refreshInterval;

            RefreshAllUnits();
        }

        UpdateRibelDot();
        UpdateSummonDots();
        UpdateEnemyDots();
    }

    // =========================================================
    // 리벨 찾기
    // =========================================================

    private void FindRibel()
    {
        if (ribel != null)
        {
            return;
        }

        GameObject ribelObject =
            GameObject.FindGameObjectWithTag("Player");

        if (ribelObject != null)
        {
            ribel = ribelObject.transform;
        }
    }

    // =========================================================
    // 리벨 점 생성
    // =========================================================

    private void CreateRibelDot()
    {
        if (ribelDotPrefab == null ||
            unitRoot == null ||
            ribelDot != null)
        {
            return;
        }

        GameObject dotObject =
            Instantiate(
                ribelDotPrefab,
                unitRoot
            );

        ribelDot =
            dotObject.GetComponent<RectTransform>();
    }

    // =========================================================
    // 전체 유닛 갱신
    // =========================================================

    private void RefreshAllUnits()
    {
        RefreshSummons();
        RefreshEnemies();
    }

    // =========================================================
    // 소환수 목록
    // =========================================================

    private void RefreshSummons()
    {
        SummonUnitBase[] summons =
            FindObjectsOfType<SummonUnitBase>();

        for (int i = 0; i < summons.Length; i++)
        {
            SummonUnitBase summon =
                summons[i];

            if (summon == null)
            {
                continue;
            }

            if (!summonDots.ContainsKey(summon))
            {
                CreateSummonDot(summon);
            }
        }

        List<SummonUnitBase> removeList =
            new List<SummonUnitBase>();

        foreach (
            KeyValuePair<SummonUnitBase, RectTransform> pair
            in summonDots)
        {
            if (pair.Key == null)
            {
                if (pair.Value != null)
                {
                    Destroy(pair.Value.gameObject);
                }

                removeList.Add(pair.Key);
            }
        }

        for (int i = 0; i < removeList.Count; i++)
        {
            summonDots.Remove(removeList[i]);
        }
    }

    private void CreateSummonDot(
        SummonUnitBase summon)
    {
        if (summonDotPrefab == null ||
            unitRoot == null)
        {
            return;
        }

        GameObject dotObject =
            Instantiate(
                summonDotPrefab,
                unitRoot
            );

        RectTransform dot =
            dotObject.GetComponent<RectTransform>();

        if (dot != null)
        {
            summonDots.Add(
                summon,
                dot
            );
        }
    }

    // =========================================================
    // 적 목록
    // =========================================================

    private void RefreshEnemies()
    {
        EnemyUnitBase[] enemies =
            FindObjectsOfType<EnemyUnitBase>();

        for (int i = 0; i < enemies.Length; i++)
        {
            EnemyUnitBase enemy =
                enemies[i];

            if (enemy == null)
            {
                continue;
            }

            if (!enemyDots.ContainsKey(enemy))
            {
                CreateEnemyDot(enemy);
            }
        }

        List<EnemyUnitBase> removeList =
            new List<EnemyUnitBase>();

        foreach (
            KeyValuePair<EnemyUnitBase, RectTransform> pair
            in enemyDots)
        {
            if (pair.Key == null)
            {
                if (pair.Value != null)
                {
                    Destroy(pair.Value.gameObject);
                }

                removeList.Add(pair.Key);
            }
        }

        for (int i = 0; i < removeList.Count; i++)
        {
            enemyDots.Remove(removeList[i]);
        }
    }

    private void CreateEnemyDot(
        EnemyUnitBase enemy)
    {
        if (enemyDotPrefab == null ||
            unitRoot == null)
        {
            return;
        }

        GameObject dotObject =
            Instantiate(
                enemyDotPrefab,
                unitRoot
            );

        RectTransform dot =
            dotObject.GetComponent<RectTransform>();

        if (dot != null)
        {
            enemyDots.Add(
                enemy,
                dot
            );
        }
    }

    // =========================================================
    // 리벨 점
    // =========================================================

    private void UpdateRibelDot()
    {
        if (ribelDot == null)
        {
            return;
        }

        // 리벨 중심형 미니맵
        ribelDot.anchoredPosition =
            Vector2.zero;
    }

    // =========================================================
    // 소환수 점
    // =========================================================

    private void UpdateSummonDots()
    {
        if (ribel == null)
        {
            FindRibel();

            if (ribel == null)
            {
                return;
            }
        }

        foreach (
            KeyValuePair<SummonUnitBase, RectTransform> pair
            in summonDots)
        {
            if (pair.Key == null ||
                pair.Value == null)
            {
                continue;
            }

            Vector2 relative =
                (Vector2)pair.Key.transform.position -
                (Vector2)ribel.position;

            // 소환수는 현재 기존처럼 미니맵 안쪽으로 제한
            Vector2 minimapPosition =
                RelativeToMinimapPositionClamped(
                    relative
                );

            pair.Value.gameObject.SetActive(true);

            pair.Value.anchoredPosition =
                minimapPosition;
        }
    }

    // =========================================================
    // 적 점
    // =========================================================

    private void UpdateEnemyDots()
    {
        if (ribel == null)
        {
            FindRibel();

            if (ribel == null)
            {
                return;
            }
        }

        float halfWorldWidth =
            worldWidth * 0.5f;

        float halfWorldHeight =
            worldHeight * 0.5f;

        foreach (
            KeyValuePair<EnemyUnitBase, RectTransform> pair
            in enemyDots)
        {
            if (pair.Key == null ||
                pair.Value == null)
            {
                continue;
            }

            Vector2 relative =
                (Vector2)pair.Key.transform.position -
                (Vector2)ribel.position;

            // =================================================
            // 핵심:
            // 미니맵 범위 밖 적은 테두리에 붙이지 않고 숨긴다.
            // =================================================

            bool outside =
                Mathf.Abs(relative.x) >
                    halfWorldWidth ||
                Mathf.Abs(relative.y) >
                    halfWorldHeight;

            if (outside)
            {
                pair.Value.gameObject.SetActive(false);
                continue;
            }

            // 리벨이 가까워져 다시 범위 안에 들어오면 표시
            if (!pair.Value.gameObject.activeSelf)
            {
                pair.Value.gameObject.SetActive(true);
            }

            pair.Value.anchoredPosition =
                RelativeToMinimapPosition(
                    relative
                );
        }
    }

    // =========================================================
    // 범위 내 좌표 → 미니맵
    // =========================================================

    private Vector2 RelativeToMinimapPosition(
        Vector2 relative)
    {
        if (minimapRect == null)
        {
            return Vector2.zero;
        }

        float halfWorldWidth =
            worldWidth * 0.5f;

        float halfWorldHeight =
            worldHeight * 0.5f;

        float normalizedX =
            relative.x /
            halfWorldWidth;

        float normalizedY =
            relative.y /
            halfWorldHeight;

        float halfMinimapWidth =
            minimapRect.rect.width *
            0.5f;

        float halfMinimapHeight =
            minimapRect.rect.height *
            0.5f;

        return new Vector2(
            normalizedX *
                halfMinimapWidth,

            normalizedY *
                halfMinimapHeight
        );
    }

    // =========================================================
    // Clamp 방식
    // 소환수 등에 사용
    // =========================================================

    private Vector2 RelativeToMinimapPositionClamped(
        Vector2 relative)
    {
        if (minimapRect == null)
        {
            return Vector2.zero;
        }

        float halfWorldWidth =
            worldWidth * 0.5f;

        float halfWorldHeight =
            worldHeight * 0.5f;

        float normalizedX =
            relative.x /
            halfWorldWidth;

        float normalizedY =
            relative.y /
            halfWorldHeight;

        normalizedX =
            Mathf.Clamp(
                normalizedX,
                -1f,
                1f
            );

        normalizedY =
            Mathf.Clamp(
                normalizedY,
                -1f,
                1f
            );

        float halfMinimapWidth =
            minimapRect.rect.width *
            0.5f;

        float halfMinimapHeight =
            minimapRect.rect.height *
            0.5f;

        return new Vector2(
            normalizedX *
                halfMinimapWidth,

            normalizedY *
                halfMinimapHeight
        );
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        worldWidth =
            Mathf.Max(
                0.1f,
                worldWidth
            );

        worldHeight =
            Mathf.Max(
                0.1f,
                worldHeight
            );

        refreshInterval =
            Mathf.Max(
                0.02f,
                refreshInterval
            );
    }
#endif
}
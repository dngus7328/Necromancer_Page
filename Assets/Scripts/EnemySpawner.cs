using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    [Header("생성할 적")]
    [SerializeField] private GameObject enemyPrefab;

    [Header("테스트 무한 스폰")]
    [Tooltip("켜면 적을 계속 보충합니다.")]
    [SerializeField] private bool endlessTestMode = true;

    [Tooltip("동시에 살아있을 수 있는 최대 적")]
    [SerializeField] private int maxAliveEnemies = 20;

    [Tooltip("새 적 생성 간격")]
    [SerializeField] private float spawnInterval = 0.5f;

    [Header("일반 테스트")]
    [Tooltip("무한 모드가 꺼져 있을 때 생성할 총 적 수")]
    [SerializeField] private int spawnCount = 5;

    [Header("카메라 바깥 스폰")]
    [SerializeField] private float minOutsideDistance = 1.5f;
    [SerializeField] private float maxOutsideDistance = 4f;

    [Header("안전 거리")]
    [SerializeField] private float minDistanceFromRibel = 4f;
    [SerializeField] private float minDistanceFromSummons = 2f;

    [Header("소환수")]
    [SerializeField] private LayerMask summonLayer;

    [Header("장애물")]
    [SerializeField] private LayerMask blockedLayers;
    [SerializeField] private float spawnCheckRadius = 0.4f;

    [Header("스폰 위치 탐색")]
    [SerializeField] private int maxSpawnAttempts = 50;

    [Header("시작")]
    [Tooltip("Play 시작과 동시에 스폰 시작")]
    [SerializeField] private bool spawnOnStart = true;

    private Camera mainCamera;
    private Transform ribel;

    private float spawnTimer;

    private int remainingSpawnCount;
    private bool spawning;

    private void Awake()
    {
        mainCamera = Camera.main;

        FindRibel();
    }

    private void Start()
    {
        if (spawnOnStart)
        {
            StartSpawnWave();
        }
    }

    private void Update()
    {
        if (!spawning)
        {
            return;
        }

        spawnTimer -= Time.deltaTime;

        if (spawnTimer > 0f)
        {
            return;
        }

        spawnTimer = spawnInterval;

        if (endlessTestMode)
        {
            UpdateEndlessSpawn();
        }
        else
        {
            UpdateNormalSpawn();
        }
    }

    // =========================================================
    // 스폰 시작
    // =========================================================

    public void StartSpawnWave()
    {
        if (enemyPrefab == null)
        {
            Debug.LogWarning(
                $"{gameObject.name}: Enemy Prefab이 없습니다."
            );

            return;
        }

        FindRibel();

        remainingSpawnCount =
            spawnCount;

        spawnTimer = 0f;
        spawning = true;
    }

    // =========================================================
    // 무한 테스트
    // =========================================================

    private void UpdateEndlessSpawn()
    {
        int aliveEnemies =
            FindObjectsOfType<EnemyUnitBase>().Length;

        if (aliveEnemies >= maxAliveEnemies)
        {
            return;
        }

        TrySpawnEnemy();
    }

    // =========================================================
    // 일반 스폰
    // =========================================================

    private void UpdateNormalSpawn()
    {
        if (remainingSpawnCount <= 0)
        {
            spawning = false;
            return;
        }

        if (TrySpawnEnemy())
        {
            remainingSpawnCount--;
        }
    }

    // =========================================================
    // 적 생성
    // =========================================================

    private bool TrySpawnEnemy()
    {
        for (int i = 0; i < maxSpawnAttempts; i++)
        {
            Vector2 position =
                GetRandomOutsideCameraPosition();

            if (!IsValidSpawnPosition(position))
            {
                continue;
            }

            Instantiate(
                enemyPrefab,
                position,
                Quaternion.identity
            );

            return true;
        }

        return false;
    }

    // =========================================================
    // 카메라 밖 랜덤 좌표
    // =========================================================

    private Vector2 GetRandomOutsideCameraPosition()
    {
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
        }

        float cameraHeight =
            mainCamera.orthographicSize * 2f;

        float cameraWidth =
            cameraHeight * mainCamera.aspect;

        Vector2 center =
            mainCamera.transform.position;

        float halfWidth =
            cameraWidth * 0.5f;

        float halfHeight =
            cameraHeight * 0.5f;

        float distance =
            Random.Range(
                minOutsideDistance,
                maxOutsideDistance
            );

        int side =
            Random.Range(0, 4);

        switch (side)
        {
            case 0:
                return new Vector2(
                    Random.Range(
                        center.x - halfWidth,
                        center.x + halfWidth
                    ),
                    center.y +
                    halfHeight +
                    distance
                );

            case 1:
                return new Vector2(
                    Random.Range(
                        center.x - halfWidth,
                        center.x + halfWidth
                    ),
                    center.y -
                    halfHeight -
                    distance
                );

            case 2:
                return new Vector2(
                    center.x -
                    halfWidth -
                    distance,
                    Random.Range(
                        center.y - halfHeight,
                        center.y + halfHeight
                    )
                );

            default:
                return new Vector2(
                    center.x +
                    halfWidth +
                    distance,
                    Random.Range(
                        center.y - halfHeight,
                        center.y + halfHeight
                    )
                );
        }
    }

    // =========================================================
    // 위치 검사
    // =========================================================

    private bool IsValidSpawnPosition(
        Vector2 position)
    {
        if (ribel != null)
        {
            float distance =
                Vector2.Distance(
                    position,
                    ribel.position
                );

            if (distance <
                minDistanceFromRibel)
            {
                return false;
            }
        }

        Collider2D[] nearbySummons =
            Physics2D.OverlapCircleAll(
                position,
                minDistanceFromSummons,
                summonLayer
            );

        for (int i = 0;
             i < nearbySummons.Length;
             i++)
        {
            SummonUnitBase summon =
                nearbySummons[i]
                    .GetComponentInParent<SummonUnitBase>();

            if (summon != null)
            {
                return false;
            }
        }

        // 소환수 탐색 범위 안에도 생성 금지
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

            float distance =
                Vector2.Distance(
                    position,
                    summon.transform.position
                );

            if (distance <=
                summon.DetectionRange)
            {
                return false;
            }
        }

        Collider2D blocked =
            Physics2D.OverlapCircle(
                position,
                spawnCheckRadius,
                blockedLayers
            );

        if (blocked != null)
        {
            return false;
        }

        return true;
    }

    private void FindRibel()
    {
        GameObject objectRibel =
            GameObject.FindGameObjectWithTag(
                "Player"
            );

        if (objectRibel != null)
        {
            ribel =
                objectRibel.transform;
        }
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        maxAliveEnemies =
            Mathf.Max(
                1,
                maxAliveEnemies
            );

        spawnInterval =
            Mathf.Max(
                0.05f,
                spawnInterval
            );

        spawnCount =
            Mathf.Max(
                1,
                spawnCount
            );

        minOutsideDistance =
            Mathf.Max(
                0.1f,
                minOutsideDistance
            );

        maxOutsideDistance =
            Mathf.Max(
                minOutsideDistance,
                maxOutsideDistance
            );

        minDistanceFromRibel =
            Mathf.Max(
                0f,
                minDistanceFromRibel
            );

        minDistanceFromSummons =
            Mathf.Max(
                0f,
                minDistanceFromSummons
            );

        spawnCheckRadius =
            Mathf.Max(
                0.05f,
                spawnCheckRadius
            );

        maxSpawnAttempts =
            Mathf.Max(
                1,
                maxSpawnAttempts
            );
    }
#endif
}
using System.Collections.Generic;
using UnityEngine;

public class EnemyManager : MonoBehaviour
{
    private static EnemyManager _instance;
    public static EnemyManager Instance
    {
        get
        {
            if (_instance == null)
            {
                GameObject go = new GameObject("EnemyManager");
                _instance = go.AddComponent<EnemyManager>();
                DontDestroyOnLoad(go);
            }
            return _instance;
        }
    }

    [Header("Enemy Settings")]
    public Material enemyMaterial;
    public int enemyCount = 5;
    public float enemySize = 1f;
    public float moveSpeed = 2f;
    public float minMoveDistance = 3f;
    public float maxMoveDistance = 8f;
    public float spawnPadding = 2f;

    [Header("Damage Settings")]
    public int damageToPlayer = 1;
    public float damageCooldown = 1f;

    private Mesh enemyMesh;
    private float lastDamageTime;

    private readonly List<Matrix4x4> enemyMatrices = new();
    private readonly List<int> enemyColliderIds = new();
    private readonly List<Vector3> startPositions = new();

    private void Start()
    {
        if (enemyMaterial != null)
            enemyMaterial.enableInstancing = true;

        CreateEnemyMesh();
        SpawnEnemies();
        lastDamageTime = -damageCooldown;
    }

    private void CreateEnemyMesh()
    {
        enemyMesh = new Mesh();

        Vector3[] vertices = new Vector3[]
        {
            new Vector3(0, 0, 0),
            new Vector3(enemySize, 0, 0),
            new Vector3(enemySize, 0, enemySize),
            new Vector3(0, 0, enemySize),
            new Vector3(0, enemySize, 0),
            new Vector3(enemySize, enemySize, 0),
            new Vector3(enemySize, enemySize, enemySize),
            new Vector3(0, enemySize, enemySize)
        };

        int[] triangles = new int[]
        {
            0, 4, 1, 1, 4, 5,
            2, 6, 3, 3, 6, 7,
            0, 3, 4, 4, 3, 7,
            1, 5, 2, 2, 5, 6,
            0, 1, 3, 3, 1, 2,
            4, 7, 5, 5, 7, 6
        };

        enemyMesh.vertices = vertices;
        enemyMesh.triangles = triangles;
        enemyMesh.RecalculateNormals();
        enemyMesh.RecalculateBounds();
    }

    private void SpawnEnemies()
    {
        for (int i = 0; i < enemyCount; i++)
        {
            Vector3 position = new Vector3(
                Random.Range(-10f, 10f),
                0,
                Random.Range(-10f, 10f)
            );

            int id = CollisionManager.Instance.RegisterCollider(position, Vector3.one * enemySize, false);
            enemyColliderIds.Add(id);
            enemyMatrices.Add(Matrix4x4.TRS(position, Quaternion.identity, Vector3.one * enemySize));
            startPositions.Add(position);
        }
    }

    public void DestroyEnemy(int colliderId)
    {
        int index = enemyColliderIds.IndexOf(colliderId);
        if (index >= 0)
        {
            CollisionManager.Instance.RemoveCollider(colliderId);
            enemyColliderIds.RemoveAt(index);
            enemyMatrices.RemoveAt(index);
            startPositions.RemoveAt(index);
        }
    }

    private void Update()
    {
        for (int i = 0; i < enemyColliderIds.Count; i++)
        {
            Vector3 currentPosition = enemyMatrices[i].GetPosition();
            float moveDirection = Random.Range(-1f, 1f);
            currentPosition.x += moveDirection * moveSpeed * Time.deltaTime;

            if (CollisionManager.Instance.CheckCollision(enemyColliderIds[i], currentPosition, out List<int> collidedIds))
            {
                foreach (int id in collidedIds)
                {
                    var player = FindObjectOfType<EnhancedMeshGenerator>();
                    if (player != null && id == player.GetPlayerID())
                    {
                        if (Time.time - lastDamageTime >= damageCooldown)
                        {
                            Debug.Log("Enemy hit player!");
                            // player.TakeDamage(damageToPlayer);
                            lastDamageTime = Time.time;
                        }
                    }
                }
            }

            enemyMatrices[i] = Matrix4x4.TRS(currentPosition, Quaternion.identity, Vector3.one * enemySize);
            CollisionManager.Instance.UpdateMatrix(enemyColliderIds[i], enemyMatrices[i]);
        }
    }

    private void OnRenderObject()
    {
        foreach (var matrix in enemyMatrices)
        {
            Graphics.DrawMesh(enemyMesh, matrix, enemyMaterial, 0);
        }
    }
}

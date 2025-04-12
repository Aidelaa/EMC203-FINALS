using UnityEngine;
using System.Collections.Generic;

public class Fireball : MonoBehaviour
{
    [Header("Movement Settings")]
    [Tooltip("Speed of the fireball in units per second")]
    public float speed = 15f;

    [Tooltip("How long the fireball lasts before disappearing")]
    public float lifetime = 2f;

    private Vector3 direction;

    [Header("Combat Settings")]
    [Tooltip("Damage dealt to enemies on hit")]
    public int damage = 1;

    [Header("Visual Settings")]
    [Tooltip("Size of the fireball mesh")]
    public float size = 0.5f;

    [Tooltip("Material used to render the fireball")]
    public Material fireballMaterial;

    [Header("Runtime Variables")]
    private float spawnTime;
    private Mesh fireballMesh;
    private Matrix4x4 fireballMatrix;
    private Camera mainCamera;

    [Header("Collision Settings")]
    private int colliderId;

    void Awake()
    {
        CreateFireballMesh();
        mainCamera = Camera.main;
    }

    void CreateFireballMesh()
    {
        fireballMesh = new Mesh();
        Vector3[] vertices = new Vector3[8];
        float half = size * 0.5f;

        // Bottom vertices
        vertices[0] = new Vector3(-half, -half, -half);
        vertices[1] = new Vector3(half, -half, -half);
        vertices[2] = new Vector3(half, -half, half);
        vertices[3] = new Vector3(-half, -half, half);

        // Top vertices
        vertices[4] = new Vector3(-half, half, -half);
        vertices[5] = new Vector3(half, half, -half);
        vertices[6] = new Vector3(half, half, half);
        vertices[7] = new Vector3(-half, half, half);

        int[] triangles = new int[]
        {
            // Bottom
            0, 3, 2, 2, 1, 0,
            // Top
            4, 5, 6, 6, 7, 4,
            // Front
            0, 1, 5, 5, 4, 0,
            // Back
            2, 3, 7, 7, 6, 2,
            // Left
            3, 0, 4, 4, 7, 3,
            // Right
            1, 2, 6, 6, 5, 1
        };

        fireballMesh.vertices = vertices;
        fireballMesh.triangles = triangles;
        fireballMesh.RecalculateNormals();
    }

    public void Initialize(Vector3 startPos, Vector3 fireDirection)
    {
        spawnTime = Time.time;
        direction = fireDirection.normalized;

        // Register collider with the collision system
        colliderId = CollisionManager.Instance.RegisterCollider(
            startPos,
            Vector3.one * size,
            false
        );

        // Set initial transform matrix
        fireballMatrix = Matrix4x4.TRS(startPos, Quaternion.LookRotation(direction), Vector3.one * size);
        CollisionManager.Instance.UpdateMatrix(colliderId, fireballMatrix);
    }

    void Update()
    {
        if (Time.time - spawnTime > lifetime)
        {
            DestroyFireball();
            return;
        }

        // Move fireball
        Vector3 currentPos = fireballMatrix.GetPosition();
        Vector3 newPos = currentPos + direction * speed * Time.deltaTime;

        // Update matrix
        fireballMatrix = Matrix4x4.TRS(newPos, Quaternion.LookRotation(direction), Vector3.one * size);

        // Check collision
        if (CollisionManager.Instance.CheckCollision(colliderId, newPos, out List<int> collidedIds))
        {
            foreach (int id in collidedIds)
            {
                EnemyManager.Instance?.DestroyEnemy(id);
            }
            DestroyFireball();
            return;
        }

        // Update collider in system
        CollisionManager.Instance.UpdateCollider(colliderId, newPos, Vector3.one * size);
        CollisionManager.Instance.UpdateMatrix(colliderId, fireballMatrix);

        // Render fireball
        RenderFireball();
    }

    void RenderFireball()
    {
        if (mainCamera == null || fireballMaterial == null) return;

        Vector3 fireballPos = fireballMatrix.GetPosition();
        Vector3 toFireball = (fireballPos - mainCamera.transform.position).normalized;
        float dot = Vector3.Dot(mainCamera.transform.forward, toFireball);

        // If behind camera, scale to zero
        Vector3 renderScale = dot > 0 ? Vector3.one * size : Vector3.zero;

        Matrix4x4 renderMatrix = Matrix4x4.TRS(
            fireballPos,
            Quaternion.LookRotation(direction),
            renderScale
        );

        Graphics.DrawMesh(
            fireballMesh,
            renderMatrix,
            fireballMaterial,
            0,
            null,
            0,
            null,
            UnityEngine.Rendering.ShadowCastingMode.Off,
            false
        );
    }

    void DestroyFireball()
    {
        CollisionManager.Instance.RemoveCollider(colliderId);
        Destroy(gameObject);
    }
}

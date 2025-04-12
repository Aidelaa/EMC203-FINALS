using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer), typeof(Rigidbody2D))]
public class EnhancedMeshGenerator : MonoBehaviour
{
    [Header("Box Settings")]
    public List<Vector3> boxPositions;
    public Vector3 boxSize = Vector3.one;
    public Material material;

    [Header("Player Settings")]
    public float moveSpeed = 5f;
    public float jumpForce = 10f;
    public float airMovementMultiplier = 0.5f;
    public float jumpCheckDistance = 0.1f;
    public LayerMask groundLayer;
    public Transform groundCheck;
    public Camera mainCamera;

    [Header("Fireball Settings")]
    public GameObject fireballPrefab;
    public Transform fireballSpawnPoint;
    public float fireballSpeed = 10f;
    public bool canShootFireballs = true;

    private Mesh mesh;
    private List<Vector3> vertices = new List<Vector3>();
    private List<int> triangles = new List<int>();
    private Rigidbody2D rb;
    private bool isGrounded;
    private Vector2 movement;

    private Matrix4x4[] matrices; // Reusable array for box rendering

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        GetComponent<MeshRenderer>().material = material;

        mesh = new Mesh();
        GetComponent<MeshFilter>().mesh = mesh;

        UpdateMesh();
        RenderBoxes();
    }

    void Update()
    {
        // Player movement input
        movement.x = Input.GetAxis("Horizontal");

        // Jump
        isGrounded = Physics2D.Raycast(groundCheck.position, Vector2.down, jumpCheckDistance, groundLayer);
        if (Input.GetButtonDown("Jump") && isGrounded)
        {
            rb.velocity = new Vector2(rb.velocity.x, jumpForce);
        }

        // Fireball shooting
        if (canShootFireballs && Input.GetKeyDown(KeyCode.F))
        {
            ShootFireball();
        }

        // Camera follow
        if (mainCamera)
        {
            Vector3 cameraPos = mainCamera.transform.position;
            cameraPos.x = transform.position.x;
            mainCamera.transform.position = cameraPos;
        }
    }

    void FixedUpdate()
    {
        // Handle movement (different control in air vs on ground)
        float control = isGrounded ? 1f : airMovementMultiplier;
        rb.velocity = new Vector2(movement.x * moveSpeed * control, rb.velocity.y);
    }

    void UpdateMesh()
    {
        mesh.Clear();
        vertices.Clear();
        triangles.Clear();

        for (int i = 0; i < boxPositions.Count; i++)
        {
            AddBox(boxPositions[i]);
        }

        mesh.vertices = vertices.ToArray();
        mesh.triangles = triangles.ToArray();
        mesh.RecalculateNormals();
    }

    void AddBox(Vector3 center)
    {
        Vector3 half = boxSize / 2;

        // Box corners
        Vector3 topLeft = center + new Vector3(-half.x, half.y);
        Vector3 topRight = center + new Vector3(half.x, half.y);
        Vector3 bottomLeft = center + new Vector3(-half.x, -half.y);
        Vector3 bottomRight = center + new Vector3(half.x, -half.y);

        int start = vertices.Count;

        vertices.Add(bottomLeft);  // 0
        vertices.Add(topLeft);     // 1
        vertices.Add(topRight);    // 2
        vertices.Add(bottomRight); // 3

        // Two triangles (0-1-2 and 0-2-3)
        triangles.Add(start + 0);
        triangles.Add(start + 1);
        triangles.Add(start + 2);
        triangles.Add(start + 0);
        triangles.Add(start + 2);
        triangles.Add(start + 3);
    }

    void RenderBoxes()
    {
        int count = boxPositions.Count;

        if (matrices == null || matrices.Length != count)
        {
            matrices = new Matrix4x4[count];
        }

        for (int i = 0; i < count; i++)
        {
            matrices[i] = Matrix4x4.TRS(boxPositions[i], Quaternion.identity, boxSize);
        }

        Graphics.DrawMeshInstanced(mesh, 0, material, matrices);
    }

    void ShootFireball()
    {
        if (fireballPrefab && fireballSpawnPoint)
        {
            GameObject fireball = Instantiate(fireballPrefab, fireballSpawnPoint.position, Quaternion.identity);
            Rigidbody2D rb = fireball.GetComponent<Rigidbody2D>();

            if (rb)
            {
                rb.velocity = new Vector2(transform.localScale.x * fireballSpeed, 0f);
            }
        }
    }
}

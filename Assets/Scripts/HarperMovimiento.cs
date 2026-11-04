using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D))]
public class HarperMovimiento : MonoBehaviour
{
    [Header("Movimiento")]
    public float velocidad = 5f;
    // Límites del piso (ajústalos en el Inspector)
    public float xMin = 2f, xMax = 28f;
    public float yMin = 0.3f, yMax = 5.5f;

    Rigidbody2D rb;
    SpriteRenderer sr;
    Vector2 dir;

    // Hacia dónde mira Harper: 1 = derecha, -1 = izquierda
    public int Direccion => (sr != null && sr.flipX) ? -1 : 1;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0;
        rb.freezeRotation = true;

        // Busca el Sprite Renderer en Harper o en un hijo
        sr = GetComponentInChildren<SpriteRenderer>();
        if (sr == null) Debug.LogWarning("Harper no tiene SpriteRenderer");
    }

    void Update()
    {
        var k = Keyboard.current;
        if (k == null) return;

        float x = 0, y = 0;
        if (k.aKey.isPressed || k.leftArrowKey.isPressed)  x -= 1;
        if (k.dKey.isPressed || k.rightArrowKey.isPressed) x += 1;
        if (k.sKey.isPressed || k.downArrowKey.isPressed)  y -= 1;
        if (k.wKey.isPressed || k.upArrowKey.isPressed)    y += 1;
        dir = new Vector2(x, y).normalized;

        // Harper mira a la derecha por defecto
        if (x != 0 && sr != null) sr.flipX = x < 0;
    }

    void FixedUpdate()
    {
        Vector2 p = rb.position + dir * velocidad * Time.fixedDeltaTime;
        p.x = Mathf.Clamp(p.x, xMin, xMax);
        p.y = Mathf.Clamp(p.y, yMin, yMax);
        rb.MovePosition(p);
    }
}
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D))]
public class HarperMovimiento : MonoBehaviour
{
    [Header("Movimiento Base")]
    public float velocidadMax = 18f; // Solo necesitas la máxima, la mínima siempre es 0
    public float xMin = 2f, xMax = 28f;
    public float yMin = 0.3f, yMax = 5.5f;

    [Header("Ajustes de responsiveness")]
    // Valores más grandes porque ahora sí los multiplicaremos por delta time
    public float aceleracion = 190f; 
    public float desaceleracion = 220f; 

    Rigidbody2D rb;
    SpriteRenderer sr;
    Vector2 dirInput;
    Vector2 velocidadActual; // Guarda el vector de la inercia actual

    public int Direccion => (sr != null && sr.flipX) ? -1 : 1;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0;
        rb.freezeRotation = true;

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
        
        dirInput = new Vector2(x, y).normalized;

        if (x != 0 && sr != null) sr.flipX = x < 0;
    }

    void FixedUpdate()
    {   
        // 1. Calculamos hacia dónde queremos ir y a qué velocidad final
        Vector2 velocidadObjetivo = dirInput * velocidadMax;

        // 2. Definimos si estamos acelerando (hay input) o frenando (no hay input)
        float tasaActual = (dirInput.magnitude > 0) ? aceleracion : desaceleracion;

        // 3. Transicionamos suavemente la velocidad actual hacia el objetivo
        velocidadActual = Vector2.MoveTowards(
            velocidadActual, 
            velocidadObjetivo, 
            tasaActual * Time.fixedDeltaTime
        );

        // 4. Aplicamos el movimiento usando la inercia calculada
        Vector2 p = rb.position + velocidadActual * Time.fixedDeltaTime;
        p.x = Mathf.Clamp(p.x, xMin, xMax);
        p.y = Mathf.Clamp(p.y, yMin, yMax);
        
        rb.MovePosition(p);
    }
}
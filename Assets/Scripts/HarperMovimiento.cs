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
    HarperAgachar agachar;   // NUEVO
    VidaHarper vida;         // NUEVO
    SpriteRenderer sr;
    Vector2 dirInput;
    Vector2 velocidadActual; // Guarda el vector de la inercia actual

    public int Direccion => (sr != null && sr.flipX) ? -1 : 1;

    // NUEVO (Aspirabot): fuerzas que mueven a Harper sin que ella quiera
    [HideInInspector] public Vector2 fuerzaExterna;   // la succión la pone cada cuadro
    Vector2 empujon;                                   // un empujón que se va frenando

    public void Empujar(Vector2 fuerza) { empujon = fuerza; }

    // NUEVO (animación): velocidad real después de chocar con los límites del piso.
    public Vector2 VelocidadReal { get; private set; }

    // NUEVO: no camina mientras está agachada o aturdida por un golpe
    public bool Bloqueada => (agachar != null && agachar.Agachada) || (vida != null && vida.Aturdida);

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0;
        rb.freezeRotation = true;

        sr = GetComponentInChildren<SpriteRenderer>();
        agachar = GetComponent<HarperAgachar>();   // NUEVO
        vida = GetComponent<VidaHarper>();         // NUEVO
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
        
        if (Bloqueada) { dirInput = Vector2.zero; return; }   // NUEVO

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
        Vector2 p = rb.position + (velocidadActual + fuerzaExterna + empujon) * Time.fixedDeltaTime;
        fuerzaExterna = Vector2.zero;
        empujon = Vector2.MoveTowards(empujon, Vector2.zero, 60f * Time.fixedDeltaTime);
        p.x = Mathf.Clamp(p.x, xMin, xMax);
        p.y = Mathf.Clamp(p.y, yMin, yMax);

        VelocidadReal = (p - rb.position) / Time.fixedDeltaTime;   // NUEVO (animación)
        rb.MovePosition(p);
    }
}
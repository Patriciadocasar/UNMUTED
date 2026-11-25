using UnityEngine;
using UnityEngine.InputSystem;

// Harper se agacha mientras mantienes presionada la tecla (Espacio o C por defecto).
//  - Agachada no camina (WASD), no dispara (J) ni usa el especial (K).
//  - Al agacharse, su caja de golpe (BoxCollider2D) se encoge hasta la altura de la pose:
//    una bala que pasa por encima no la toca, pero si la roza (manos, cabeza, espalda) sí le hace daño.
//  - No puede agacharse durante la pose de ataque ni mientras está herida.
// Va en el mismo GameObject que HarperMovimiento y su BoxCollider2D.
[DefaultExecutionOrder(-10)]                 // decide antes que HarperMovimiento y HarperAtaque
[RequireComponent(typeof(HarperMovimiento))]
public class HarperAgachar : MonoBehaviour
{
    [Header("Teclas")]
    public Key tecla = Key.Space;
    public Key teclaAlterna = Key.C;

    [Header("Ajustes")]
    [Tooltip("Segundos para bajar o subir (cuadro 'Agacharse')")]
    public float duracionTransicion = 0.06f;

    [Header("Caja de golpe agachada")]
    [Tooltip("Altura del borde superior de la caja agachada, en unidades locales desde los pies (pivote). " +
             "6.4 = punta de las manos en el sprite Agachada (fila 16 de 80, PPU 10).")]
    public float alturaAgachada = 6.4f;

    HarperMovimiento mov;
    HarperAtaque ataque;
    VidaHarper vida;
    BoxCollider2D caja;
    Vector2 offsetDePie, tamanoDePie;
    float bordeAbajo, bordeArribaDePie;

    // true mientras está abajo o bajando/subiendo: bloquea caminar y atacar
    public bool Agachada => Progreso > 0f;
    // 0 = de pie, 1 = agachada del todo
    public float Progreso { get; private set; }

    void Awake()
    {
        mov = GetComponent<HarperMovimiento>();
        ataque = GetComponent<HarperAtaque>();
        vida = GetComponent<VidaHarper>();
        caja = GetComponent<BoxCollider2D>();
        if (caja != null)
        {
            offsetDePie = caja.offset;
            tamanoDePie = caja.size;
            bordeAbajo = offsetDePie.y - tamanoDePie.y * 0.5f;
            bordeArribaDePie = offsetDePie.y + tamanoDePie.y * 0.5f;
        }
        else Debug.LogWarning("HarperAgachar: Harper no tiene BoxCollider2D; la caja de golpe no se encogerá");
    }

    void Update()
    {
        var k = Keyboard.current;
        bool quiere = k != null && (k[tecla].isPressed || k[teclaAlterna].isPressed);

        // casos en que no puede agacharse
        if (!mov.enabled) quiere = false;                              // muerta
        if (vida != null && vida.Herida) quiere = false;               // el golpe la levanta
        if (ataque != null && ataque.EnPose && Progreso == 0f) quiere = false;

        float paso = duracionTransicion > 0f ? Time.deltaTime / duracionTransicion : 1f;
        Progreso = (vida != null && vida.Herida) ? 0f : Mathf.MoveTowards(Progreso, quiere ? 1f : 0f, paso);

        AjustarCaja();
    }

    // Baja el borde superior de la caja según cuánto esté agachada; el borde de los pies no se mueve
    void AjustarCaja()
    {
        if (caja == null) return;
        float arriba = Mathf.Lerp(bordeArribaDePie, Mathf.Min(alturaAgachada, bordeArribaDePie), Progreso);
        float alto = arriba - bordeAbajo;
        caja.size = new Vector2(tamanoDePie.x, alto);
        caja.offset = new Vector2(offsetDePie.x, bordeAbajo + alto * 0.5f);
    }

    void OnDisable()
    {
        if (caja != null) { caja.size = tamanoDePie; caja.offset = offsetDePie; }
    }

    // Línea amarilla: hasta dónde llega la caja cuando está agachada (seleccionando a Harper)
    void OnDrawGizmosSelected()
    {
        var c = GetComponent<BoxCollider2D>();
        if (c == null) return;
        float abajo = Application.isPlaying ? bordeAbajo : c.offset.y - c.size.y * 0.5f;
        Vector3 a = transform.TransformPoint(new Vector3(c.offset.x - c.size.x * 0.5f, alturaAgachada, 0f));
        Vector3 b = transform.TransformPoint(new Vector3(c.offset.x + c.size.x * 0.5f, alturaAgachada, 0f));
        Vector3 p = transform.TransformPoint(new Vector3(c.offset.x - c.size.x * 0.5f, abajo, 0f));
        Vector3 q = transform.TransformPoint(new Vector3(c.offset.x + c.size.x * 0.5f, abajo, 0f));
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(a, b); Gizmos.DrawLine(a, p); Gizmos.DrawLine(b, q); Gizmos.DrawLine(p, q);
    }
}

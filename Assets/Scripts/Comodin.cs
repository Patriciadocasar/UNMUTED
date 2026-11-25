using UnityEngine;

public enum TipoComodin { Corazon, Nota }

// Va en el prefab de cada comodín (el corazón o la nota).
// Aparece cuando muere un robot, flota, y si Harper pasa encima, lo agarra.
[RequireComponent(typeof(SpriteRenderer))]
public class Comodin : MonoBehaviour
{
    [Header("Qué es")]
    public TipoComodin tipo = TipoComodin.Corazon;
    public float cantidad = 2f;          // corazón: vida que devuelve | nota: energía que da

    [Header("Comportamiento")]
    public float duracion = 8f;          // segundos antes de desaparecer
    public float radioAgarre = 0.7f;     // qué tan cerca tiene que pasar Harper
    public float alturaFlotar = 0.15f;   // cuánto sube y baja

    [Header("Sonido")]
    public AudioClip sonidoAgarrar;

    SpriteRenderer sr;
    Vector3 escalaOriginal, posBase;
    float t;

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        escalaOriginal = transform.localScale;
        transform.localScale = Vector3.zero;   // aparece creciendo
        if (sr.sortingOrder < 15) sr.sortingOrder = 15;
    }

    void Start() { posBase = transform.position; }

    void Update()
    {
        t += Time.deltaTime;
        if (t >= duracion) { Destroy(gameObject); return; }

        // Aparece con un "boing" y luego flota
        float entrada = Mathf.Clamp01(t / 0.25f);
        float rebote = entrada < 1f ? Mathf.Sin(entrada * Mathf.PI * 0.75f) * 1.25f : 1f;
        transform.localScale = escalaOriginal * rebote;
        transform.position = posBase + Vector3.up * Mathf.Sin(t * 4f) * alturaFlotar;

        // Los últimos 2 segundos parpadea, avisando que se va
        sr.enabled = t < duracion - 2f || ((int)(t * 10f)) % 2 == 0;

        // ¿Harper lo está tocando?
        foreach (var c in Physics2D.OverlapCircleAll(transform.position, radioAgarre))
        {
            var vida = c.GetComponentInParent<VidaHarper>();
            if (vida == null || vida.Actual <= 0) continue;
            Agarrar(vida);
            return;
        }
    }

    void Agarrar(VidaHarper vida)
    {
        if (tipo == TipoComodin.Corazon)
            vida.Curar(Mathf.RoundToInt(cantidad), new Color(1f, 0.45f, 0.6f));   // destello rosado
        else
        {
            var energia = vida.GetComponent<EnergiaSonica>();
            if (energia != null) energia.Sumar(cantidad);
        }
        Sonidos.Tocar(sonidoAgarrar);
        Destroy(gameObject);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, radioAgarre);
    }
}
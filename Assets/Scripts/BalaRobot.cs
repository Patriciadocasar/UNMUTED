using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class BalaRobot : MonoBehaviour
{
    public Sprite[] frames;
    public float fps = 10f;
    public float velocidad = 6f;
    public int dano = 1;
    public Vector2 tamanoHit = new Vector2(0.8f, 0.5f);
    public float duracionMax = 4f;

    [Header("Movimiento ondulado (opcional)")]
    public bool ondulada = false;      // ✅ = la bala va haciendo olas en vez de ir recta
    public float amplitud = 1f;        // qué tan alto y bajo sube la ola
    public float frecuencia = 1.5f;    // cuántas olas por segundo
    float baseY;
    bool baseLista;

    Vector2 direccionLibre;     // para las balas que van en diagonal (las del robot volador)
    bool apuntada;

    public void LanzarApuntada(Vector2 direccion)
    {
        apuntada = true;
        direccionLibre = direccion.normalized;
        dir = direccion.x >= 0f ? 1 : -1;
        sr.flipX = dir > 0;
    }

    SpriteRenderer sr;
    int dir = -1;
    float t;

    void Awake() { sr = GetComponent<SpriteRenderer>(); }

    public void Lanzar(int direccion, float carril)
    {
        dir = direccion;
        baseY = transform.position.y; baseLista = true;
        sr.flipX = dir > 0;   // el dibujo mira a la izquierda
    }

    void Update()
    {
        t += Time.deltaTime;
        if (t > duracionMax) { Destroy(gameObject); return; }

        if (apuntada)
            transform.position += (Vector3)(direccionLibre * velocidad * Time.deltaTime);
        else
            transform.position += new Vector3(dir * velocidad * Time.deltaTime, 0f, 0f);
        if (ondulada && !apuntada)
        {
            if (!baseLista) { baseY = transform.position.y; baseLista = true; }
            var pos = transform.position;
            pos.y = baseY + Mathf.Sin(t * frecuencia * 2f * Mathf.PI) * amplitud;
            transform.position = pos;
        }
        if (frames != null && frames.Length > 0)
            sr.sprite = frames[(int)(t * fps) % frames.Length];

        foreach (var c in Physics2D.OverlapBoxAll(transform.position, tamanoHit, 0f))
        {
            var v = c.GetComponentInParent<VidaHarper>();
            if (v == null) continue;
            // Si la bala toca el cuerpo de Harper (pies, pecho o cabeza), cuenta como golpe

            // Si Harper está parpadeando (invulnerable), la bala la atraviesa
            // y sigue de largo en vez de desaparecer sin hacer daño
            if (v.RecibirDano(dano))
            {
                Destroy(gameObject);
                return;
            }
        }
    }
}
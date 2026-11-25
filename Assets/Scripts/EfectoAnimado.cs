using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class EfectoAnimado : MonoBehaviour
{
    public Sprite[] frames;
    public float fps = 12f;

    [Header("Efectos que se agrandan (Acorde Explosivo)")]
    public float duracionTotal = 0f;   // 0 = usa los fps; si no, dura este tiempo en segundos
    public float crecimiento = 0f;     // 0 = tamaño fijo; 12 = termina 12 veces más grande
    public bool desvanecer = false;    // se vuelve transparente al final
    public bool repetirAnimacion = false;   // las imágenes se repiten en bucle (la bola de disco girando)

    [Header("Modo proyectil (solo para el golpe del bajo)")]
    public bool esProyectil = false;
    public float velocidadProyectil = 10f;
    public float alcanceMaximo = 9f;
    public float toleranciaCarril = 1.2f;
    public Vector2 tamanoHit = new Vector2(0.5f, 1f);

    SpriteRenderer sr;
    float t;

    bool lanzado;
    int dir = 1, dano = 1;
    float carrilY, distObjetivo, recorrido, energiaImpacto;
    EnergiaSonica energia;
    GameObject prefabImpacto;

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        if (frames == null || frames.Length == 0)
            Debug.LogWarning(name + ": la lista Frames está vacía");
        else
            sr.sprite = frames[0];
    }

    public void Lanzar(int direccion, int danoGolpe, float carril,
                       EnergiaSonica en, float energiaPorImpacto, GameObject impacto)
    {
        esProyectil = true;
        dir = direccion; dano = danoGolpe; carrilY = carril;
        energia = en; energiaImpacto = energiaPorImpacto; prefabImpacto = impacto;

        float origenX = transform.position.x;
        float mejor = alcanceMaximo;
        foreach (var e in FindObjectsByType<Enemigo>())
        {
            float dx = (e.transform.position.x - origenX) * dir;
            if (dx <= 0f) continue;
            if (Mathf.Abs(e.AlturaCarril - carrilY) > toleranciaCarril) continue;

            var col = e.GetComponent<Collider2D>();
            float borde = col != null
                ? (dir > 0 ? col.bounds.min.x : col.bounds.max.x)
                : e.transform.position.x;
            float dist = (borde - origenX) * dir;
            if (dist > 0.3f && dist < mejor) mejor = dist;
        }
        distObjetivo = mejor;
        lanzado = true;
    }

    void Update()
    {
        if (frames == null || frames.Length == 0) { Destroy(gameObject); return; }

        if (esProyectil)
        {
            if (lanzado) MoverProyectil();
            return;
        }

        // Animación por tiempo (Acorde Explosivo e Impacto)
        t += Time.deltaTime;
        float dur = duracionTotal > 0f ? duracionTotal : frames.Length / fps;
        if (t >= dur) { Destroy(gameObject); return; }

        float progreso = t / dur;
        int i = repetirAnimacion
            ? (int)(t * fps) % frames.Length                                  // gira y gira
            : Mathf.Min((int)(progreso * frames.Length), frames.Length - 1);  // pasa una sola vez
        sr.sprite = frames[i];

        if (crecimiento > 0f)
            transform.localScale = Vector3.one * Mathf.Lerp(1f, crecimiento, progreso);

        if (desvanecer)
        {
            float a = 1f - Mathf.Clamp01((progreso - 0.6f) / 0.4f);
            sr.color = new Color(1f, 1f, 1f, a);
        }
    }

    void MoverProyectil()
    {
        float paso = velocidadProyectil * Time.deltaTime;
        transform.position += new Vector3(dir * paso, 0f, 0f);
        recorrido += paso;

        float progreso = Mathf.Clamp01(recorrido / distObjetivo);
        int i = Mathf.Min((int)(progreso * frames.Length), frames.Length - 1);
        sr.sprite = frames[i];

        foreach (var c in Physics2D.OverlapBoxAll(transform.position, tamanoHit, 0f))
        {
            var e = c.GetComponent<Enemigo>();
            if (e == null) continue;
            if (Mathf.Abs(e.AlturaCarril - carrilY) > toleranciaCarril) continue;

            e.RecibirDano(dano);
            if (energia != null) energia.Sumar(energiaImpacto);
            if (prefabImpacto != null)
                Instantiate(prefabImpacto, transform.position, Quaternion.identity);
            Destroy(gameObject);
            return;
        }

        if (recorrido >= distObjetivo + 0.5f) Destroy(gameObject);
    }
}
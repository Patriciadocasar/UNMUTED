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
        float mejor = Mathf.Max(alcanceMaximo, 30f);   // busca robots en todo el ancho del nivel
        foreach (var e in FindObjectsByType<Enemigo>())
        {
            float dx = (e.transform.position.x - origenX) * dir;
            if (dx <= 0f) continue;
            if (Mathf.Abs(e.AlturaCarril - carrilY) > ToleranciaPara(e)) continue;

            var col = e.GetComponent<Collider2D>();
            float borde = col != null
                ? (dir > 0 ? col.bounds.min.x : col.bounds.max.x)
                : e.transform.position.x;
            float dist = (borde - origenX) * dir;
            if (dist > 0.3f && dist < mejor) mejor = dist;
        }
        distObjetivo = mejor < 30f ? mejor : alcanceMaximo;   // para la animación
        lanzado = true;
    }

    // Más fácil de acertar si el robot está mareado bailando
    float ToleranciaPara(Enemigo e) =>
        e.EstaMareado ? toleranciaCarril * 2.2f :     // Robot Espejo en el piso: le pegas aunque estés más arriba o más abajo
        e.Bailando    ? toleranciaCarril * 1.6f : toleranciaCarril;

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
        float xAntes = transform.position.x;
        transform.position += new Vector3(dir * paso, 0f, 0f);
        float xAhora = transform.position.x;
        recorrido += paso;

        float progreso = Mathf.Clamp01(recorrido / distObjetivo);
        int i = Mathf.Min((int)(progreso * frames.Length), frames.Length - 1);
        sr.sprite = frames[i];

        // ¿Le pegó a algún robot? Cuenta si el golpe pasó por encima del robot (de lado a lado)
        // y el robot está en el mismo carril que Harper, sin importar si apuntó a la cabeza o a los pies.
        float minX = Mathf.Min(xAntes, xAhora) - tamanoHit.x / 2f;
        float maxX = Mathf.Max(xAntes, xAhora) + tamanoHit.x / 2f;
        foreach (var e in FindObjectsByType<Enemigo>())
        {
            if (e.Invulnerable) continue;   // el Robot Espejo arriba: el golpe lo atraviesa
            var col = e.GetComponentInChildren<Collider2D>();

            // Cuenta si está en el mismo carril que Harper... o si el golpe pasa visualmente por su cuerpo
            // (así también se le puede pegar al robot volador cuando está arriba)
            bool enCarril = Mathf.Abs(e.AlturaCarril - carrilY) <= ToleranciaPara(e);
            bool tocaCuerpo = col != null && transform.position.y >= col.bounds.min.y - 0.3f
                                          && transform.position.y <= col.bounds.max.y + 0.3f;
            if (!enCarril && !tocaCuerpo) continue;

            float izq = col != null ? col.bounds.min.x : e.transform.position.x - 0.5f;
            float der = col != null ? col.bounds.max.x : e.transform.position.x + 0.5f;
            if (maxX < izq || minX > der) continue;   // todavía no llega o ya pasó

            e.RecibirDano(dano);
            if (energia != null) energia.Sumar(energiaImpacto);
            if (prefabImpacto != null)
                Instantiate(prefabImpacto, transform.position, Quaternion.identity);
            Destroy(gameObject);
            return;
        }

        // Desaparece cuando sale de la pantalla
        var cam = Camera.main;
        if (cam != null)
        {
            float vx = cam.WorldToViewportPoint(transform.position).x;
            if (vx < -0.1f || vx > 1.1f) Destroy(gameObject);
        }
        if (recorrido > 40f) Destroy(gameObject);
    }
}
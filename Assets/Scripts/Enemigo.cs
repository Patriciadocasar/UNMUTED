using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class Enemigo : MonoBehaviour
{
    [Header("Vida")]
    public int vida = 20;

    [Header("Movimiento")]
    public float velocidad = 2f;
    public float distanciaMin = 7f;       // lo más cerca que se pone para disparar
    public float distanciaMax = 12f;      // lo más lejos desde donde dispara
    float distanciaDisparo;               // se elige al azar entre Min y Max en cada disparo
    public float margenCarril = 0.3f;     // qué tan alineado en altura se pone
    public float xMin = 2f, xMax = 28f, yMin = 0.3f, yMax = 5.5f;
    public bool miraIzquierdaPorDefecto = true;   // tu dibujo mira a la izquierda
    public float ajusteAltura = 1.2f;     // súbelo si el robot se ve muy abajo respecto a Harper

    // La "altura de carril" del robot, comparable con la de Harper
    public float AlturaCarril => transform.position.y - ajusteAltura;

    [Header("Disparo")]
    public GameObject prefabBala;
    public Vector2 salidaBala = new Vector2(1.4f, 1f);  // de los pies hacia la boca del taladro
    public float cadencia = 2.5f;         // segundos entre disparos
    public float retrasoInicial = 1.2f;
    public float toleranciaCarril = 1f;

    [Header("Animación (opcional: si lo dejas vacío usa una sola imagen)")]
    public Sprite[] framesMovimiento;     // por ejemplo, el robot nuevo flotando
    public float fpsMovimiento = 8f;
    float finReaccion;

    [Header("Reacciones")]
    public Sprite spriteGolpe1;           // destello
    public Sprite spriteGolpe2;           // tinte rosa
    public float duracionGolpe = 0.25f;
    public GameObject prefabMuerte;

    [Header("Comodines que puede soltar al morir")]
    public GameObject[] comodines;                 // arrastra aquí el corazón y/o la nota
    [Range(0f, 1f)] public float probabilidadComodin = 0.3f;   // 0.3 = 30% de las veces

    [Header("Sonidos")]
    public AudioClip sonidoDisparo;      // cuando dispara
    public AudioClip sonidoGolpe;        // cuando Harper le pega
    public AudioClip sonidoMuerte;       // cuando explota

    Rigidbody2D rb;
    SpriteRenderer sr;
    Sprite spriteNormal;
    Transform harper;
    Vector2 mover;
    int dirHacia = -1;
    float proximoDisparo, finAturdido;

    [Header("Volador (el Silenciador)")]
    public bool volador = false;            // ✅ = vuela libre por la escena y dispara apuntando a Harper
    public float alturaVueloMax = 7.5f;     // hasta dónde puede subir (para que Harper igual le alcance)
    public float cambioRumbo = 2.5f;        // cada cuántos segundos elige otro lugar a donde volar
    Vector2 objetivoVuelo;
    float proximoRumbo;

    // Entrada deslizándose desde fuera de la pantalla (la usa GestorOleadas)
    bool entrando;
    float xDestinoEntrada;
    public float velocidadEntrada = 5f;

    public void EntrarHasta(float xDestino)
    {
        entrando = true;
        xDestinoEntrada = xDestino;
    }

    [Header("Baile (poder Bola de Disco)")]
    public float multiplicadorDanoBailando = 2f;   // daño extra mientras baila
    float bailandoHasta;
    bool estabaBailando;
    public bool Bailando => Time.time < bailandoHasta;

    // Lo llama el poder Bola de Disco: el robot queda hipnotizado bailando
    public void Hipnotizar(float segundos)
    {
        bailandoHasta = Time.time + segundos;
        finAturdido = Mathf.Max(finAturdido, bailandoHasta);
        EstrellasMareo.Crear(transform, sr, segundos);   // estrellitas girando sobre la cabeza
    }
    Coroutine reaccion;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0;
        rb.freezeRotation = true;
        sr = GetComponentInChildren<SpriteRenderer>();
        if (sr != null) spriteNormal = sr.sprite;
        proximoDisparo = Time.time + retrasoInicial;
    }

    void Start()
    {
        var v = FindAnyObjectByType<VidaHarper>();
        distanciaDisparo = Random.Range(distanciaMin, distanciaMax);
        Debug.Log("Robot empieza con vida: " + vida);
        if (v != null) harper = v.transform;
        else Debug.LogWarning("Enemigo: Harper no tiene el script VidaHarper");
    }

    void Update()
    {
        mover = Vector2.zero;
        if (harper == null) return;

        float dx = harper.position.x - transform.position.x;
        float dy = harper.position.y + ajusteAltura - transform.position.y;
        dirHacia = dx >= 0f ? 1 : -1;
        if (sr != null) sr.flipX = miraIzquierdaPorDefecto ? dirHacia > 0 : dirHacia < 0;

        // Animación de moverse/flotar (si no está en medio de una reacción a un golpe)
        if (sr != null && framesMovimiento != null && framesMovimiento.Length > 0 && Time.time >= finReaccion)
            sr.sprite = framesMovimiento[(int)(Time.time * fpsMovimiento) % framesMovimiento.Length];

        if (entrando) return;          // mientras entra se ve y se anima, pero no persigue ni dispara

        // Bailando: se voltea de un lado a otro y cambia de colores
        if (Bailando)
        {
            estabaBailando = true;
            if (sr != null)
            {
                sr.flipX = ((int)(Time.time * 4f)) % 2 == 0;
                sr.color = Color.HSVToRGB((Time.time * 0.8f) % 1f, 0.45f, 1f);
            }
            return;
        }
        if (estabaBailando) { estabaBailando = false; if (sr != null) sr.color = Color.white; }

        if (Time.time < finAturdido) return;   // aturdido: no se mueve ni dispara

        if (volador) { Volar(); return; }

        if (Mathf.Abs(dx) > distanciaDisparo) mover.x = dirHacia;                 // muy lejos: se acerca
        else if (Mathf.Abs(dx) < distanciaDisparo - 1.5f) mover.x = -dirHacia;   // muy cerca: retrocede
        if (Mathf.Abs(dy) > margenCarril) mover.y = Mathf.Sign(dy);
        mover = mover.normalized;

        if (Time.time >= proximoDisparo
            && Mathf.Abs(dx) <= distanciaDisparo + 1f
            && Mathf.Abs(dy) <= toleranciaCarril)
            Disparar();
    }

    void FixedUpdate()
    {
        if (entrando)
        {
            // Se desliza hacia su lugar dentro del piso, sin respetar todavía los límites
            Vector2 p0 = rb.position;
            float paso = velocidadEntrada * Time.fixedDeltaTime;
            float nx = Mathf.MoveTowards(p0.x, xDestinoEntrada, paso);
            rb.MovePosition(new Vector2(nx, p0.y));
            if (Mathf.Abs(nx - xDestinoEntrada) < 0.01f) entrando = false;
            return;
        }

        Vector2 p = rb.position + mover * velocidad * Time.fixedDeltaTime;
        p.x = Mathf.Clamp(p.x, xMin, xMax);
        float techo = volador ? alturaVueloMax : yMax + ajusteAltura;
        p.y = Mathf.Clamp(p.y, yMin + ajusteAltura, techo);
        rb.MovePosition(p);
    }

    // Vuela de un punto a otro de la escena, manteniéndose a distancia de Harper, y le dispara apuntando
    void Volar()
    {
        Vector2 pos = transform.position;
        if (Time.time >= proximoRumbo || Vector2.Distance(pos, objetivoVuelo) < 0.3f)
        {
            proximoRumbo = Time.time + cambioRumbo;
            float lado = Random.value < 0.5f ? -1f : 1f;
            float x = Mathf.Clamp(harper.position.x + lado * Random.Range(distanciaMin, distanciaMax), xMin, xMax);
            float y = Random.Range(yMin + ajusteAltura, alturaVueloMax);
            objetivoVuelo = new Vector2(x, y);
        }
        Vector2 hacia = objetivoVuelo - pos;
        if (hacia.magnitude > 0.2f) mover = hacia.normalized;

        if (Time.time >= proximoDisparo) Disparar();
    }

    void Disparar()
    {
        proximoDisparo = Time.time + cadencia;
        distanciaDisparo = Random.Range(distanciaMin, distanciaMax);   // la próxima vez, otra distancia
        if (prefabBala == null) { Debug.LogWarning("Enemigo: falta Prefab Bala"); return; }
        Sonidos.Tocar(sonidoDisparo);

        Vector3 pos = transform.position +
            new Vector3(dirHacia * Mathf.Abs(salidaBala.x), salidaBala.y, 0f);
        var b = Instantiate(prefabBala, pos, Quaternion.identity);
        var bala = b.GetComponent<BalaRobot>();
        if (bala != null)
        {
            if (volador && harper != null)
            {
                // Apunta al pecho de Harper, aunque tenga que disparar en diagonal
                Vector2 objetivo = harper.position + Vector3.up * 1.5f;
                bala.LanzarApuntada((objetivo - (Vector2)pos).normalized);
            }
            else bala.Lanzar(dirHacia, AlturaCarril);
        }
    }

    public void RecibirDano(int d)
    {
        if (Bailando) d = Mathf.RoundToInt(d * multiplicadorDanoBailando);
        vida -= d;
        Debug.Log("Robot recibió " + d + " de daño. Le queda: " + vida);
        if (vida <= 0) { Morir(); return; }
        Sonidos.Tocar(sonidoGolpe);

        finAturdido = Mathf.Max(finAturdido, Time.time + duracionGolpe * 2f);
        finReaccion = Time.time + duracionGolpe * 2f;
        if (reaccion != null) StopCoroutine(reaccion);
        reaccion = StartCoroutine(Reaccion());
    }

    IEnumerator Reaccion()
    {
        if (sr == null) yield break;
        if (spriteGolpe1 != null && spriteGolpe2 != null)
        {
            sr.sprite = spriteGolpe1;
            yield return new WaitForSeconds(duracionGolpe);
            sr.sprite = spriteGolpe2;
            yield return new WaitForSeconds(duracionGolpe);
        }
        else
        {
            sr.color = Color.red;
            yield return new WaitForSeconds(0.1f);
            sr.color = Color.white;
        }
        sr.sprite = spriteNormal;
    }

    void Morir()
    {
        Sonidos.Tocar(sonidoMuerte);
        SoltarComodin();
        if (prefabMuerte != null)
            Instantiate(prefabMuerte, transform.position + Vector3.up * 0.5f, Quaternion.identity);
        Destroy(gameObject);
    }

    void SoltarComodin()
    {
        if (comodines == null || comodines.Length == 0) return;
        if (Random.value > probabilidadComodin) return;
        var elegido = comodines[Random.Range(0, comodines.Length)];
        if (elegido == null) return;
        // Cae en el piso, a la altura del carril donde estaba el robot
        Instantiate(elegido, new Vector3(transform.position.x, AlturaCarril + 0.6f, 0f), Quaternion.identity);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position + new Vector3(Mathf.Abs(salidaBala.x), salidaBala.y, 0f), 0.2f);
    }
}
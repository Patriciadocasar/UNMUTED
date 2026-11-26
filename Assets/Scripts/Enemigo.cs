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

    [Header("Picada (el Robot Espejo)")]
    public bool picada = false;              // ✅ = patrulla arriba y baja en picada si Harper se pone debajo
    public float alturaPatrulla = 9.5f;      // a qué altura patrulla (cerca de la pared, lejos de la guitarra)
    public float rangoAviso = 1.5f;          // qué tan debajo tiene que estar Harper para que se anime a bajar
    public float tiempoAviso = 1.2f;         // segundos titilando antes de bajar
    public float distanciaCancelar = 4f;     // si Harper se aleja más que esto durante el aviso, se arrepiente y no baja
    public float velocidadPicada = 9f;
    public float radioGolpe = 1.5f;          // tamaño del círculo del piso
    public int danoPicada = 1;
    public float tiempoMareado = 1.8f;       // segundos en el piso, el único momento en que se le puede pegar
    public float descansoEntrePicadas = 1.2f;
    public Sprite[] framesAviso, framesPicada, framesMareado;
    public AudioClip sonidoAviso, sonidoAterrizaje;

    enum EstadoPicada { Patrulla, Aviso, Bajando, Mareado, Subiendo }
    EstadoPicada estadoPicada = EstadoPicada.Patrulla;
    Vector2 objetivoPicada;          // a dónde se está moviendo
    Vector2 puntoImpacto;            // los pies de Harper cuando empezó el aviso
    float tiempoEstado, proximaPicada;
    int ladoPatrulla = -1;
    GameObject circulo;
    static Sprite spriteCirculo;

    // ¿Está mareado en el piso? (para que sea más fácil pegarle desde otro carril)
    public bool EstaMareado => picada && estadoPicada == EstadoPicada.Mareado;

    // Solo se le puede pegar cuando está mareado en el piso (o bailando por la Bola de Disco)
    public bool Invulnerable => picada && estadoPicada != EstadoPicada.Mareado && !Bailando;

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

        // El Robot Espejo, si lo pilla la Bola de Disco, se cae al piso bailando
        if (picada && Bailando)
        {
            BorrarCirculo();
            estadoPicada = EstadoPicada.Mareado;
            tiempoEstado = Time.time;
            objetivoPicada = new Vector2(transform.position.x, AlturaPiso(transform.position.y));
        }

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
        if (picada) { ActualizarPicada(); return; }

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

        if (picada)
        {
            float vel = estadoPicada == EstadoPicada.Bajando ? velocidadPicada
                      : estadoPicada == EstadoPicada.Subiendo ? velocidadPicada * 0.4f
                      : estadoPicada == EstadoPicada.Aviso ? velocidad * 2.5f : velocidad;
            rb.MovePosition(Vector2.MoveTowards(rb.position, objetivoPicada, vel * Time.fixedDeltaTime));
            return;
        }

        Vector2 p = rb.position + mover * velocidad * Time.fixedDeltaTime;
        p.x = Mathf.Clamp(p.x, xMin, xMax);
        float techo = volador ? alturaVueloMax : yMax + ajusteAltura;
        p.y = Mathf.Clamp(p.y, yMin + ajusteAltura, techo);
        rb.MovePosition(p);
    }

    // ================= ROBOT ESPEJO =================
    float AlturaPiso(float yHarperOPunto) => Mathf.Clamp(yHarperOPunto, yMin, yMax) + ajusteAltura;

    void ActualizarPicada()
    {
        Vector2 pos = transform.position;
        float t = Time.time - tiempoEstado;

        switch (estadoPicada)
        {
            case EstadoPicada.Patrulla:
                // Va de lado a lado arriba
                if (pos.x <= xMin + 0.5f) ladoPatrulla = 1;
                if (pos.x >= xMax - 0.5f) ladoPatrulla = -1;
                objetivoPicada = new Vector2(pos.x + ladoPatrulla * 2f, alturaPatrulla);
                // ¿Harper se puso debajo?
                if (Time.time >= proximaPicada && Mathf.Abs(harper.position.x - pos.x) < rangoAviso)
                {
                    estadoPicada = EstadoPicada.Aviso; tiempoEstado = Time.time;
                    puntoImpacto = harper.position;
                    MostrarCirculo(puntoImpacto);
                    Sonidos.Tocar(sonidoAviso);
                }
                break;

            case EstadoPicada.Aviso:
                // Se queda encima del círculo, titilando
                objetivoPicada = new Vector2(puntoImpacto.x, alturaPatrulla);
                PonerFrames(framesAviso, 10f);
                if (circulo != null)
                {
                    circulo.transform.localScale = new Vector3(radioGolpe * 2f, radioGolpe, 1f) * (1f + Mathf.Sin(Time.time * 20f) * 0.06f);
                    var rc = circulo.GetComponent<SpriteRenderer>();
                    rc.color = new Color(1f, 0.25f, 0.4f, 0.55f + 0.4f * Mathf.Abs(Mathf.Sin(Time.time * 10f)));   // late
                }
                // Titila en rojo para que se note que va a bajar
                if (sr != null) sr.color = ((int)(Time.time * 10f)) % 2 == 0 ? new Color(1f, 0.35f, 0.45f) : Color.white;
                // Si Harper se fue lejos, se arrepiente (si solo salió del círculo, igual baja y se estrella en el piso)
                if (Mathf.Abs(harper.position.x - puntoImpacto.x) > distanciaCancelar)
                {
                    if (sr != null) sr.color = Color.white;
                    BorrarCirculo();
                    estadoPicada = EstadoPicada.Patrulla; tiempoEstado = Time.time;
                    proximaPicada = Time.time + descansoEntrePicadas;
                }
                else if (t >= tiempoAviso)
                {
                    if (sr != null) sr.color = Color.white;
                    estadoPicada = EstadoPicada.Bajando; tiempoEstado = Time.time;
                    objetivoPicada = new Vector2(puntoImpacto.x, AlturaPiso(puntoImpacto.y));
                }
                break;

            case EstadoPicada.Bajando:
                PonerFrames(framesPicada, 10f);
                if (Vector2.Distance(pos, objetivoPicada) < 0.1f)
                {
                    // ¡Aterrizó! ¿Agarró a Harper?
                    var vida = harper.GetComponent<VidaHarper>();
                    if (vida != null && DentroDelCirculo(harper.position)) vida.RecibirDano(danoPicada);
                    Sonidos.Tocar(sonidoAterrizaje);
                    BorrarCirculo();
                    estadoPicada = EstadoPicada.Mareado; tiempoEstado = Time.time;
                    EstrellasMareo.Crear(transform, sr, tiempoMareado);
                }
                break;

            case EstadoPicada.Mareado:
                PonerFrames(framesMareado, 6f);
                if (t >= tiempoMareado)
                {
                    estadoPicada = EstadoPicada.Subiendo; tiempoEstado = Time.time;
                    objetivoPicada = new Vector2(pos.x, alturaPatrulla);
                }
                break;

            case EstadoPicada.Subiendo:
                if (Mathf.Abs(pos.y - alturaPatrulla) < 0.1f)
                {
                    estadoPicada = EstadoPicada.Patrulla; tiempoEstado = Time.time;
                    proximaPicada = Time.time + descansoEntrePicadas;
                }
                break;
        }
    }

    bool DentroDelCirculo(Vector2 pies)
    {
        float dx = (pies.x - puntoImpacto.x) / radioGolpe;
        float dy = (pies.y - puntoImpacto.y) / (radioGolpe * 0.5f);   // el círculo es un óvalo (se ve en perspectiva)
        return dx * dx + dy * dy <= 1f;
    }

    void PonerFrames(Sprite[] frames, float fps)
    {
        if (sr == null || frames == null || frames.Length == 0 || Time.time < finReaccion) return;
        sr.sprite = frames[(int)(Time.time * fps) % frames.Length];
    }

    void MostrarCirculo(Vector2 pies)
    {
        if (spriteCirculo == null) spriteCirculo = CrearSpriteCirculo();
        BorrarCirculo();
        circulo = new GameObject("CirculoPicada");
        circulo.transform.position = new Vector3(pies.x, pies.y, 0f);
        circulo.transform.localScale = new Vector3(radioGolpe * 2f, radioGolpe, 1f);
        var r = circulo.AddComponent<SpriteRenderer>();
        r.sprite = spriteCirculo;
        r.color = new Color(1f, 0.25f, 0.4f, 0.8f);
        r.sortingOrder = 20;  // encima del piso y de los props, para que se vea bien
    }

    void BorrarCirculo() { if (circulo != null) Destroy(circulo); circulo = null; }

    void OnDestroy() => BorrarCirculo();

    // Óvalo de pixel art (32x32 que se aplasta con la escala)
    static Sprite CrearSpriteCirculo()
    {
        int n = 32;
        var tex = new Texture2D(n, n) { filterMode = FilterMode.Point };
        for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
            {
                float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(n / 2f, n / 2f)) / (n / 2f);
                Color c = d > 1f ? Color.clear : d > 0.78f ? Color.white : new Color(1f, 1f, 1f, 0.35f);
                tex.SetPixel(x, y, c);
            }
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), n);
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
        if (Invulnerable) return;   // el Robot Espejo arriba no recibe daño
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
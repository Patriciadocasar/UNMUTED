using System.Collections;
using UnityEngine;

// El mini jefe de Aitana. Va en el mismo robot que el script Enemigo
// (Enemigo se encarga de la vida, los golpes y la explosión; este decide qué hace).
[RequireComponent(typeof(Enemigo))]
public class Aspirabot : MonoBehaviour
{
    [Header("Movimiento")]
    public float distanciaPreferida = 9f;    // se queda más o menos a esta distancia de Harper

    [Header("Succión")]
    public float cadaCuantoSucciona = 7f;    // segundos entre succiones
    public float tiempoAvisoSuccion = 1f;    // se le ilumina la boca antes de jalar
    public float duracionSuccion = 2.5f;     // cuánto rato jala
    [Range(0f, 1f)]
    public float fuerzaSuccion = 0.5f;       // 0.5 = la mitad de lo que corre Harper (si corre en contra, escapa)
    public Sprite[] framesSuccion;
    public AudioClip sonidoSuccion;

    [Header("Bombas")]
    public GameObject prefabBomba;
    public float cadaCuantoBomba = 3.5f;
    public Vector2 salidaBomba = new Vector2(1.5f, 3f);   // desde la mochila
    public Sprite[] framesLanzando;
    public AudioClip sonidoLanzar;

    [Header("Si Harper lo toca")]
    public int danoContacto = 3;
    public float fuerzaEmpujon = 25f;
    public float toleranciaCarril = 0.8f;    // solo duele si Harper choca en SU carril; por delante o por detrás se puede pasar

    [Header("Fase 2: mariposas (con la mitad de la vida)")]
    public GameObject[] prefabsMariposa;     // la azul y la rosa
    public int cantidadMariposas = 3;
    public Sprite[] framesDanado;            // se ve con grietas desde aquí
    public AudioClip sonidoFase2;

    Enemigo enemigo;
    SpriteRenderer sr;
    Collider2D miCuerpo;
    Transform harper;
    HarperMovimiento harperMov;
    VidaHarper harperVida;
    Collider2D cuerpoHarper;

    int vidaInicial;
    bool fase2;
    float proximaSuccion, proximaBomba, finLanzando;
    enum Estado { Normal, AvisoSuccion, Succionando }
    Estado estado = Estado.Normal;
    float inicioEstado;

    void Start()
    {
        enemigo = GetComponent<Enemigo>();
        enemigo.controlExterno = true;
        sr = GetComponentInChildren<SpriteRenderer>();
        miCuerpo = GetComponentInChildren<Collider2D>();
        vidaInicial = Mathf.Max(1, enemigo.vida);

        harperVida = FindAnyObjectByType<VidaHarper>();
        if (harperVida != null)
        {
            harper = harperVida.transform;
            harperMov = harperVida.GetComponent<HarperMovimiento>();
            cuerpoHarper = harperVida.GetComponentInChildren<Collider2D>();
        }
        proximaSuccion = Time.time + 4f;
        proximaBomba = Time.time + 2f;
    }

    void Update()
    {
        if (harper == null || enemigo == null) return;
        enemigo.moverExterno = Vector2.zero;

        RevisarFase2();

        // Bailando (Bola de Disco) o aturdido por un golpe: no hace nada
        if (enemigo.Bailando || enemigo.Aturdido) { estado = Estado.Normal; return; }

        RevisarContacto();

        float t = Time.time - inicioEstado;
        switch (estado)
        {
            case Estado.Normal:
                Moverse();
                if (Time.time >= proximaBomba) LanzarBomba();
                if (Time.time >= proximaSuccion)
                {
                    estado = Estado.AvisoSuccion; inicioEstado = Time.time;
                    Sonidos.Tocar(sonidoSuccion);
                    // Combo: a veces tira una bomba justo antes de succionar 😈
                    if (Random.value < 0.5f) LanzarBomba();
                }
                break;

            case Estado.AvisoSuccion:
                if (t >= tiempoAvisoSuccion) { estado = Estado.Succionando; inicioEstado = Time.time; }
                break;

            case Estado.Succionando:
                Jalar();
                if (t >= duracionSuccion)
                {
                    estado = Estado.Normal; inicioEstado = Time.time;
                    proximaSuccion = Time.time + cadaCuantoSucciona;
                }
                break;
        }
    }

    // Se pone a su distancia favorita y en el carril de Harper, despacito
    void Moverse()
    {
        float dx = harper.position.x - transform.position.x;
        float dy = harper.position.y + enemigo.ajusteAltura - transform.position.y;
        Vector2 m = Vector2.zero;
        if (Mathf.Abs(dx) > distanciaPreferida + 1f) m.x = Mathf.Sign(dx);
        else if (Mathf.Abs(dx) < distanciaPreferida - 1f) m.x = -Mathf.Sign(dx);
        if (Mathf.Abs(dy) > 0.4f) m.y = Mathf.Sign(dy) * 0.35f;   // sigue tu carril despacito
        // Si Harper está muy cerca, no se le pega al carril (así ella puede pasarle por un lado)
        if (Mathf.Abs(dx) < 4f) m.y = 0f;
        enemigo.moverExterno = m;
    }

    // La boca está en el borde del cuerpo, del lado de Harper, y a la altura de SUS PIES
    // (antes la jalaba hacia arriba, la sacaba de su carril y nunca la "tocaba")
    Vector2 Boca()
    {
        float lado = harper.position.x >= transform.position.x ? 1f : -1f;
        float borde = miCuerpo != null ? (lado > 0 ? miCuerpo.bounds.max.x : miCuerpo.bounds.min.x)
                                       : transform.position.x + lado * 2.6f;
        return new Vector2(borde, transform.position.y - enemigo.ajusteAltura);
    }

    void Jalar()
    {
        if (harperMov == null) return;
        Vector2 hacia = Boca() - (Vector2)harper.position;
        if (hacia.magnitude < 0.05f) return;
        // Sigue jalando hasta que la toca (el choque lo revisa RevisarContacto)
        harperMov.fuerzaExterna = hacia.normalized * harperMov.velocidadMax * fuerzaSuccion;
    }

    void LanzarBomba()
    {
        proximaBomba = Time.time + cadaCuantoBomba;
        if (prefabBomba == null) return;
        finLanzando = Time.time + 0.4f;
        Sonidos.Tocar(sonidoLanzar);
        float lado = harper.position.x >= transform.position.x ? 1f : -1f;
        Vector3 desde = transform.position + new Vector3(-lado * Mathf.Abs(salidaBomba.x), salidaBomba.y, 0f);
        // Cae cerca de Harper (no exacto, para que se pueda esquivar)
        Vector2 destino = (Vector2)harper.position + new Vector2(Random.Range(-1.2f, 1.2f), Random.Range(-0.6f, 0.6f));
        destino.x = Mathf.Clamp(destino.x, enemigo.xMin, enemigo.xMax);
        destino.y = Mathf.Clamp(destino.y, enemigo.yMin, enemigo.yMax);
        var b = Instantiate(prefabBomba, desde, Quaternion.identity).GetComponent<Bomba>();
        if (b != null) b.Lanzar(destino);
    }

    void RevisarContacto()
    {
        if (miCuerpo == null || harperVida == null) return;
        bool succionando = estado == Estado.Succionando;

        // ¿Harper está en su mismo carril? (si pasa por delante o por detrás en el piso, no duele)
        float carrilRobot = transform.position.y - enemigo.ajusteAltura;
        if (Mathf.Abs(harper.position.y - carrilRobot) > toleranciaCarril) return;

        // ¿Lo está tocando de lado? Se mide con el CUERPO de Harper, no con su centro,
        // porque los dos colliders chocan y su centro nunca llega a meterse en el robot.
        float margen = succionando ? 0.6f : 0.25f;
        float hMin = cuerpoHarper != null ? cuerpoHarper.bounds.min.x : harper.position.x;
        float hMax = cuerpoHarper != null ? cuerpoHarper.bounds.max.x : harper.position.x;
        if (hMax < miCuerpo.bounds.min.x - margen || hMin > miCuerpo.bounds.max.x + margen) return;

        bool leDolio = harperVida.RecibirDano(danoContacto);

        // Si la estaba succionando, la escupe SIEMPRE (aunque esté titilando),
        // para que no se quede pegada a la boca
        if ((leDolio || succionando) && harperMov != null)
        {
            // ¡Fuuu! La escupe lejos
            float lado = harper.position.x >= transform.position.x ? 1f : -1f;
            harperMov.Empujar(new Vector2(lado * fuerzaEmpujon, 0f));
            if (succionando)
            {
                estado = Estado.Normal; inicioEstado = Time.time;
                proximaSuccion = Time.time + cadaCuantoSucciona;
                harperMov.fuerzaExterna = Vector2.zero;
            }
        }
    }

    void RevisarFase2()
    {
        if (fase2 || enemigo.vida > vidaInicial / 2) return;
        fase2 = true;
        Debug.Log("Aspirabot: ¡FASE 2! Soltando " + cantidadMariposas + " mariposas");
        Sonidos.Tocar(sonidoFase2);
        if (framesDanado != null && framesDanado.Length > 0) enemigo.framesMovimiento = framesDanado;
        StartCoroutine(SoltarMariposas());
    }

    IEnumerator SoltarMariposas()
    {
        if (prefabsMariposa == null || prefabsMariposa.Length == 0)
        {
            Debug.LogWarning("Aspirabot: la lista Prefabs Mariposa está vacía, no hay mariposas que soltar");
            yield break;
        }
        for (int i = 0; i < cantidadMariposas; i++)
        {
            var p = prefabsMariposa[i % prefabsMariposa.Length];
            if (p != null) Instantiate(p, transform.position + new Vector3(0.5f, 2.5f, 0f), Quaternion.identity);
            yield return new WaitForSeconds(0.5f);
        }
    }

    // Pone los dibujos de succionar o lanzar encima de la animación normal
    void LateUpdate()
    {
        if (sr == null || enemigo == null || enemigo.Bailando) return;
        if ((estado == Estado.AvisoSuccion || estado == Estado.Succionando) && framesSuccion != null && framesSuccion.Length > 0)
            sr.sprite = framesSuccion[(int)(Time.time * 12f) % framesSuccion.Length];
        else if (Time.time < finLanzando && framesLanzando != null && framesLanzando.Length > 0)
            sr.sprite = framesLanzando[(int)(Time.time * 8f) % framesLanzando.Length];
    }

    void OnDisable()
    {
        if (harperMov != null) harperMov.fuerzaExterna = Vector2.zero;
    }
}
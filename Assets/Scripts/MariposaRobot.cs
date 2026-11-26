using UnityEngine;

// Mariposa robótica del nivel de Aitana.
// Vuela solo por arriba (pared y techo), suelta polvo brillante que hace daño,
// y SOLO muere con las estrellas del Escudo de Estrellas (la guitarra no le hace nada).
[RequireComponent(typeof(SpriteRenderer))]
public class MariposaRobot : MonoBehaviour
{
    public static int Vivas;    // ControlNivel espera a que sea 0 para dar la victoria

    [Header("Dibujos")]
    public Sprite[] framesAleteo;
    public Sprite spriteGolpe;
    public Sprite[] framesMuerte;
    public float fpsAleteo = 12f;

    [Header("Vida")]
    public int vida = 2;

    [Header("Vuelo (si dejas las alturas en 0, se calculan solas con la cámara)")]
    public float velocidad = 3f;
    public float alturaMin = 0f;
    public float alturaMax = 0f;

    [Header("Polvo brillante")]
    public float cadaCuantoPolvo = 2.5f;
    public int brillosPorVez = 5;
    public int danoPolvo = 1;
    public Color colorPolvo = new Color(1f, 0.85f, 1f);

    [Header("Sonidos")]
    public AudioClip sonidoGolpe;
    public AudioClip sonidoMuerte;

    SpriteRenderer sr;
    Vector2 objetivo;
    float proximoPolvo, finGolpe, tiempo, xMin, xMax;
    bool muriendo;
    float inicioMuerte;

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        if (sr.sortingOrder < 16) sr.sortingOrder = 16;
    }

    void OnEnable() { Vivas++; }
    void OnDisable() { Vivas--; }

    void Start()
    {
        var cam = Camera.main;
        float alto = cam != null ? cam.orthographicSize : 8.5f;
        float ancho = alto * (cam != null ? cam.aspect : 1.77f);
        Vector3 c = cam != null ? cam.transform.position : new Vector3(15f, 8.5f, 0f);
        xMin = c.x - ancho + 1.5f; xMax = c.x + ancho - 1.5f;

        // Por defecto: desde un poco arriba de la cabeza de Harper hasta casi el techo
        if (alturaMax <= 0f) alturaMax = c.y + alto - 1.5f;
        if (alturaMin <= 0f)
        {
            var mov = FindAnyObjectByType<HarperMovimiento>();
            alturaMin = mov != null ? mov.yMax + 4.5f : c.y + 1f;
            alturaMin = Mathf.Min(alturaMin, alturaMax - 1f);
        }
        ElegirObjetivo();
        proximoPolvo = Time.time + Random.Range(1.5f, cadaCuantoPolvo + 1.5f);
        tiempo = Random.value * 10f;
    }

    void Update()
    {
        if (muriendo) { AnimarMuerte(); return; }

        tiempo += Time.deltaTime;

        // Aleteo (o el destello si la golpearon)
        if (Time.time < finGolpe && spriteGolpe != null) sr.sprite = spriteGolpe;
        else if (framesAleteo != null && framesAleteo.Length > 0)
            sr.sprite = framesAleteo[(int)(tiempo * fpsAleteo) % framesAleteo.Length];

        // Vuela hacia un punto al azar, subiendo y bajando suavecito
        Vector2 pos = transform.position;
        if (Vector2.Distance(pos, objetivo) < 0.4f) ElegirObjetivo();
        pos = Vector2.MoveTowards(pos, objetivo, velocidad * Time.deltaTime);
        pos.y += Mathf.Sin(tiempo * 4f) * 0.01f;
        transform.position = pos;
        sr.flipX = objetivo.x > pos.x;

        // Suelta polvo brillante
        if (Time.time >= proximoPolvo)
        {
            proximoPolvo = Time.time + cadaCuantoPolvo + Random.Range(-0.7f, 0.7f);
            for (int i = 0; i < brillosPorVez; i++)
                PolvoBrillante.Crear(transform.position + new Vector3(Random.Range(-0.6f, 0.6f), -0.3f, 0f), colorPolvo, danoPolvo, i * 0.08f);
        }
    }

    void ElegirObjetivo()
    {
        objetivo = new Vector2(Random.Range(xMin, xMax), Random.Range(alturaMin, alturaMax));
    }

    // La llaman las estrellas del escudo
    public void RecibirDano(int d)
    {
        if (muriendo) return;
        vida -= d;
        finGolpe = Time.time + 0.12f;
        Sonidos.Tocar(sonidoGolpe);
        if (vida <= 0)
        {
            muriendo = true; inicioMuerte = Time.time;
            Sonidos.Tocar(sonidoMuerte);
        }
    }

    void AnimarMuerte()
    {
        float t = Time.time - inicioMuerte;
        if (framesMuerte == null || framesMuerte.Length == 0 || t >= framesMuerte.Length * 0.1f) { Destroy(gameObject); return; }
        sr.sprite = framesMuerte[(int)(t / 0.1f)];
    }
}

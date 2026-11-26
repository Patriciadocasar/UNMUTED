using UnityEngine;

// La bomba del Aspirabot: vuela en arco, cae, titila cada vez más rápido y explota.
// Va en el prefab de la bomba (con un Sprite Renderer).
[RequireComponent(typeof(SpriteRenderer))]
public class Bomba : MonoBehaviour
{
    [Header("Dibujos")]
    public Sprite[] frames;               // 0 = normal, 1 = titila rojo, 2 = titila blanco

    [Header("Vuelo")]
    public float tiempoVuelo = 0.8f;
    public float alturaArco = 3f;

    [Header("Explosión")]
    public float tiempoMecha = 2f;        // cuánto titila en el piso antes de explotar
    public float radio = 1.6f;            // tamaño del círculo rojo
    public int dano = 2;
    public GameObject prefabExplosion;
    public AudioClip sonidoExplosion;
    public AudioClip sonidoCaer;

    SpriteRenderer sr;
    Vector2 inicio, destino;
    float t;
    bool enPiso;
    GameObject circulo;
    static Sprite spriteCirculo;

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        if (sr.sortingOrder < 15) sr.sortingOrder = 15;
    }

    public void Lanzar(Vector2 adonde)
    {
        inicio = transform.position;
        destino = adonde;
        t = 0f;
    }

    void Update()
    {
        t += Time.deltaTime;

        if (!enPiso)
        {
            // Vuela en arco
            float p = Mathf.Clamp01(t / tiempoVuelo);
            Vector2 pos = Vector2.Lerp(inicio, destino + Vector2.up * 0.3f, p);
            pos.y += Mathf.Sin(p * Mathf.PI) * alturaArco;
            transform.position = pos;
            transform.rotation = Quaternion.Euler(0f, 0f, -p * 360f);
            if (p >= 1f)
            {
                enPiso = true; t = 0f;
                transform.rotation = Quaternion.identity;
                Sonidos.Tocar(sonidoCaer);
                MostrarCirculo();
            }
            return;
        }

        // En el piso: titila cada vez más rápido
        float avance = t / tiempoMecha;
        float velTitileo = Mathf.Lerp(4f, 18f, avance);
        if (frames != null && frames.Length >= 3)
            sr.sprite = ((int)(t * velTitileo)) % 2 == 0 ? frames[0] : frames[avance > 0.6f ? 2 : 1];
        if (circulo != null)
        {
            var rc = circulo.GetComponent<SpriteRenderer>();
            rc.color = new Color(1f, 0.25f, 0.4f, 0.4f + 0.5f * Mathf.Abs(Mathf.Sin(t * velTitileo)));
        }

        if (t >= tiempoMecha) Explotar();
    }

    void Explotar()
    {
        Sonidos.Tocar(sonidoExplosion);
        if (prefabExplosion != null) Instantiate(prefabExplosion, transform.position, Quaternion.identity);

        // ¿Harper estaba dentro del círculo?
        var vida = FindAnyObjectByType<VidaHarper>();
        if (vida != null)
        {
            Vector2 pies = vida.transform.position;
            float dx = (pies.x - destino.x) / radio;
            float dy = (pies.y - destino.y) / (radio * 0.5f);
            if (dx * dx + dy * dy <= 1f) vida.RecibirDano(dano);
        }
        Destroy(gameObject);
    }

    void MostrarCirculo()
    {
        if (spriteCirculo == null) spriteCirculo = CrearSpriteCirculo();
        circulo = new GameObject("CirculoBomba");
        circulo.transform.position = new Vector3(destino.x, destino.y, 0f);
        circulo.transform.localScale = new Vector3(radio * 2f, radio, 1f);
        var r = circulo.AddComponent<SpriteRenderer>();
        r.sprite = spriteCirculo;
        r.sortingOrder = 14;
    }

    void OnDestroy() { if (circulo != null) Destroy(circulo); }

    static Sprite CrearSpriteCirculo()
    {
        int n = 32;
        var tex = new Texture2D(n, n) { filterMode = FilterMode.Point };
        for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
            {
                float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(n / 2f, n / 2f)) / (n / 2f);
                tex.SetPixel(x, y, d > 1f ? Color.clear : d > 0.78f ? Color.white : new Color(1f, 1f, 1f, 0.35f));
            }
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), n);
    }
}

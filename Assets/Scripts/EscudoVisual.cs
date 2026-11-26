using UnityEngine;

// La burbuja dorada del Escudo de Estrellas, con estrellitas girando alrededor de Harper.
// No hay que ponerlo en ningún objeto: HarperAtaque lo crea solo.
public class EscudoVisual : MonoBehaviour
{
    static Sprite spriteBurbuja, spriteEstrella;

    float fin, inicio;
    SpriteRenderer burbuja;
    Transform[] estrellas;
    SpriteRenderer[] estrellasSr;

    Sprite[] frames;     // la animación de pixel art (si viene)

    public static void Mostrar(Transform harper, float altura, float duracion, Sprite[] frames = null)
    {
        // Si ya había un escudo, solo se alarga
        var viejo = harper.GetComponentInChildren<EscudoVisual>();
        if (viejo != null) { viejo.fin = Time.time + duracion; return; }

        if (spriteBurbuja == null) CrearSprites();
        var go = new GameObject("EscudoVisual");
        go.transform.SetParent(harper, false);
        float escala = Mathf.Max(0.01f, harper.lossyScale.y);
        go.transform.localPosition = new Vector3(0f, altura / escala, 0f);

        var e = go.AddComponent<EscudoVisual>();
        e.fin = Time.time + duracion;
        e.inicio = Time.time;
        e.frames = (frames != null && frames.Length > 0) ? frames : null;
        e.Armar(escala);
    }

    void Armar(float escalaPadre)
    {
        var b = new GameObject("burbuja");
        b.transform.SetParent(transform, false);
        b.transform.localScale = new Vector3(3.2f, 3.8f, 1f) / escalaPadre;   // óvalo alrededor del cuerpo
        burbuja = b.AddComponent<SpriteRenderer>();
        burbuja.sprite = frames != null ? frames[0] : spriteBurbuja;
        burbuja.sortingOrder = 24;
        if (frames != null)
        {
            b.transform.localScale = Vector3.one / escalaPadre;   // el dibujo ya tiene su tamaño
            estrellas = new Transform[0]; estrellasSr = new SpriteRenderer[0];   // las estrellas ya vienen dibujadas
            return;
        }

        estrellas = new Transform[5];
        estrellasSr = new SpriteRenderer[5];
        Color[] col = { new Color(1f, 0.9f, 0.35f), new Color(1f, 0.75f, 0.9f), new Color(0.7f, 0.9f, 1f), new Color(1f, 0.9f, 0.35f), new Color(1f, 1f, 1f) };
        for (int i = 0; i < 5; i++)
        {
            var s = new GameObject("estrella");
            s.transform.SetParent(transform, false);
            s.transform.localScale = Vector3.one / escalaPadre;
            var r = s.AddComponent<SpriteRenderer>();
            r.sprite = spriteEstrella; r.color = col[i]; r.sortingOrder = 25;
            estrellas[i] = s.transform; estrellasSr[i] = r;
        }
    }

    void Update()
    {
        float resta = fin - Time.time;
        if (resta <= 0f) { Destroy(gameObject); return; }

        float escala = transform.parent != null ? Mathf.Max(0.01f, transform.parent.lossyScale.y) : 1f;
        float t = Time.time - inicio;

        // Aparece creciendo, late suavecito y parpadea en el último segundo
        float entrada = Mathf.Clamp01(t / 0.25f);
        float latido = 1f + Mathf.Sin(t * 6f) * 0.04f;
        bool visible = resta > 1f || ((int)(t * 12f)) % 2 == 0;
        if (frames != null)
        {
            burbuja.sprite = frames[(int)(t * 11f) % frames.Length];
            burbuja.transform.localScale = Vector3.one * entrada * latido / escala;
            burbuja.color = new Color(1f, 1f, 1f, visible ? 1f : 0.15f);
            return;
        }
        burbuja.transform.localScale = new Vector3(3.2f, 3.8f, 1f) * entrada * latido / escala;
        float alfa = visible ? 0.55f + 0.2f * Mathf.Sin(t * 8f) : 0.1f;
        burbuja.color = new Color(1f, 0.85f, 0.35f, alfa);

        // Estrellas girando en un óvalo alrededor de Harper
        for (int i = 0; i < estrellas.Length; i++)
        {
            float a = t * 3.5f + i * Mathf.PI * 2f / estrellas.Length;
            estrellas[i].localPosition = new Vector3(Mathf.Cos(a) * 1.7f, Mathf.Sin(a) * 2f, 0f) / escala;
            estrellas[i].localRotation = Quaternion.Euler(0f, 0f, t * 300f);
            estrellasSr[i].enabled = visible;
        }
    }

    static void CrearSprites()
    {
        // Burbuja: óvalo con borde brillante y relleno suave (32x32)
        int n = 32;
        var tex = new Texture2D(n, n) { filterMode = FilterMode.Point };
        for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
            {
                float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(n / 2f, n / 2f)) / (n / 2f);
                Color c = d > 1f ? Color.clear : d > 0.86f ? Color.white : new Color(1f, 1f, 1f, 0.18f + 0.25f * d * d);
                if (d < 0.5f && x < n / 2 - 4 && y > n / 2 + 4 && d > 0.35f) c = new Color(1f, 1f, 1f, 0.7f);   // reflejo
                tex.SetPixel(x, y, c);
            }
        tex.Apply();
        spriteBurbuja = Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), n);

        // Estrellita de 4 puntas (7x7)
        string[] dib = { "...#...", "...#...", "..###..", "#######", "..###..", "...#...", "...#..." };
        var t2 = new Texture2D(7, 7) { filterMode = FilterMode.Point };
        for (int y = 0; y < 7; y++)
            for (int x = 0; x < 7; x++)
                t2.SetPixel(x, 6 - y, dib[y][x] == '#' ? Color.white : Color.clear);
        t2.Apply();
        spriteEstrella = Sprite.Create(t2, new Rect(0, 0, 7, 7), new Vector2(0.5f, 0.5f), 12f);
    }
}

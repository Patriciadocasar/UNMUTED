using System.Collections.Generic;
using UnityEngine;

// Poder "Coro de Siete" (BTS): 7 notas musicales que salen en abanico y persiguen solas a los robots.
// No hay que ponerlo en ningún objeto ni crear prefabs: HarperAtaque lo llama y las notas se dibujan solas.
public class NotaCoro : MonoBehaviour
{
    static Sprite spriteNota, spriteBrillo;

    // 7 tonos del morado al rosado
    static readonly Color[] colores = {
        new Color(0.62f, 0.38f, 1f),  new Color(0.74f, 0.45f, 1f),  new Color(0.86f, 0.52f, 1f),
        new Color(1f, 0.55f, 0.95f),  new Color(1f, 0.62f, 0.85f),  new Color(0.8f, 0.6f, 1f),
        new Color(0.68f, 0.5f, 0.98f)
    };

    // ---------- Lanzar el coro ----------
    public static void LanzarCoro(Vector3 origen, int direccion, int danoPorNota, float velocidad, GameObject prefabImpacto,
                                  Sprite spritePropio = null)
    {
        if (spriteBrillo == null) CrearSprites();
        Sprite dibujo = spritePropio != null ? spritePropio : spriteNota;   // si pusiste una imagen, usa esa

        // Repartir las notas entre los robots vivos
        var robots = new List<Enemigo>(FindObjectsByType<Enemigo>());

        for (int i = 0; i < 7; i++)
        {
            // Abanico de -60° a +60° hacia donde mira Harper
            float ang = Mathf.Lerp(-60f, 60f, i / 6f) * Mathf.Deg2Rad;
            Vector2 v = new Vector2(Mathf.Cos(ang) * direccion, Mathf.Sin(ang)) * velocidad;

            var go = new GameObject("Nota del coro");
            go.transform.position = origen;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = dibujo;
            sr.color = colores[i];
            sr.sortingOrder = 22;

            var n = go.AddComponent<NotaCoro>();
            n.velocidad = v;
            n.rapidez = velocidad;
            n.dano = danoPorNota;
            n.impacto = prefabImpacto;
            n.color = colores[i];
            n.espera = i * 0.04f;                                   // salen una tras otra, como una ráfaga
            n.objetivo = robots.Count > 0 ? robots[i % robots.Count] : null;
            go.SetActive(true);
        }
    }

    // ---------- Cada nota ----------
    Vector2 velocidad;
    float rapidez, espera, vida, proximoBrillo;
    int dano;
    GameObject impacto;
    Color color;
    Enemigo objetivo;
    SpriteRenderer sr;

    void Awake() { sr = GetComponent<SpriteRenderer>(); }

    void Update()
    {
        if (esBrillo) { ActualizarBrillo(); return; }
        if (espera > 0f) { espera -= Time.deltaTime; sr.enabled = false; return; }
        sr.enabled = true;
        vida += Time.deltaTime;
        if (vida > 4f) { Destroy(gameObject); return; }

        // Si su robot murió, busca el más cercano
        if (objetivo == null) objetivo = MasCercano();

        // Los primeros instantes vuelan en abanico; después persiguen a su robot
        if (objetivo != null && vida > 0.2f)
        {
            Vector2 hacia = (CentroDe(objetivo) - (Vector2)transform.position).normalized * rapidez;
            velocidad = Vector2.Lerp(velocidad, hacia, Time.deltaTime * 6f);
            velocidad = velocidad.normalized * rapidez;
        }
        transform.position += (Vector3)(velocidad * Time.deltaTime);

        // Se balancean y laten un poquito
        transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Sin(vida * 14f) * 15f);
        transform.localScale = Vector3.one * (1f + Mathf.Sin(vida * 20f) * 0.08f);

        // Estelita de brillos
        if (Time.time >= proximoBrillo)
        {
            proximoBrillo = Time.time + 0.04f;
            CrearBrillo(transform.position, color);
        }

        // ¿Tocó a algún robot?
        foreach (var e in FindObjectsByType<Enemigo>())
        {
            var col = e.GetComponentInChildren<Collider2D>();
            if (col == null) continue;
            var caja = col.bounds; caja.Expand(0.3f);
            if (!caja.Contains(new Vector3(transform.position.x, transform.position.y, caja.center.z))) continue;

            e.RecibirDano(dano);
            if (impacto != null) Instantiate(impacto, transform.position, Quaternion.identity);
            Destroy(gameObject);
            return;
        }

        // Fuera de la pantalla: desaparece
        var cam = Camera.main;
        if (cam != null)
        {
            Vector3 vp = cam.WorldToViewportPoint(transform.position);
            if (vp.x < -0.15f || vp.x > 1.15f || vp.y < -0.15f || vp.y > 1.15f) Destroy(gameObject);
        }
    }

    Enemigo MasCercano()
    {
        Enemigo mejor = null; float dMin = float.MaxValue;
        foreach (var e in FindObjectsByType<Enemigo>())
        {
            float d = ((Vector2)e.transform.position - (Vector2)transform.position).sqrMagnitude;
            if (d < dMin) { dMin = d; mejor = e; }
        }
        return mejor;
    }

    static Vector2 CentroDe(Enemigo e)
    {
        var col = e.GetComponentInChildren<Collider2D>();
        return col != null ? (Vector2)col.bounds.center : (Vector2)e.transform.position;
    }

    // ---------- Dibujos en pixel art ----------
    static void CrearSprites()
    {
        // Corchea de 12x14: O = borde oscuro, F = relleno (se pinta con el color de cada nota), H = brillo
        string[] nota = {
            "....OOOO....",
            "....OHFFOO..",
            "....OFOOFFO.",
            "....OFO.OOFO",
            "....OFO...OO",
            "....OFO.....",
            "....OFO.....",
            "....OFO.....",
            ".OOOOFO.....",
            "OHHFFFO.....",
            "OHFFFFO.....",
            "OFFFFFO.....",
            ".OFFFO......",
            "..OOO.......",
        };
        int w = nota[0].Length, h = nota.Length;
        var tex = new Texture2D(w, h) { filterMode = FilterMode.Point };
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                char c = nota[y][x];
                Color col = c == 'O' ? new Color(0.12f, 0.08f, 0.18f) :
                            c == 'F' ? Color.white :
                            c == 'H' ? new Color(1f, 1f, 1f) : Color.clear;
                tex.SetPixel(x, h - 1 - y, col);
            }
        tex.Apply();
        spriteNota = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 12f);

        var t2 = new Texture2D(2, 2) { filterMode = FilterMode.Point };
        t2.SetPixels(new[] { Color.white, Color.white, Color.white, Color.white }); t2.Apply();
        spriteBrillo = Sprite.Create(t2, new Rect(0, 0, 2, 2), new Vector2(0.5f, 0.5f), 16f);
    }

    // ---------- Puntito de brillo que se desvanece (la estelita) ----------
    bool esBrillo;
    float tBrillo;
    Color colorBrillo;

    static void CrearBrillo(Vector3 pos, Color color)
    {
        var go = new GameObject("brillo");
        go.transform.position = pos + (Vector3)Random.insideUnitCircle * 0.2f;
        var r = go.AddComponent<SpriteRenderer>();
        r.sprite = spriteBrillo; r.sortingOrder = 21;
        var b = go.AddComponent<NotaCoro>();
        b.esBrillo = true;
        b.colorBrillo = Color.Lerp(color, Color.white, 0.5f);
        r.color = b.colorBrillo;
    }

    void ActualizarBrillo()
    {
        tBrillo += Time.deltaTime;
        if (tBrillo > 0.35f) { Destroy(gameObject); return; }
        sr.color = new Color(colorBrillo.r, colorBrillo.g, colorBrillo.b, 1f - tBrillo / 0.35f);
        transform.position += Vector3.down * Time.deltaTime * 0.5f;
    }
}
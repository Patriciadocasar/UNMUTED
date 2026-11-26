using UnityEngine;

// Estrellitas que salen del Escudo de Estrellas (nivel de Aitana).
// Persiguen SOLO a las mariposas robóticas. No hay que ponerlo en ningún objeto.
public class EstrellaEscudo : MonoBehaviour
{
    static Sprite sprite;
    MariposaRobot objetivo;
    Vector2 velocidad;
    float rapidez, vida;
    int dano;
    SpriteRenderer sr;

    public static void Lanzar(Vector3 desde, int dano, float rapidez)
    {
        // Busca la mariposa más cercana; si no hay ninguna, no sale nada
        MariposaRobot mejor = null; float dMin = float.MaxValue;
        foreach (var m in FindObjectsByType<MariposaRobot>())
        {
            float d = ((Vector2)m.transform.position - (Vector2)desde).sqrMagnitude;
            if (d < dMin) { dMin = d; mejor = m; }
        }
        if (mejor == null) return;

        if (sprite == null) sprite = CrearSprite();
        var go = new GameObject("EstrellaEscudo");
        go.transform.position = desde;
        var r = go.AddComponent<SpriteRenderer>();
        r.sprite = sprite; r.color = new Color(1f, 0.88f, 0.35f); r.sortingOrder = 23;
        var e = go.AddComponent<EstrellaEscudo>();
        e.objetivo = mejor; e.dano = dano; e.rapidez = rapidez;
        e.velocidad = new Vector2(Random.Range(-1f, 1f), 1.5f).normalized * rapidez;   // sale hacia arriba
    }

    void Awake() { sr = GetComponent<SpriteRenderer>(); }

    void Update()
    {
        vida += Time.deltaTime;
        if (vida > 4f) { Destroy(gameObject); return; }
        if (objetivo == null)
        {
            // Su mariposa ya murió: busca otra
            float dMin = float.MaxValue;
            foreach (var m in FindObjectsByType<MariposaRobot>())
            {
                float d = ((Vector2)m.transform.position - (Vector2)transform.position).sqrMagnitude;
                if (d < dMin) { dMin = d; objetivo = m; }
            }
            if (objetivo == null) { Destroy(gameObject); return; }
        }

        Vector2 hacia = ((Vector2)objetivo.transform.position - (Vector2)transform.position).normalized * rapidez;
        velocidad = Vector2.Lerp(velocidad, hacia, Time.deltaTime * 5f).normalized * rapidez;
        transform.position += (Vector3)(velocidad * Time.deltaTime);
        transform.Rotate(0f, 0f, 540f * Time.deltaTime);   // gira

        if (Vector2.Distance(transform.position, objetivo.transform.position) < 0.6f)
        {
            objetivo.RecibirDano(dano);
            Destroy(gameObject);
        }
    }

    // Estrella de 5 puntas en pixel art (9x9)
    static Sprite CrearSprite()
    {
        string[] d = {
            "....#....",
            "....#....",
            "...###...",
            "#########",
            ".#######.",
            "..#####..",
            "..##.##..",
            ".##...##.",
            ".#.....#.",
        };
        var tex = new Texture2D(9, 9) { filterMode = FilterMode.Point };
        for (int y = 0; y < 9; y++)
            for (int x = 0; x < 9; x++)
                tex.SetPixel(x, 8 - y, d[y][x] == '#' ? Color.white : Color.clear);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, 9, 9), new Vector2(0.5f, 0.5f), 12f);
    }
}

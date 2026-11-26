using UnityEngine;

// Brillito que sueltan las mariposas robóticas: cae, titila y, si toca a Harper, le quita vida.
// No hay que ponerlo en ningún objeto: la mariposa lo crea sola.
public class PolvoBrillante : MonoBehaviour
{
    static Sprite sprite;

    int dano;
    float espera, vida, deriva, pisoY;
    Color color;
    SpriteRenderer sr;
    Collider2D cuerpoHarper;
    VidaHarper harper;

    public static void Crear(Vector3 pos, Color color, int dano, float espera)
    {
        if (sprite == null) sprite = CrearSprite();
        var go = new GameObject("PolvoBrillante");
        go.transform.position = pos;
        var r = go.AddComponent<SpriteRenderer>();
        r.sprite = sprite; r.color = color; r.sortingOrder = 17; r.enabled = false;
        var p = go.AddComponent<PolvoBrillante>();
        p.dano = dano; p.espera = espera; p.color = color;
        p.deriva = Random.Range(-0.4f, 0.4f);
    }

    void Start()
    {
        sr = GetComponent<SpriteRenderer>();
        harper = FindAnyObjectByType<VidaHarper>();
        if (harper != null) cuerpoHarper = harper.GetComponentInChildren<Collider2D>();
        var mov = FindAnyObjectByType<HarperMovimiento>();
        pisoY = mov != null ? mov.yMin - 0.5f : 0f;   // se apaga al llegar al piso
    }

    void Update()
    {
        if (espera > 0f) { espera -= Time.deltaTime; return; }
        sr.enabled = true;
        vida += Time.deltaTime;

        // Cae meciéndose y titilando
        transform.position += new Vector3(Mathf.Sin(vida * 5f) * deriva, -3f, 0f) * Time.deltaTime;
        float brillo = 0.6f + 0.4f * Mathf.Abs(Mathf.Sin(vida * 14f));
        sr.color = new Color(color.r, color.g, color.b, brillo);

        // ¿Tocó a Harper?
        if (cuerpoHarper != null && cuerpoHarper.bounds.Contains(new Vector3(transform.position.x, transform.position.y, cuerpoHarper.bounds.center.z)))
        {
            harper.RecibirDano(dano);
            Destroy(gameObject);
            return;
        }

        if (transform.position.y < pisoY || vida > 6f) Destroy(gameObject);
    }

    // Destellito de 4 puntas (5x5)
    static Sprite CrearSprite()
    {
        string[] d = { "..#..", ".###.", "#####", ".###.", "..#.." };
        var tex = new Texture2D(5, 5) { filterMode = FilterMode.Point };
        for (int y = 0; y < 5; y++)
            for (int x = 0; x < 5; x++)
                tex.SetPixel(x, 4 - y, d[y][x] == '#' ? Color.white : Color.clear);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, 5, 5), new Vector2(0.5f, 0.5f), 12f);
    }
}

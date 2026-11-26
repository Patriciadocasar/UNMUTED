using UnityEngine;

// Estrellitas de colores que giran sobre la cabeza de un robot mareado.
// No hay que ponerlo en ningún objeto: el robot lo crea solo cuando le cae la Bola de Disco.
public class EstrellasMareo : MonoBehaviour
{
    static Sprite estrella;
    static readonly Color[] colores = {
        new Color(1f, 0.47f, 0.86f),   // rosa
        new Color(1f, 0.9f, 0.35f),    // amarillo
        new Color(0.43f, 0.94f, 0.9f)  // turquesa
    };

    float fin;
    Transform[] estrellas;

    public static void Crear(Transform robot, SpriteRenderer srRobot, float segundos)
    {
        // Si ya estaba mareado, solo se alarga el mareo
        var viejo = robot.GetComponentInChildren<EstrellasMareo>();
        if (viejo != null) { viejo.fin = Time.time + segundos; return; }

        var go = new GameObject("EstrellasMareo");
        go.transform.SetParent(robot, false);

        // Justo encima de la cabeza del robot
        float arriba = srRobot != null ? srRobot.bounds.max.y - robot.position.y + 0.3f : 1.8f;
        float escala = Mathf.Max(0.01f, robot.lossyScale.y);
        go.transform.localPosition = new Vector3(0f, arriba / escala, 0f);

        var m = go.AddComponent<EstrellasMareo>();
        m.fin = Time.time + segundos;
        m.Armar(escala);
    }

    void Armar(float escalaPadre)
    {
        if (estrella == null) estrella = CrearSpriteEstrella();
        estrellas = new Transform[colores.Length];
        for (int i = 0; i < colores.Length; i++)
        {
            var e = new GameObject("estrella");
            e.transform.SetParent(transform, false);
            e.transform.localScale = Vector3.one / escalaPadre;   // que no se deformen con el tamaño del robot
            var r = e.AddComponent<SpriteRenderer>();
            r.sprite = estrella;
            r.color = colores[i];
            r.sortingOrder = 25;
            estrellas[i] = e.transform;
        }
    }

    void Update()
    {
        if (Time.time > fin) { Destroy(gameObject); return; }

        // Giran en un óvalo, como en las caricaturas
        for (int i = 0; i < estrellas.Length; i++)
        {
            float a = Time.time * 5f + i * Mathf.PI * 2f / estrellas.Length;
            estrellas[i].localPosition = new Vector3(Mathf.Cos(a) * 0.7f, Mathf.Sin(a) * 0.2f, 0f) / transform.lossyScale.x;
            float brillo = 0.8f + 0.2f * Mathf.Sin(Time.time * 12f + i);   // titilan un poquito
            estrellas[i].localScale = Vector3.one * brillo / transform.parent.lossyScale.x;
        }
    }

    // Dibuja una estrellita de 4 puntas en pixel art (7x7)
    static Sprite CrearSpriteEstrella()
    {
        string[] dibujo = {
            "...#...",
            "...#...",
            "..###..",
            "#######",
            "..###..",
            "...#...",
            "...#...",
        };
        var tex = new Texture2D(7, 7) { filterMode = FilterMode.Point };
        for (int y = 0; y < 7; y++)
            for (int x = 0; x < 7; x++)
                tex.SetPixel(x, 6 - y, dibujo[y][x] == '#' ? Color.white : Color.clear);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, 7, 7), new Vector2(0.5f, 0.5f), 16f);
    }
}
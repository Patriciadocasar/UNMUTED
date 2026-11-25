using UnityEngine;
using UnityEngine.Tilemaps;

// Define el rectángulo del piso por donde pueden caminar Harper y los robots.
// HarperMovimiento y Enemigo ya recortan su posición con xMin/xMax/yMin/yMax;
// este componente calcula esos límites a partir del Tilemap "Piso" y se los asigna a todos.
// En la vista Scene dibuja la zona en verde para que se vea qué es piso y qué es pared.
[DefaultExecutionOrder(-100)]   // corre antes que GestorOleadas, así los robots clonados heredan los límites
public class ZonaCaminable : MonoBehaviour
{
    [Header("Calcular desde el Tilemap del piso")]
    public bool usarTilemapPiso = true;
    public Tilemap piso;
    [Tooltip("Filas de arriba del piso que NO se pisan (el zócalo dorado = 1).")]
    public int filasDeFondo = 1;
    [Tooltip("Margen a los lados para que Harper no quede medio fuera de cámara.")]
    public float margenLados = 2f;
    [Tooltip("Margen bajo la fila más alta pisable (los pies no tocan la pared).")]
    public float margenArriba = 0.5f;
    [Tooltip("Margen sobre el borde inferior de la cámara.")]
    public float margenAbajo = 0.3f;

    [Header("Límites (se llenan solos si usas el Tilemap)")]
    public float xMin = 2f, xMax = 28f, yMin = 0.3f, yMax = 5.5f;

    void Awake()
    {
        Calcular();
        Aplicar();
    }

    void OnValidate() => Calcular();

    public void Calcular()
    {
        if (!usarTilemapPiso) return;
        if (piso == null)
        {
            foreach (var t in FindObjectsByType<Tilemap>(FindObjectsSortMode.None))
                if (t.gameObject.name == "Piso") { piso = t; break; }
            if (piso == null) return;
        }
        piso.CompressBounds();
        var b = piso.cellBounds;           // en celdas (1 celda = 1 unidad)
        if (b.size.x == 0 || b.size.y == 0) return;

        float abajo = b.yMin;
        var cam = Camera.main;
        if (cam != null && cam.orthographic)
            abajo = Mathf.Max(abajo, cam.transform.position.y - cam.orthographicSize);

        xMin = b.xMin + margenLados;
        xMax = b.xMax - margenLados;
        yMin = abajo + margenAbajo;
        yMax = (b.yMax - filasDeFondo) - margenArriba;
    }

    public void Aplicar()
    {
        foreach (var h in FindObjectsByType<HarperMovimiento>(FindObjectsSortMode.None))
        {
            h.xMin = xMin; h.xMax = xMax; h.yMin = yMin; h.yMax = yMax;
        }
        // Incluye el molde inactivo de GestorOleadas si ya existe
        foreach (var e in FindObjectsByType<Enemigo>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            e.xMin = xMin; e.xMax = xMax; e.yMin = yMin; e.yMax = yMax;
        }
    }

    void OnDrawGizmos()
    {
        var c = new Vector3((xMin + xMax) / 2f, (yMin + yMax) / 2f, 0f);
        var s = new Vector3(xMax - xMin, yMax - yMin, 0f);
        Gizmos.color = new Color(0.2f, 1f, 0.3f, 0.15f);
        Gizmos.DrawCube(c, s);
        Gizmos.color = new Color(0.2f, 1f, 0.3f, 1f);
        Gizmos.DrawWireCube(c, s);
    }
}

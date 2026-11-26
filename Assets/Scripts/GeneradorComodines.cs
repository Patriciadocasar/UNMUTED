using UnityEngine;

// Pon este script en un objeto vacío llamado "GeneradorComodines" en el nivel.
// Cada cierto tiempo hace aparecer un comodín en un lugar al azar del piso.
public class GeneradorComodines : MonoBehaviour
{
    [Header("Qué comodines pueden salir")]
    public GameObject[] comodines;           // arrastra aquí Comodin Nota y/o Comodin Corazon

    [Header("Cada cuánto aparecen (segundos)")]
    public float primerComodin = 8f;         // el primero sale a los 8 segundos
    public float tiempoMinimo = 12f;
    public float tiempoMaximo = 18f;
    public int maximoEnPantalla = 1;         // para que no se llene el piso

    [Header("Dónde pueden aparecer (el piso)")]
    public float xMin = 3f, xMax = 27f;
    public float yMin = 0.3f, yMax = 4.5f;   // igual que los límites de Harper

    float proximo;

    void Start() { proximo = Time.time + primerComodin; }

    void Update()
    {
        if (comodines == null || comodines.Length == 0 || Time.time < proximo) return;
        proximo = Time.time + Random.Range(tiempoMinimo, tiempoMaximo);

        if (FindObjectsByType<Enemigo>().Length == 0) return;                  // el nivel ya terminó
        if (FindObjectsByType<Comodin>().Length >= maximoEnPantalla) return;    // ya hay uno esperando

        var harper = FindAnyObjectByType<VidaHarper>();
        if (harper == null || harper.Actual <= 0) return;

        // Un lugar al azar del piso, que no quede pegado a Harper (para que haya que ir a buscarlo)
        Vector3 pos = Vector3.zero;
        for (int i = 0; i < 10; i++)
        {
            pos = new Vector3(Random.Range(xMin, xMax), Random.Range(yMin, yMax) + 0.6f, 0f);
            if (Mathf.Abs(pos.x - harper.transform.position.x) > 4f) break;
        }

        var elegido = comodines[Random.Range(0, comodines.Length)];
        if (elegido != null) Instantiate(elegido, pos, Quaternion.identity);
    }

    // Muestra en la Scene el rectángulo donde pueden aparecer (en verde)
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Vector3 centro = new Vector3((xMin + xMax) / 2f, (yMin + yMax) / 2f + 0.6f, 0f);
        Gizmos.DrawWireCube(centro, new Vector3(xMax - xMin, yMax - yMin, 0f));
    }
}
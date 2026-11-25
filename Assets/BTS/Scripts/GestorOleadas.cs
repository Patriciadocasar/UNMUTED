using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

// Oleadas de "Robots sin alma" para el Estudio BTS.
// Usa el robot que ya está en la escena como molde (no hace falta un prefab):
// ese robot cuenta como el primero de la oleada 1 y los demás son copias suyas.
// Al vencer la última oleada dispara el evento "Al Ganar" y, si hay, carga la siguiente escena.
public class GestorOleadas : MonoBehaviour
{
    [Header("Robots")]
    public Enemigo robotBase;
    public int[] robotsPorOleada = { 1, 2, 3 };
    public float pausaEntreOleadas = 2.5f;
    public float pausaEntreRobots = 0.6f;
    [Tooltip("Por dónde entran los robots (x). Se alternan.")]
    public float[] xAparicion = { 27.5f, 2.5f };

    [Header("Conexiones")]
    public BarraRobotUI barraRobot;          // la barra se pasa al siguiente robot vivo

    [Header("Al ganar")]
    public UnityEvent alGanar;                // ej.: mostrar texto, sonido de pieza liberada
    [Tooltip("Nombre de la escena del nivel 3 (vacío = quedarse aquí).")]
    public string siguienteEscena = "";
    public float esperaSiguiente = 5f;

    public int OleadaActual { get; private set; }

    GameObject molde;
    readonly List<Enemigo> vivos = new List<Enemigo>();
    int lado;

    void Awake()
    {
        if (robotBase == null) robotBase = FindFirstObjectByType<Enemigo>();
        if (robotBase == null) { Debug.LogWarning("GestorOleadas: no hay ningún robot (Enemigo) en la escena"); enabled = false; return; }

        // Copia inactiva del robot para clonar después (sin que corra su Awake/Start)
        bool activo = robotBase.gameObject.activeSelf;
        robotBase.gameObject.SetActive(false);
        molde = Instantiate(robotBase.gameObject, transform);
        molde.name = "Molde robot (no borrar)";
        robotBase.gameObject.SetActive(activo);
    }

    IEnumerator Start()
    {
        for (int o = 0; o < robotsPorOleada.Length; o++)
        {
            OleadaActual = o + 1;
            int n = robotsPorOleada[o];
            Debug.Log("Oleada " + OleadaActual + ": " + n + " robot(s)");

            if (o == 0 && robotBase != null) { vivos.Add(robotBase); n--; }
            for (int i = 0; i < n; i++)
            {
                Aparecer();
                yield return new WaitForSeconds(pausaEntreRobots);
            }

            yield return new WaitUntil(() => { vivos.RemoveAll(e => e == null); return vivos.Count == 0; });
            if (o < robotsPorOleada.Length - 1) yield return new WaitForSeconds(pausaEntreOleadas);
        }

        Debug.Log("¡Estudio BTS liberado!");
        alGanar?.Invoke();

        if (!string.IsNullOrEmpty(siguienteEscena))
        {
            yield return new WaitForSeconds(esperaSiguiente);
            SceneManager.LoadScene(siguienteEscena);
        }
    }

    void Aparecer()
    {
        float x = xAparicion.Length > 0 ? xAparicion[lado++ % xAparicion.Length] : 27f;
        var e0 = molde.GetComponent<Enemigo>();
        float y = Random.Range(e0.yMin, e0.yMax) + e0.ajusteAltura;

        var go = Instantiate(molde, new Vector3(x, y, 0f), Quaternion.identity);
        go.name = "Robot sin alma";
        go.SetActive(true);
        vivos.Add(go.GetComponent<Enemigo>());
    }

    void Update()
    {
        // Si el robot que muestra la barra murió, la barra pasa al siguiente vivo
        if (barraRobot == null || barraRobot.enemigo != null) return;
        foreach (var e in vivos)
            if (e != null) { barraRobot.enemigo = e; break; }
    }
}

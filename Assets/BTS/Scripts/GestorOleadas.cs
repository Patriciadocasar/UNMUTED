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
    [Tooltip("Cuántos robots NORMALES llegan en cada oleada.")]
    public int[] robotsPorOleada = { 1, 2, 0 };

    [Header("Robot especial (el Silenciador)")]
    [Tooltip("Arrastra aquí el PREFAB del robot nuevo (desde la carpeta Prefab).")]
    public GameObject prefabEspecial;
    [Tooltip("Cuántos robots ESPECIALES llegan en cada oleada (misma posición que la lista de arriba).")]
    public int[] especialesPorOleada = { 0, 0, 1 };

    [Header("Tiempos")]
    public float pausaEntreOleadas = 2.5f;
    public float pausaEntreRobots = 0.6f;
    [Tooltip("Por dónde entran los robots (x). Se alternan.")]
    public float[] xAparicion = { 27.5f, 2.5f };
    [Tooltip("Qué tan afuera de la pantalla aparecen antes de entrar deslizándose.")]
    public float distanciaAfuera = 6f;

    [Header("Al ganar")]
    public UnityEvent alGanar;                // ej.: mostrar texto, sonido de pieza liberada
    [Tooltip("Nombre de la escena del nivel 3 (vacío = quedarse aquí).")]
    public string siguienteEscena = "";
    public float esperaSiguiente = 5f;

    public int OleadaActual { get; private set; }
    public bool Terminado { get; private set; }   // ControlNivel espera a que sea true para dar la victoria

    GameObject molde;
    readonly List<Enemigo> vivos = new List<Enemigo>();
    int lado;

    void Awake()
    {
        if (robotBase == null) robotBase = FindAnyObjectByType<Enemigo>();
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
            int m = (prefabEspecial != null && o < especialesPorOleada.Length) ? especialesPorOleada[o] : 0;
            Debug.Log("Oleada " + OleadaActual + ": " + n + " normal(es) y " + m + " especial(es)");

            if (o == 0 && robotBase != null) { vivos.Add(robotBase); n--; }
            for (int i = 0; i < n; i++)
            {
                Aparecer();
                yield return new WaitForSeconds(pausaEntreRobots);
            }
            for (int i = 0; i < m; i++)
            {
                AparecerEspecial();
                yield return new WaitForSeconds(pausaEntreRobots);
            }

            yield return new WaitUntil(() => { vivos.RemoveAll(e => e == null); return vivos.Count == 0; });
            if (o < robotsPorOleada.Length - 1) yield return new WaitForSeconds(pausaEntreOleadas);
        }

        Debug.Log("¡Estudio BTS liberado!");
        Terminado = true;
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

        var go = Instantiate(molde, new Vector3(PuntoAfuera(x), y, 0f), Quaternion.identity);
        go.name = "Robot sin alma";
        go.SetActive(true);
        var e = go.GetComponent<Enemigo>();
        e.EntrarHasta(x);
        vivos.Add(e);
    }

    // Si el lugar está a la derecha, sale más a la derecha (fuera de cámara); si está a la izquierda, más a la izquierda
    float PuntoAfuera(float x)
    {
        var cam = Camera.main;
        float centro = cam != null ? cam.transform.position.x : 15f;
        return x >= centro ? x + distanciaAfuera : x - distanciaAfuera;
    }

    void AparecerEspecial()
    {
        float x = xAparicion.Length > 0 ? xAparicion[lado++ % xAparicion.Length] : 27f;
        var e0 = molde.GetComponent<Enemigo>();
        var go = Instantiate(prefabEspecial, new Vector3(x, 0f, 0f), Quaternion.identity);
        go.name = "Robot Silenciador";
        var e = go.GetComponent<Enemigo>();
        if (e != null)
        {
            // Hereda los límites del piso que calculó ZonaCaminable
            e.xMin = e0.xMin; e.xMax = e0.xMax; e.yMin = e0.yMin; e.yMax = e0.yMax;
            go.transform.position = new Vector3(PuntoAfuera(x), Random.Range(e.yMin, e.yMax) + e.ajusteAltura, 0f);
            e.EntrarHasta(x);
            vivos.Add(e);
        }
        else Debug.LogWarning("GestorOleadas: el Prefab Especial no tiene el script Enemigo");
    }

    // (La barra de vida de robots ya suma sola a todos los robots vivos, no hace falta pasársela)
}

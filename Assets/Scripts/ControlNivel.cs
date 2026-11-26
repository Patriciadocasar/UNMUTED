using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Pon este script en un objeto vacío llamado "ControlNivel" en cada nivel.
// Cuando todos los robots mueren, muestra "¡Nivel completado!" y carga el siguiente nivel
// (el que está después en File > Build Profiles > Scene List).
public class ControlNivel : MonoBehaviour
{
    [Header("Mensaje")]
    public string mensaje = "¡NIVEL COMPLETADO!";
    public string mensajeFinal = "¡GANASTE!";   // si es el último nivel de la lista
    public Color colorTexto = new Color(1f, 0.85f, 0.2f);
    public int tamanoTexto = 90;

    [Header("Tiempos")]
    public float esperaAntesDeCambiar = 3f;     // segundos que se ve el mensaje

    [Header("Sonido")]
    public AudioClip sonidoVictoria;

    bool habiaRobots, terminado;

    void Start()
    {
        habiaRobots = FindObjectsByType<Enemigo>().Length > 0;
        if (!habiaRobots) Debug.LogWarning("ControlNivel: este nivel no tiene robots");
        InvokeRepeating(nameof(Revisar), 1f, 0.5f);
    }

    void Revisar()
    {
        if (terminado || !habiaRobots) return;

        // Si el nivel tiene oleadas, esperamos a que termine la última (entre oleadas no hay robots)
        var oleadas = FindAnyObjectByType<GestorOleadas>();
        if (oleadas != null && !oleadas.Terminado) return;
        if (FindObjectsByType<Enemigo>().Length > 0) return;   // todavía quedan robots

        // ¿Harper sigue viva?
        var harper = FindAnyObjectByType<VidaHarper>();
        if (harper != null && harper.Actual <= 0) return;

        terminado = true;
        StartCoroutine(Ganar());
    }

    IEnumerator Ganar()
    {
        int siguiente = SceneManager.GetActiveScene().buildIndex + 1;
        bool hayOtroNivel = siguiente < SceneManager.sceneCountInBuildSettings;

        Sonidos.Tocar(sonidoVictoria);
        MostrarTexto(hayOtroNivel ? mensaje : mensajeFinal);

        // Harper queda quieta celebrando
        var ataque = FindAnyObjectByType<HarperAtaque>(); if (ataque) ataque.enabled = false;

        yield return new WaitForSeconds(esperaAntesDeCambiar);

        if (hayOtroNivel) SceneManager.LoadScene(siguiente);
        else Debug.Log("¡Era el último nivel de la lista!");
    }

    // Crea el letrero en pantalla sin que tengas que armarlo a mano
    void MostrarTexto(string texto)
    {
        var lienzo = new GameObject("CartelNivel");
        var canvas = lienzo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;   // encima de todo
        var escala = lienzo.AddComponent<CanvasScaler>();
        escala.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        escala.referenceResolution = new Vector2(1920, 1080);

        // Fondo oscuro semitransparente
        var fondo = new GameObject("Fondo").AddComponent<Image>();
        fondo.transform.SetParent(lienzo.transform, false);
        fondo.color = new Color(0f, 0f, 0f, 0.55f);
        fondo.rectTransform.anchorMin = Vector2.zero;
        fondo.rectTransform.anchorMax = Vector2.one;
        fondo.rectTransform.offsetMin = fondo.rectTransform.offsetMax = Vector2.zero;

        // Texto
        var t = new GameObject("Texto").AddComponent<Text>();
        t.transform.SetParent(lienzo.transform, false);
        t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        t.text = texto;
        t.fontSize = tamanoTexto;
        t.fontStyle = FontStyle.Bold;
        t.color = colorTexto;
        t.alignment = TextAnchor.MiddleCenter;
        t.rectTransform.anchorMin = Vector2.zero;
        t.rectTransform.anchorMax = Vector2.one;
        t.rectTransform.offsetMin = t.rectTransform.offsetMax = Vector2.zero;
        var sombra = t.gameObject.AddComponent<Outline>();
        sombra.effectColor = new Color(0.3f, 0f, 0.3f);
        sombra.effectDistance = new Vector2(4, -4);
    }
}
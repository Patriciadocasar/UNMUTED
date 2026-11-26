using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Fundido a negro entre escenas. No hace falta ponerlo en ningún objeto: se crea solo.
public class Transicion : MonoBehaviour
{
    static Transicion instancia;
    CanvasGroup grupo;
    bool ocupada;

    public static bool HayOverlay => instancia != null && instancia.grupo.alpha > 0.01f;

    public static void IrA(int indiceEscena, float salida = 0.8f, float entrada = 0.8f)
        => Iniciar(() => SceneManager.LoadSceneAsync(indiceEscena), salida, entrada);

    public static void IrA(string nombreEscena, float salida = 0.8f, float entrada = 0.8f)
        => Iniciar(() => SceneManager.LoadSceneAsync(nombreEscena), salida, entrada);

    public static void Reiniciar(float salida = 0.6f, float entrada = 0.6f)
        => IrA(SceneManager.GetActiveScene().buildIndex, salida, entrada);

    static void Iniciar(Func<AsyncOperation> cargar, float salida, float entrada)
    {
        if (instancia == null)
        {
            var go = new GameObject("Transicion");
            DontDestroyOnLoad(go);
            instancia = go.AddComponent<Transicion>();
            instancia.Armar();
        }
        if (instancia.ocupada) return;
        instancia.StartCoroutine(instancia.Rutina(cargar, Mathf.Max(salida, 0.01f), Mathf.Max(entrada, 0.01f)));
    }

    void Armar()
    {
        var canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 1000;   // encima de todo
        grupo = gameObject.AddComponent<CanvasGroup>();
        grupo.alpha = 0f;
        grupo.blocksRaycasts = false;
        grupo.interactable = false;

        var negro = new GameObject("Negro").AddComponent<Image>();
        negro.transform.SetParent(transform, false);
        negro.color = Color.black;
        negro.raycastTarget = false;
        var rt = negro.rectTransform;
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
    }

    IEnumerator Rutina(Func<AsyncOperation> cargar, float salida, float entrada)
    {
        ocupada = true;
        if (Sonidos.instancia != null) Sonidos.instancia.ApagarMusica(salida);

        for (float t = 0; t < salida; t += Time.unscaledDeltaTime)
        { grupo.alpha = t / salida; yield return null; }
        grupo.alpha = 1f;

        var op = cargar();
        while (!op.isDone) yield return null;
        yield return null; yield return null;   // deja que el nivel nuevo arme su título

        for (float t = 0; t < entrada; t += Time.unscaledDeltaTime)
        { grupo.alpha = 1f - t / entrada; yield return null; }
        grupo.alpha = 0f;
        ocupada = false;
    }
}
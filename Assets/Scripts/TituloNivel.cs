using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

public class TituloNivel : MonoBehaviour
{
    public Sprite imagen;
    public float duracion = 2.5f;     // segundos que se ve
    public float fundido = 0.4f;      // segundos de entrada y salida
    public bool pausarJuego = true;   // congela el nivel mientras se ve

    void Start()
    {
        if (imagen == null)
        {
            Debug.LogWarning("TituloNivel: falta la imagen");
            if (Sonidos.instancia != null) Sonidos.instancia.IniciarFundido();
            return;
        }
        StartCoroutine(Mostrar());
    }

    IEnumerator Mostrar()
    {
        var lienzo = new GameObject("TituloNivelCanvas");
        var canvas = lienzo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 200;
        var escala = lienzo.AddComponent<CanvasScaler>();
        escala.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        escala.referenceResolution = new Vector2(1920, 1080);
        var grupo = lienzo.AddComponent<CanvasGroup>();

        var fondo = new GameObject("Fondo").AddComponent<Image>();
        fondo.transform.SetParent(lienzo.transform, false);
        fondo.color = Color.black;
        Estirar(fondo.rectTransform);

        var img = new GameObject("Imagen").AddComponent<Image>();
        img.transform.SetParent(lienzo.transform, false);
        img.sprite = imagen;
        img.preserveAspect = true;
        Estirar(img.rectTransform);

        float escalaPrevia = Time.timeScale;
        if (pausarJuego) Time.timeScale = 0f;

                if (Transicion.HayOverlay)
        {
            grupo.alpha = 1;   // ya viene del negro: el título está listo
        }
        else
        {
            for (float t = 0; t < fundido; t += Time.unscaledDeltaTime)
            { grupo.alpha = t / fundido; yield return null; }
            grupo.alpha = 1;
        }

        float espera = 0;
        while (espera < duracion && !Saltar())
        { espera += Time.unscaledDeltaTime; yield return null; }

        for (float t = 0; t < fundido; t += Time.unscaledDeltaTime)
        { grupo.alpha = 1 - t / fundido; yield return null; }

        if (pausarJuego) Time.timeScale = escalaPrevia > 0 ? escalaPrevia : 1f;
        if (Sonidos.instancia != null) Sonidos.instancia.IniciarFundido();
        Destroy(lienzo);
    }

    bool Saltar()
    {
        return (Keyboard.current != null && Keyboard.current.anyKey.wasPressedThisFrame)
            || (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame);
    }

    static void Estirar(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
    }
}
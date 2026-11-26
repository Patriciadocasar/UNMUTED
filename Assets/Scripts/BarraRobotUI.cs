using UnityEngine;
using UnityEngine.UI;

// Va en "Barra vida robot" (o en su Slider).
// Muestra la vida de TODOS los robots del nivel sumada en una sola barra.
public class BarraRobotUI : MonoBehaviour
{
    Slider barra;
    float vidaTotalInicial;

    void Start()
    {
        barra = GetComponentInChildren<Slider>();
        if (barra == null) { Debug.LogWarning("BarraRobotUI: no encuentro el Slider"); enabled = false; return; }

        vidaTotalInicial = Mathf.Max(1, VidaTotal());
        barra.interactable = false;
        barra.wholeNumbers = false;
        barra.minValue = 0;
        barra.maxValue = vidaTotalInicial;
        barra.value = vidaTotalInicial;   // empieza llena
    }

    float anterior;

    void Update()
    {
        float v = VidaTotal();
        // Si llegan robots nuevos (oleadas), la barra se vuelve a llenar con la vida de los nuevos
        if (anterior <= 0f && v > 0f) barra.maxValue = v;
        if (v > barra.maxValue) barra.maxValue = v;
        barra.value = v;
        anterior = v;
    }

    // Suma la vida que le queda a cada robot vivo
    float VidaTotal()
    {
        float total = 0;
        foreach (var e in FindObjectsByType<Enemigo>())
            total += Mathf.Max(0, e.vida);
        return total;
    }
}
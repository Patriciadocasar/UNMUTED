using UnityEngine;
using UnityEngine.UI;

// Va en "Barra vida" (o en su Slider). Muestra la vida de Harper.
public class BarraVida : MonoBehaviour
{
    public VidaHarper vida;
    Slider barra;

    void Start()
    {
        barra = GetComponentInChildren<Slider>();
        if (barra == null) { Debug.LogWarning("BarraVida: no encuentro el Slider"); enabled = false; return; }
        if (vida == null) vida = FindFirstObjectByType<VidaHarper>();
        if (vida == null) { Debug.LogWarning("BarraVida: no encuentro a Harper (VidaHarper)"); enabled = false; return; }
        barra.interactable = false;   // para que no se pueda mover con el mouse
        barra.wholeNumbers = false;
        barra.minValue = 0;
        barra.maxValue = vida.vidaMax;
        barra.value = vida.vidaMax;   // empieza llena
    }

    void Update()
    {
        barra.maxValue = vida.vidaMax;
        barra.value = vida.Actual;
    }
}
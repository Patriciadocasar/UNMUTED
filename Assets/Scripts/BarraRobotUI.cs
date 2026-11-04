using UnityEngine;
using UnityEngine.UI;

// Va en "Barra robot" (o en su Slider). Muestra la vida del robot en la esquina.
public class BarraRobotUI : MonoBehaviour
{
    public Enemigo enemigo;
    Slider barra;

    void Start()
    {
        barra = GetComponentInChildren<Slider>();
        if (enemigo == null) enemigo = FindFirstObjectByType<Enemigo>();
        if (barra == null || enemigo == null) { Debug.LogWarning("BarraRobotUI: falta el Slider o el robot"); enabled = false; return; }
        barra.interactable = false;
        barra.wholeNumbers = false;
        barra.minValue = 0;
        barra.maxValue = Mathf.Max(1, enemigo.vida);
        barra.value = barra.maxValue;   // empieza llena
    }

    void Update()
    {
        if (enemigo == null) { barra.value = 0; return; }   // el robot murió
        barra.value = enemigo.vida;
    }
}
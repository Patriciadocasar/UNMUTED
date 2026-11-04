using UnityEngine;
using UnityEngine.UI;

public class BarraEnergia : MonoBehaviour
{
    public EnergiaSonica energia;
    Slider barra;

    void Awake() => barra = GetComponent<Slider>();

    void Update()
    {
        if (energia != null) barra.value = energia.Actual;
    }
}
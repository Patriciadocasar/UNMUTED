using UnityEngine;
using UnityEngine.Rendering.Universal;

// Luz 2D (URP) que palpita con el pulso de la música.
// "Nivel" va de 0 (apagada) a 1 (encendida).
[RequireComponent(typeof(Light2D))]
public class LuzRitmo : MonoBehaviour
{
    public float intensidadBase = 1f;
    [Tooltip("Cuánto sube la intensidad en cada golpe (0.4 = +40%).")]
    public float pulso = 0.35f;

    [Range(0f, 1f)] public float nivel = 1f;

    Light2D luz;

    void Awake() => luz = GetComponent<Light2D>();

    void Update()
    {
        float golpe = PulsoRitmo.Actual != null ? PulsoRitmo.Actual.Golpe : 0f;
        luz.intensity = intensidadBase * nivel * (1f + pulso * golpe);
    }
}

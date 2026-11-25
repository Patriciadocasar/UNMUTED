using UnityEngine;
using UnityEngine.Rendering.Universal;

[RequireComponent(typeof(Light2D))]
public class MovimientoLucesDisco : MonoBehaviour
{
    [Header("Movimiento")]
    [SerializeField] private float anguloBase = 180f; // 180 = apunta hacia abajo
    [SerializeField] private float amplitud = 35f;    // grados a cada lado
    [SerializeField] private float velocidad = 0.5f;
    [SerializeField] private float fase = 0f;         // desfase entre focos

    [Header("Color")]
    [SerializeField] private Color colorA = new Color(1f, 0.24f, 0.86f);
    [SerializeField] private Color colorB = new Color(0.48f, 0.24f, 1f);
    [SerializeField] private float velocidadColor = 0.3f;

    private Light2D luz;
    private float tiempo;

    // El poder Bola de Disco lo sube para que los focos se muevan más rápido
    public static float multiplicadorFiesta = 1f;

    void Awake()
    {
        luz = GetComponent<Light2D>();
    }

    void Update()
    {
        tiempo += Time.deltaTime * multiplicadorFiesta;
        float t = tiempo;

        float angulo = anguloBase + Mathf.Sin(t * velocidad + fase) * amplitud;
        transform.localRotation = Quaternion.Euler(0f, 0f, angulo);

        float mezcla = (Mathf.Sin(t * velocidadColor + fase) + 1f) * 0.5f;
        luz.color = Color.Lerp(colorA, colorB, mezcla);
    }
}
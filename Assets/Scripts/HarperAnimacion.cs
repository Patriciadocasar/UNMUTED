using UnityEngine;

// Anima a Harper cambiando el sprite del SpriteRenderer:
//  - "Idle" (respirar) cuando está quieta
//  - "Caminar" cuando se mueve (en X o en Y, como un beat 'em up)
// Respeta la pose de ataque de HarperAtaque y se detiene cuando Harper muere.
// Va en el mismo GameObject que HarperMovimiento.
[DefaultExecutionOrder(100)]                 // corre después de HarperMovimiento y HarperAtaque
[RequireComponent(typeof(HarperMovimiento))]
public class HarperAnimacion : MonoBehaviour
{
    [Header("Cuadros (arrástralos en orden desde Harper_Animaciones.aseprite)")]
    public Sprite[] idle;                    // 4 cuadros
    public Sprite[] caminar;                 // 8 cuadros

    [Header("Velocidad de la animación")]
    public float fpsIdle = 4f;               // 250 ms por cuadro, igual que en Aseprite
    public float fpsCaminar = 12f;           // ~85 ms por cuadro
    [Tooltip("Si está activo, los pasos van más lentos cuando Harper acelera o frena")]
    public bool ajustarAVelocidad = true;
    [Tooltip("Por debajo de esta velocidad (unidades/seg) se considera quieta")]
    public float umbralMovimiento = 0.5f;

    HarperMovimiento mov;
    HarperAtaque ataque;
    SpriteRenderer sr;
    float t;
    bool caminando;

    public bool Caminando => caminando;

    void Awake()
    {
        mov = GetComponent<HarperMovimiento>();
        ataque = GetComponent<HarperAtaque>();
        sr = GetComponentInChildren<SpriteRenderer>();
        if (sr == null) Debug.LogWarning("HarperAnimacion: Harper no tiene SpriteRenderer");
    }

    void LateUpdate()
    {
        if (sr == null || mov == null) return;
        if (!mov.enabled) return;                          // VidaHarper la desactiva al morir
        if (ataque != null && ataque.EnPose) return;      // no pisar la pose de ataque

        float v = mov.VelocidadReal.magnitude;
        bool camina = v > umbralMovimiento;
        if (camina != caminando) { caminando = camina; t = 0f; }   // reinicia el ciclo al cambiar

        Sprite[] cuadros = (caminando && caminar != null && caminar.Length > 0) ? caminar : idle;
        if (cuadros == null || cuadros.Length == 0) return;

        float fps = caminando ? fpsCaminar : fpsIdle;
        if (caminando && ajustarAVelocidad && mov.velocidadMax > 0f)
            fps *= Mathf.Clamp(v / mov.velocidadMax, 0.5f, 1f);

        t += Time.deltaTime * fps;
        int i = (int)t % cuadros.Length;
        if (cuadros[i] != null) sr.sprite = cuadros[i];
    }
}

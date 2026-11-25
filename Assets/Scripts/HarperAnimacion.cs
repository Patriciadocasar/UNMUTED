using UnityEngine;

// Anima a Harper cambiando el sprite del SpriteRenderer. Prioridades:
//  1. Muerta          -> sprite Herida (y VidaHarper la pinta de gris)
//  2. Herida          -> sprite Herida durante VidaHarper.duracionHerida (1 s) y luego vuelve
//  3. Pose de ataque  -> no se toca (la pone HarperAtaque con J/K)
//  4. Agachada        -> Agacharse (transición) y Agachada (bucle)
//  5. Caminar / Idle  -> según la velocidad real
// Va en el mismo GameObject que HarperMovimiento.
[DefaultExecutionOrder(100)]                 // corre después de los demás scripts de Harper
[RequireComponent(typeof(HarperMovimiento))]
public class HarperAnimacion : MonoBehaviour
{
    [Header("Cuadros (desde Harper_Animaciones.aseprite)")]
    public Sprite[] idle;          // Frame_0 a Frame_3
    public Sprite[] caminar;       // Frame_4 a Frame_11
    public Sprite agacharse;       // Frame_12 (transición)
    public Sprite[] agachada;      // Frame_13 y Frame_14
    public Sprite herida;          // Frame_15 (o tu HarperHerida.png)

    [Header("Velocidad de la animación")]
    public float fpsIdle = 4f;
    public float fpsCaminar = 12f;
    public float fpsAgachada = 8f;      // temblor de miedo (120 ms por cuadro)
    [Tooltip("Los pasos van más lentos cuando Harper acelera o frena")]
    public bool ajustarAVelocidad = true;
    [Tooltip("Por debajo de esta velocidad (unidades/seg) se considera quieta")]
    public float umbralMovimiento = 0.5f;

    HarperMovimiento mov;
    HarperAtaque ataque;
    HarperAgachar agachar;
    VidaHarper vida;
    SpriteRenderer sr;
    float t;
    int estado = -1;               // para reiniciar el ciclo al cambiar de estado

    void Awake()
    {
        mov = GetComponent<HarperMovimiento>();
        ataque = GetComponent<HarperAtaque>();
        agachar = GetComponent<HarperAgachar>();
        vida = GetComponent<VidaHarper>();
        sr = GetComponentInChildren<SpriteRenderer>();
        if (sr == null) Debug.LogWarning("HarperAnimacion: Harper no tiene SpriteRenderer");
    }

    void LateUpdate()
    {
        if (sr == null || mov == null) return;

        if (vida != null && vida.Muerta) { Poner(herida); return; }
        if (!mov.enabled) return;
        if (vida != null && vida.Herida) { Cambiar(1); Poner(herida); return; }
        if (ataque != null && ataque.EnPose) { Cambiar(2); return; }

        if (agachar != null && agachar.Agachada)
        {
            if (agachar.Progreso < 1f) { Cambiar(3); Poner(agacharse); }
            else { Cambiar(4); Ciclo(agachada, fpsAgachada); }
            return;
        }

        float v = mov.VelocidadReal.magnitude;
        if (v > umbralMovimiento && caminar != null && caminar.Length > 0)
        {
            Cambiar(5);
            float fps = fpsCaminar;
            if (ajustarAVelocidad && mov.velocidadMax > 0f)
                fps *= Mathf.Clamp(v / mov.velocidadMax, 0.5f, 1f);
            Ciclo(caminar, fps);
        }
        else { Cambiar(6); Ciclo(idle, fpsIdle); }
    }

    void Cambiar(int nuevo) { if (nuevo != estado) { estado = nuevo; t = 0f; } }

    void Poner(Sprite s) { if (s != null) sr.sprite = s; }

    void Ciclo(Sprite[] cuadros, float fps)
    {
        if (cuadros == null || cuadros.Length == 0) return;
        t += Time.deltaTime * fps;
        Poner(cuadros[(int)t % cuadros.Length]);
    }
}

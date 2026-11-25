using UnityEngine;

// Prop del escenario que anima sus frames a ritmo de PulsoRitmo
// (bombillas que corren, letreros que parpadean, bocinas que laten).
[RequireComponent(typeof(SpriteRenderer))]
public class PropMusical : MonoBehaviour
{
    public Sprite[] frames;

    [Tooltip("Cuántos frames avanza por pulso. 0 = no anima (se queda en el frame 1).")]
    public float framesPorPulso = 1f;

    [Tooltip("Cuánto crece en cada golpe (0.04 = 4%). 0 = no late.")]
    public float latido = 0f;

    [Tooltip("Desactívalo para congelar la animación.")]
    public bool encendido = true;

    SpriteRenderer sr;
    Vector3 escalaBase;

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        escalaBase = transform.localScale;
    }

    void Update()
    {
        if (!encendido || frames == null || frames.Length == 0) return;
        var pulso = PulsoRitmo.Actual;
        if (pulso == null) return;

        if (framesPorPulso > 0f && frames.Length > 1)
            sr.sprite = frames[(int)(pulso.Pulsos * framesPorPulso) % frames.Length];

        if (latido > 0f)
            transform.localScale = escalaBase * (1f + latido * pulso.Golpe);
    }
}

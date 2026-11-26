using UnityEngine;

// Reloj musical del nivel. Todo lo que "baila" (bombillas, luces, bocinas) lee de aquí.
// "Boy With Luv" va a 120 BPM. Si asignas la canción en "Musica", el pulso se sincroniza
// con el tiempo real del audio; si no, usa el reloj del juego.
public class PulsoRitmo : MonoBehaviour
{
    public static PulsoRitmo Actual { get; private set; }

    [Header("Tempo")]
    public float bpm = 120f;
    public float desfaseSegundos = 0f;      // ajusta si la canción tiene silencio al inicio

    [Header("Audio (opcional)")]
    public AudioSource musica;

    float inicio;

    // Pulsos (beats) transcurridos. Ej: 3.5 = a la mitad del cuarto pulso.
    public float Pulsos
    {
        get
        {
            float t = (musica != null && musica.isPlaying) ? musica.time : Time.time - inicio;
            return Mathf.Max(0f, t - desfaseSegundos) * bpm / 60f;
        }
    }

    // 1 justo en el golpe y baja hasta 0 antes del siguiente. Útil para "latidos".
    public float Golpe
    {
        get
        {
            float f = Pulsos % 1f;
            return (1f - f) * (1f - f);
        }
    }

    void Awake()
    {
        Actual = this;
        inicio = Time.time;
    }

    void OnDestroy()
    {
        if (Actual == this) Actual = null;
    }
}

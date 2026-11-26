using System.Collections;
using UnityEngine;

// Pon este script en un objeto vacío llamado "Sonidos" en la escena.
// Toca la canción de fondo y los efectos que le piden los demás scripts.
public class Sonidos : MonoBehaviour
{
    public static Sonidos instancia;

    [Header("Música de fondo")]
    public AudioClip cancion;
    [Range(0f, 1f)] public float volumenMusica = 0.4f;

    [Tooltip("Segundos que tarda la música en llegar al volumen final. 0 = sin fundido")]
    public float duracionFundido = 12f;
    [Range(0.005f, 1f)] public float volumenInicial = 0.02f;
    float tiempoMusica;
    bool fundidoIniciado;
    float salida = 1f;   // 1 = normal, baja a 0 al cambiar de nivel

    [Header("Efectos")]
    [Range(0f, 1f)] public float volumenEfectos = 0.8f;

    AudioSource musica, efectos;

    void Awake()
    {
        instancia = this;

        musica = gameObject.AddComponent<AudioSource>();
        musica.loop = true;
        musica.playOnAwake = false;

        efectos = gameObject.AddComponent<AudioSource>();
        efectos.playOnAwake = false;
    }

    void Start()
    {
        if (cancion != null)
        {
            musica.clip = cancion;
            musica.volume = volumenInicial;
            musica.Play();
        }
        // si el nivel tiene imagen de título, el fundido espera a que termine
        fundidoIniciado = FindAnyObjectByType<TituloNivel>() == null;
    }

    public void IniciarFundido() { fundidoIniciado = true; }

    public void ApagarMusica(float segundos) { StartCoroutine(Apagar(segundos)); }

    IEnumerator Apagar(float segundos)
    {
        for (float t = 0; t < segundos; t += Time.unscaledDeltaTime)
        { salida = 1f - t / segundos; yield return null; }
        salida = 0f;
    }

    void Update()
    {
        float v = volumenInicial;
        if (fundidoIniciado)
        {
            tiempoMusica += Time.unscaledDeltaTime;
            float t = duracionFundido <= 0f ? 1f : Mathf.Clamp01(tiempoMusica / duracionFundido);
            float final = Mathf.Max(volumenMusica, volumenInicial);
            v = volumenInicial * Mathf.Pow(final / volumenInicial, t);   // subida pareja al oído
        }
        musica.volume = v * salida;
    }

    // Los otros scripts llaman a esto: Sonidos.Tocar(miSonido);
    public static void Tocar(AudioClip clip, float volumen = 1f)
    {
        if (clip == null || instancia == null) return;
        instancia.efectos.PlayOneShot(clip, volumen * instancia.volumenEfectos);
    }
}
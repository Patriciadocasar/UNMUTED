using UnityEngine;

// Pon este script en un objeto vacío llamado "Sonidos" en la escena.
// Toca la canción de fondo y los efectos que le piden los demás scripts.
public class Sonidos : MonoBehaviour
{
    public static Sonidos instancia;

    [Header("Música de fondo")]
    public AudioClip cancion;
    [Range(0f, 1f)] public float volumenMusica = 0.4f;

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
            musica.Play();
        }
    }

    void Update()
    {
        musica.volume = volumenMusica;   // así puedes moverle el volumen mientras juegas
    }

    // Los otros scripts llaman a esto: Sonidos.Tocar(miSonido);
    public static void Tocar(AudioClip clip, float volumen = 1f)
    {
        if (clip == null || instancia == null) return;
        instancia.efectos.PlayOneShot(clip, volumen * instancia.volumenEfectos);
    }
}
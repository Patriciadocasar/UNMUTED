using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class VidaHarper : MonoBehaviour
{
    public int vidaMax = 10;
    public float invulnerable = 0.8f;
    public float retrasoReinicio = 1.5f;
    public int Actual { get; private set; }

    [Header("Efecto cuando la golpean")]
    public GameObject prefabGolpeRecibido;   // arrastra aquí el prefab del destello
    public float alturaEfecto = 1.75f;       // qué tan arriba de los pies aparece

    [Header("Sonidos")]
    public AudioClip sonidoGolpe;            // cuando la golpean
    public AudioClip sonidoMuerte;           // cuando pierde

    [Header("Reacción al golpe (sprite HarperHerida)")]
    public float duracionHerida = 0.3f;        // NUEVO: segundos con la cara de dolor
    public bool quietaMientrasHerida = true; // NUEVO: no camina ni ataca durante ese segundo

    SpriteRenderer sr;
    float hasta;
    bool muerta;
    float finHerida;                         // NUEVO

    // NUEVO: los usan HarperAnimacion, HarperMovimiento, HarperAtaque y HarperAgachar
    public bool Muerta => muerta;
    public bool Herida => !muerta && Time.time < finHerida;
    public bool Aturdida => quietaMientrasHerida && Herida;
    public event System.Action AlSerGolpeada;

    void Awake()
    {
        Actual = vidaMax;
        sr = GetComponentInChildren<SpriteRenderer>();
    }

    // Devuelve true si el golpe sí le quitó vida
    public bool RecibirDano(int d)
    {
        if (muerta || Time.time < hasta) return false;   // invulnerable: el golpe no cuenta
        Actual = Mathf.Max(0, Actual - d);
        hasta = Time.time + invulnerable;
        finHerida = Time.time + duracionHerida;   
        AlSerGolpeada?.Invoke();                  

        // Destello de golpe sobre Harper
        if (prefabGolpeRecibido != null)
            Instantiate(prefabGolpeRecibido, transform.position + Vector3.up * alturaEfecto, Quaternion.identity);

        if (Actual <= 0) { Sonidos.Tocar(sonidoMuerte); StartCoroutine(Morir()); }
        else { Sonidos.Tocar(sonidoGolpe); StartCoroutine(Parpadeo()); }
        return true;
    }

    // Poder del Fandom: recupera vida
    public void Curar(int cantidad) => Curar(cantidad, new Color(0.75f, 0.55f, 1f));   // destello morado

    // El corazón de los comodines usa un destello rosado
    public void Curar(int cantidad, Color colorBrillo)
    {
        if (muerta) return;
        Actual = Mathf.Min(vidaMax, Actual + cantidad);
        StartCoroutine(Brillo(colorBrillo, 0.8f));
    }

    // Escudo de Estrellas: no le pasa nada por unos segundos
    public void HacerInvencible(float segundos)
    {
        if (muerta) return;
        hasta = Mathf.Max(hasta, Time.time + segundos);
        StartCoroutine(Brillo(new Color(1f, 0.85f, 0.3f), segundos));  // brilla dorado
    }

    IEnumerator Brillo(Color color, float segundos)
    {
        float fin = Time.time + segundos;
        while (Time.time < fin)
        {
            if (sr != null) sr.color = Color.Lerp(Color.white, color, (Mathf.Sin(Time.time * 12f) + 1f) * 0.5f);
            yield return null;
        }
        if (sr != null && !muerta) sr.color = Color.white;
    }

    IEnumerator Parpadeo()
    {
        float fin = Time.time + invulnerable;
        while (Time.time < fin)
        {
            if (sr != null) sr.enabled = !sr.enabled;
            yield return new WaitForSeconds(0.08f);
        }
        if (sr != null) sr.enabled = true;
    }

    IEnumerator Morir()
    {
        muerta = true;
        var mov = GetComponent<HarperMovimiento>(); if (mov) mov.enabled = false;
        var ata = GetComponent<HarperAtaque>();     if (ata) ata.enabled = false;
        if (sr != null) { sr.enabled = true; sr.color = new Color(0.45f, 0.5f, 0.55f); }  // se vuelve gris, sin alma

        yield return new WaitForSeconds(retrasoReinicio);
        Transicion.Reiniciar();   // reinicia el nivel con fundido a negro
    }
}
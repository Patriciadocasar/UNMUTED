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

    SpriteRenderer sr;
    float hasta;
    bool muerta;

    void Awake()
    {
        Actual = vidaMax;
        sr = GetComponentInChildren<SpriteRenderer>();
    }

    public void RecibirDano(int d)
    {
        if (muerta || Time.time < hasta) return;
        Actual = Mathf.Max(0, Actual - d);
        hasta = Time.time + invulnerable;

        // Destello de golpe sobre Harper
        if (prefabGolpeRecibido != null)
            Instantiate(prefabGolpeRecibido, transform.position + Vector3.up * alturaEfecto, Quaternion.identity);

        if (Actual <= 0) StartCoroutine(Morir());
        else StartCoroutine(Parpadeo());
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
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);   // reinicia el nivel
    }
}
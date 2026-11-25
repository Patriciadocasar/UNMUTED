using System.Collections;
using UnityEngine;

// Aleteo suave para mariposas decorativas. La mariposa NO se desplaza:
// solo alterna entre alas abiertas / cerradas y, opcionalmente,
// "respira" 1 pixel arriba y abajo.
[RequireComponent(typeof(SpriteRenderer))]
public class MariposaAleteo : MonoBehaviour
{
    [Header("Sprites (FRAME 1 = abierta, FRAME 2 = cerrada)")]
    public Sprite alaAbierta;
    public Sprite alaCerrada;

    [Header("Aleteo")]
    public float tiempoPorFrame = 0.18f;          // mas alto = mas suave/lento
    public Vector2Int aleteosPorRafaga = new Vector2Int(2, 5);
    public Vector2 pausaEntreRafagas = new Vector2(0.8f, 2.5f);

    [Header("Flotacion (0 = totalmente quieta)")]
    public float amplitudPixeles = 1f;            // el sprite es pixel art a 8 PPU
    public float velocidadFlotacion = 1.5f;
    public float pixelesPorUnidad = 8f;

    SpriteRenderer sr;
    Vector3 posBase;
    float fase;

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        posBase = transform.localPosition;
        fase = Random.value * Mathf.PI * 2f;      // cada mariposa va desfasada
    }

    void OnEnable()
    {
        if (alaAbierta == null || alaCerrada == null)
        {
            Debug.LogWarning(name + ": asigna Ala Abierta y Ala Cerrada en el Inspector");
            enabled = false;
            return;
        }
        StartCoroutine(Aletear());
    }

    IEnumerator Aletear()
    {
        sr.sprite = alaAbierta;
        // Arranque aleatorio para que no aleteen todas a la vez
        yield return new WaitForSeconds(Random.Range(0f, pausaEntreRafagas.y));

        while (true)
        {
            int aleteos = Random.Range(aleteosPorRafaga.x, aleteosPorRafaga.y + 1);
            for (int i = 0; i < aleteos; i++)
            {
                sr.sprite = alaCerrada;
                yield return new WaitForSeconds(tiempoPorFrame * Random.Range(0.85f, 1.2f));
                sr.sprite = alaAbierta;
                yield return new WaitForSeconds(tiempoPorFrame * Random.Range(0.85f, 1.2f));
            }
            // descansa con las alas abiertas
            yield return new WaitForSeconds(Random.Range(pausaEntreRafagas.x, pausaEntreRafagas.y));
        }
    }

    void Update()
    {
        if (amplitudPixeles <= 0f) return;

        // Se redondea a pixeles enteros para que no se vea borroso
        float y = Mathf.Sin(Time.time * velocidadFlotacion + fase) * amplitudPixeles;
        y = Mathf.Round(y) / pixelesPorUnidad;
        transform.localPosition = posBase + new Vector3(0f, y, 0f);
    }
}
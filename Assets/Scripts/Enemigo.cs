using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class Enemigo : MonoBehaviour
{
    [Header("Vida")]
    public int vida = 20;

    [Header("Movimiento")]
    public float velocidad = 2f;
    public float distanciaMin = 7f;       // lo más cerca que se pone para disparar
    public float distanciaMax = 12f;      // lo más lejos desde donde dispara
    float distanciaDisparo;               // se elige al azar entre Min y Max en cada disparo
    public float margenCarril = 0.3f;     // qué tan alineado en altura se pone
    public float xMin = 2f, xMax = 28f, yMin = 0.3f, yMax = 5.5f;
    public bool miraIzquierdaPorDefecto = true;   // tu dibujo mira a la izquierda
    public float ajusteAltura = 1.2f;     // súbelo si el robot se ve muy abajo respecto a Harper

    // La "altura de carril" del robot, comparable con la de Harper
    public float AlturaCarril => transform.position.y - ajusteAltura;

    [Header("Disparo")]
    public GameObject prefabBala;
    public Vector2 salidaBala = new Vector2(1.4f, 1f);  // de los pies hacia la boca del taladro
    public float cadencia = 2.5f;         // segundos entre disparos
    public float retrasoInicial = 1.2f;
    public float toleranciaCarril = 1f;

    [Header("Reacciones")]
    public Sprite spriteGolpe1;           // destello
    public Sprite spriteGolpe2;           // tinte rosa
    public float duracionGolpe = 0.25f;
    public GameObject prefabMuerte;

    [Header("Sonidos")]
    public AudioClip sonidoDisparo;      // cuando dispara
    public AudioClip sonidoGolpe;        // cuando Harper le pega
    public AudioClip sonidoMuerte;       // cuando explota

    Rigidbody2D rb;
    SpriteRenderer sr;
    Sprite spriteNormal;
    Transform harper;
    Vector2 mover;
    int dirHacia = -1;
    float proximoDisparo, finAturdido;
    Coroutine reaccion;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0;
        rb.freezeRotation = true;
        sr = GetComponentInChildren<SpriteRenderer>();
        if (sr != null) spriteNormal = sr.sprite;
        proximoDisparo = Time.time + retrasoInicial;
    }

    void Start()
    {
        var v = FindFirstObjectByType<VidaHarper>();
        distanciaDisparo = Random.Range(distanciaMin, distanciaMax);
        Debug.Log("Robot empieza con vida: " + vida);
        if (v != null) harper = v.transform;
        else Debug.LogWarning("Enemigo: Harper no tiene el script VidaHarper");
    }

    void Update()
    {
        mover = Vector2.zero;
        if (harper == null) return;

        float dx = harper.position.x - transform.position.x;
        float dy = harper.position.y + ajusteAltura - transform.position.y;
        dirHacia = dx >= 0f ? 1 : -1;
        if (sr != null) sr.flipX = miraIzquierdaPorDefecto ? dirHacia > 0 : dirHacia < 0;

        if (Time.time < finAturdido) return;   // aturdido: no se mueve ni dispara

        if (Mathf.Abs(dx) > distanciaDisparo) mover.x = dirHacia;                 // muy lejos: se acerca
        else if (Mathf.Abs(dx) < distanciaDisparo - 1.5f) mover.x = -dirHacia;   // muy cerca: retrocede
        if (Mathf.Abs(dy) > margenCarril) mover.y = Mathf.Sign(dy);
        mover = mover.normalized;

        if (Time.time >= proximoDisparo
            && Mathf.Abs(dx) <= distanciaDisparo + 1f
            && Mathf.Abs(dy) <= toleranciaCarril)
            Disparar();
    }

    void FixedUpdate()
    {
        Vector2 p = rb.position + mover * velocidad * Time.fixedDeltaTime;
        p.x = Mathf.Clamp(p.x, xMin, xMax);
        p.y = Mathf.Clamp(p.y, yMin + ajusteAltura, yMax + ajusteAltura);
        rb.MovePosition(p);
    }

    void Disparar()
    {
        proximoDisparo = Time.time + cadencia;
        distanciaDisparo = Random.Range(distanciaMin, distanciaMax);   // la próxima vez, otra distancia
        if (prefabBala == null) { Debug.LogWarning("Enemigo: falta Prefab Bala"); return; }
        Sonidos.Tocar(sonidoDisparo);

        Vector3 pos = transform.position +
            new Vector3(dirHacia * Mathf.Abs(salidaBala.x), salidaBala.y, 0f);
        var b = Instantiate(prefabBala, pos, Quaternion.identity);
        var bala = b.GetComponent<BalaRobot>();
        if (bala != null) bala.Lanzar(dirHacia, AlturaCarril);
    }

    public void RecibirDano(int d)
    {
        vida -= d;
        Debug.Log("Robot recibió " + d + " de daño. Le queda: " + vida);
        if (vida <= 0) { Morir(); return; }
        Sonidos.Tocar(sonidoGolpe);

        finAturdido = Time.time + duracionGolpe * 2f;
        if (reaccion != null) StopCoroutine(reaccion);
        reaccion = StartCoroutine(Reaccion());
    }

    IEnumerator Reaccion()
    {
        if (sr == null) yield break;
        if (spriteGolpe1 != null && spriteGolpe2 != null)
        {
            sr.sprite = spriteGolpe1;
            yield return new WaitForSeconds(duracionGolpe);
            sr.sprite = spriteGolpe2;
            yield return new WaitForSeconds(duracionGolpe);
        }
        else
        {
            sr.color = Color.red;
            yield return new WaitForSeconds(0.1f);
            sr.color = Color.white;
        }
        sr.sprite = spriteNormal;
    }

    void Morir()
    {
        Sonidos.Tocar(sonidoMuerte);
        if (prefabMuerte != null)
            Instantiate(prefabMuerte, transform.position + Vector3.up * 0.5f, Quaternion.identity);
        Destroy(gameObject);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position + new Vector3(Mathf.Abs(salidaBala.x), salidaBala.y, 0f), 0.2f);
    }
}
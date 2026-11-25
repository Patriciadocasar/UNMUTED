using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

// Los poderes especiales de cada nivel
public enum TipoPoder { AcordeExplosivo, BolaDeDisco, PoderDelFandom, EscudoDeEstrellas }

[RequireComponent(typeof(EnergiaSonica))]
[RequireComponent(typeof(HarperMovimiento))]
public class HarperAtaque : MonoBehaviour
{
    [Header("Efectos (prefabs)")]
    public GameObject prefabGolpe;
    public GameObject prefabAcorde;
    public GameObject prefabImpacto;

    [Header("Pose de ataque")]
    public Sprite spriteAtaque;          // Harper1unity
    public float duracionPose = 0.3f;    // cuánto dura la pose
    // De dónde sale el poder: desde los pies, hacia adelante y arriba (la guitarra)
    public Vector2 salidaDisparo = new Vector2(1.1f, 2f);

    [Header("Ataque básico (J o Z)")]
    public int dano = 1;
    public float cooldown = 0.3f;
    public float energiaPorGolpe = 5f;
    public float energiaPorImpacto = 5f;
    public float alturaEfecto = 1.6f;    // altura de la chispa en el robot

    [Header("PODER ESPECIAL (K o X): elige cuál usa este nivel")]
    public TipoPoder poder = TipoPoder.AcordeExplosivo;

    [Header("Acorde Explosivo (Olivia)")]
    public int danoAcorde = 99;

    [Header("Bola de Disco (ABBA)")]
    public float duracionBaile = 4f;           // segundos que los robots bailan
    public GameObject prefabBolaDisco;         // la bola que sale volando hacia los robots
    public float tamanoBola = 0.5f;            // qué tan grande se ve la bola volando
    public AudioClip sonidoBolaDisco;

    [Header("Poder del Fandom (BTS)")]
    public int vidaQueCura = 10;
    public GameObject prefabFandom;            // efecto opcional sobre Harper
    public AudioClip sonidoFandom;

    [Header("Escudo de Estrellas (Aitana)")]
    public float duracionEscudo = 5f;
    public GameObject prefabEscudo;            // efecto opcional sobre Harper
    public AudioClip sonidoEscudo;

    [Header("Sonidos")]
    public AudioClip sonidoDisparo;      // cada golpe básico
    public AudioClip sonidoAcorde;       // el Acorde Explosivo

    EnergiaSonica energia;
    VidaHarper vida;
    HarperMovimiento mov;
    SpriteRenderer sr;
    Sprite spriteNormal;
    float proximoGolpe;
    float finPose;
    HarperAgachar agachar;   // NUEVO

    // NUEVO (animación): true mientras se muestra la pose de ataque
    public bool EnPose => finPose > 0f;

    void Awake()
    {
        energia = GetComponent<EnergiaSonica>();
        vida = GetComponent<VidaHarper>();
        MovimientoLucesDisco.multiplicadorFiesta = 1f;
        mov = GetComponent<HarperMovimiento>();
        sr = GetComponentInChildren<SpriteRenderer>();
        if (sr != null) spriteNormal = sr.sprite;
        agachar = GetComponent<HarperAgachar>();   // NUEVO
    }

    void Update()
    {
        var k = Keyboard.current;
        if (k == null) return;

        // Volver al sprite normal cuando termina la pose
        if (finPose > 0f && Time.time >= finPose)
        {
            if (sr != null && spriteNormal != null) sr.sprite = spriteNormal;
            finPose = 0f;
        }

        // NUEVO: agachada o aturdida por un golpe no puede disparar (J) ni usar el especial (K)
        if (agachar != null && agachar.Agachada) return;
        if (vida != null && vida.Aturdida) return;

        if ((k.jKey.wasPressedThisFrame || k.zKey.wasPressedThisFrame)
            && Time.time >= proximoGolpe)
            AtaqueBasico();

        if (k.kKey.wasPressedThisFrame || k.xKey.wasPressedThisFrame)
            UsarPoder();
    }

    Vector3 PuntoDeSalida(int dir)
    {
        return transform.position + new Vector3(dir * salidaDisparo.x, salidaDisparo.y, 0f);
    }

    void PonerPose()
    {
        if (sr == null || spriteAtaque == null) return;
        sr.sprite = spriteAtaque;
        finPose = Time.time + duracionPose;
    }

    void AtaqueBasico()
    {
        proximoGolpe = Time.time + cooldown;
        Sonidos.Tocar(sonidoDisparo);
        energia.Sumar(energiaPorGolpe);
        PonerPose();

        if (prefabGolpe == null)
        {
            Debug.LogWarning("Falta arrastrar Prefab Golpe en Harper Ataque");
            return;
        }

        int dir = mov.Direccion;
        var g = Instantiate(prefabGolpe, PuntoDeSalida(dir), Quaternion.identity);
        g.transform.localScale = new Vector3(dir, 1f, 1f);

        var ef = g.GetComponent<EfectoAnimado>();
        if (ef != null)
            ef.Lanzar(dir, dano, transform.position.y, energia, energiaPorImpacto, prefabImpacto);
        else
            Debug.LogWarning("El prefab EfectoGolpe no tiene el script EfectoAnimado");
    }

    void UsarPoder()
    {
        if (!energia.EstaLlena) return;
        energia.GastarTodo();
        PonerPose();

        switch (poder)
        {
            case TipoPoder.AcordeExplosivo:   AcordeExplosivo(); break;
            case TipoPoder.BolaDeDisco:       BolaDeDisco();     break;
            case TipoPoder.PoderDelFandom:    PoderDelFandom();  break;
            case TipoPoder.EscudoDeEstrellas: EscudoDeEstrellas(); break;
        }
    }

    // Robots en pantalla (los de fuera de cámara no se ven afectados)
    System.Collections.Generic.List<Enemigo> RobotsEnPantalla()
    {
        var lista = new System.Collections.Generic.List<Enemigo>();
        var cam = Camera.main;
        foreach (var e in FindObjectsByType<Enemigo>())
        {
            Vector3 v = cam.WorldToViewportPoint(e.transform.position);
            if (v.x >= 0f && v.x <= 1f && v.y >= 0f && v.y <= 1f) lista.Add(e);
        }
        return lista;
    }

    void AcordeExplosivo()
    {
        Sonidos.Tocar(sonidoAcorde);

        if (prefabAcorde != null)
            Instantiate(prefabAcorde, PuntoDeSalida(mov.Direccion), Quaternion.identity);

        foreach (var e in RobotsEnPantalla())
        {
            if (prefabImpacto != null)
                Instantiate(prefabImpacto, e.transform.position + Vector3.up * alturaEfecto, Quaternion.identity);
            e.RecibirDano(danoAcorde);   // mismo impacto, mucho más daño
        }
    }

    // 🪩 ABBA: la bola sale volando de la guitarra, llega a los robots y los marea
    void BolaDeDisco()
    {
        Sonidos.Tocar(sonidoBolaDisco);
        StartCoroutine(LanzarBolaDisco(RobotsEnPantalla()));
        StartCoroutine(FiestaDeLuces(duracionBaile + 0.8f));
    }

    IEnumerator LanzarBolaDisco(System.Collections.Generic.List<Enemigo> robots)
    {
        Vector3 inicio = PuntoDeSalida(mov.Direccion);

        // Destino: el centro de los robots en pantalla (si no hay, hacia adelante)
        Vector3 destino = inicio + new Vector3(mov.Direccion * 8f, 0f, 0f);
        if (robots.Count > 0)
        {
            Vector3 suma = Vector3.zero;
            foreach (var e in robots) suma += e.transform.position;
            destino = suma / robots.Count + Vector3.up * alturaEfecto;
        }

        GameObject bola = null;
        Vector3 escala = Vector3.one;
        if (prefabBolaDisco != null)
        {
            bola = Instantiate(prefabBolaDisco, inicio, Quaternion.identity);
            escala = bola.transform.localScale * tamanoBola;
            var anim = bola.GetComponent<EfectoAnimado>();
            if (anim != null) { anim.repetirAnimacion = true; anim.fps = 14f; anim.desvanecer = false; anim.duracionTotal = 10f; }
        }

        // Vuela en arco, girando y creciendo
        float dur = Mathf.Clamp(Vector3.Distance(inicio, destino) / 14f, 0.35f, 0.9f);
        for (float t = 0f; t < dur; t += Time.deltaTime)
        {
            float p = t / dur;
            if (bola != null)
            {
                Vector3 pos = Vector3.Lerp(inicio, destino, p);
                pos.y += Mathf.Sin(p * Mathf.PI) * 2f;          // el arco
                bola.transform.position = pos;
                bola.transform.localScale = escala * Mathf.Lerp(0.4f, 1f, p);
            }
            yield return null;
        }

        // ¡Llegó! Los robots quedan mareados bailando
        foreach (var e in robots) if (e != null) e.Hipnotizar(duracionBaile);
        if (prefabImpacto != null) Instantiate(prefabImpacto, destino, Quaternion.identity);

        // La bola hace "pop" y desaparece
        if (bola != null)
        {
            for (float t = 0f; t < 0.2f; t += Time.deltaTime)
            {
                bola.transform.localScale = escala * Mathf.Lerp(1f, 1.7f, t / 0.2f);
                yield return null;
            }
            Destroy(bola);
        }
    }

    IEnumerator FiestaDeLuces(float segundos)
    {
        MovimientoLucesDisco.multiplicadorFiesta = 5f;   // los focos se vuelven locos
        yield return new WaitForSeconds(segundos);
        MovimientoLucesDisco.multiplicadorFiesta = 1f;
    }

    // 💜 BTS: Harper recupera vida
    void PoderDelFandom()
    {
        Sonidos.Tocar(sonidoFandom);
        if (prefabFandom != null)
            Instantiate(prefabFandom, transform.position + Vector3.up * alturaEfecto, Quaternion.identity);
        if (vida != null) vida.Curar(vidaQueCura);
    }

    // 🌟 Aitana: Harper queda invencible
    void EscudoDeEstrellas()
    {
        Sonidos.Tocar(sonidoEscudo);
        if (prefabEscudo != null)
            Instantiate(prefabEscudo, transform.position + Vector3.up * alturaEfecto, Quaternion.identity);
        if (vida != null) vida.HacerInvencible(duracionEscudo);
    }

    // Muestra en la Scene de dónde sale el poder (círculo amarillo)
    void OnDrawGizmosSelected()
    {
        int dir = 1;
        var m = GetComponent<HarperMovimiento>();
        if (m != null && Application.isPlaying) dir = m.Direccion;
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(PuntoDeSalida(dir), 0.25f);
    }
}
using UnityEngine;
using UnityEngine.InputSystem;

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

    [Header("Acorde Explosivo (K o X)")]
    public int danoAcorde = 99;

    EnergiaSonica energia;
    HarperMovimiento mov;
    SpriteRenderer sr;
    Sprite spriteNormal;
    float proximoGolpe;
    float finPose;

    void Awake()
    {
        energia = GetComponent<EnergiaSonica>();
        mov = GetComponent<HarperMovimiento>();
        sr = GetComponentInChildren<SpriteRenderer>();
        if (sr != null) spriteNormal = sr.sprite;
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

        if ((k.jKey.wasPressedThisFrame || k.zKey.wasPressedThisFrame)
            && Time.time >= proximoGolpe)
            AtaqueBasico();

        if (k.kKey.wasPressedThisFrame || k.xKey.wasPressedThisFrame)
            AcordeExplosivo();
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

    void AcordeExplosivo()
    {
        if (!energia.EstaLlena) return;
        energia.GastarTodo();
        PonerPose();

        var cam = Camera.main;
        if (prefabAcorde != null)
            Instantiate(prefabAcorde, PuntoDeSalida(mov.Direccion), Quaternion.identity);

        foreach (var e in FindObjectsByType<Enemigo>(FindObjectsSortMode.None))
        {
            Vector3 v = cam.WorldToViewportPoint(e.transform.position);
            bool enPantalla = v.x >= 0f && v.x <= 1f && v.y >= 0f && v.y <= 1f;
            if (!enPantalla) continue;

            if (prefabImpacto != null)
                Instantiate(prefabImpacto, e.transform.position + Vector3.up * alturaEfecto, Quaternion.identity);
            e.RecibirDano(danoAcorde);   // mismo impacto, mucho más daño
        }
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
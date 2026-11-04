using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class BalaRobot : MonoBehaviour
{
    public Sprite[] frames;
    public float fps = 10f;
    public float velocidad = 6f;
    public int dano = 1;
    public float toleranciaCarril = 1f;                 // altura a la que puede golpear a Harper
    public Vector2 tamanoHit = new Vector2(0.8f, 0.5f);
    public float duracionMax = 4f;

    SpriteRenderer sr;
    int dir = -1;
    float carrilY, t;

    void Awake() { sr = GetComponent<SpriteRenderer>(); }

    public void Lanzar(int direccion, float carril)
    {
        dir = direccion;
        carrilY = carril;
        sr.flipX = dir > 0;   // el dibujo mira a la izquierda
    }

    void Update()
    {
        t += Time.deltaTime;
        if (t > duracionMax) { Destroy(gameObject); return; }

        transform.position += new Vector3(dir * velocidad * Time.deltaTime, 0f, 0f);
        if (frames != null && frames.Length > 0)
            sr.sprite = frames[(int)(t * fps) % frames.Length];

        foreach (var c in Physics2D.OverlapBoxAll(transform.position, tamanoHit, 0f))
        {
            var v = c.GetComponentInParent<VidaHarper>();
            if (v == null) continue;
            if (Mathf.Abs(v.transform.position.y - carrilY) > toleranciaCarril) continue;

            v.RecibirDano(dano);
            Destroy(gameObject);
            return;
        }
    }
}
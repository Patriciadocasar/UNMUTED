using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Tutorial guiado de UNMUTED.
// Pon este script en un objeto vacío llamado "Tutorial" en la escena TUTORIAL.
// Muestra un paso a la vez y avanza solo cuando Harper hace lo que se pide.
// No modifica ningún script de Harper: solo "mira" lo que ya hace
// (HarperMovimiento, HarperAgachar y EnergiaSonica).
// El cartel se arma solo por código, con la paleta del Nivel 1.
public class TutorialManager : MonoBehaviour
{
    [Header("Al terminar")]
    [Tooltip("Nombre exacto de la escena a cargar. Vacío = la siguiente en File > Build Profiles.")]
    public string escenaSiguiente = "";
    public float esperaFinal = 2.5f;

    [Header("Tamaño y posición del cartel")]
    public bool panelAbajo = true;                          // desmárcalo si el cartel tapa a Harper
    [Range(0.4f, 0.98f)] public float anchoPanel = 0.88f;   // fracción del ancho de la pantalla
    [Range(0.1f, 0.4f)] public float altoPanel = 0.19f;     // fracción del alto de la pantalla
    [Range(0f, 0.2f)] public float margenBorde = 0.035f;    // separación con el borde de la pantalla

    [Header("Letra")]
    [Tooltip("Opcional: arrastra aquí una fuente pixel art. Si está vacía usa la de Unity.")]
    public Font fuente;
    public int tamanoTitulo = 42;
    public int tamanoTexto = 30;
    public int tamanoAyuda = 22;

    [Header("Colores (paleta del Nivel 1)")]
    public Color colorNegro = new Color32(0x14, 0x14, 0x14, 255);
    public Color colorBorde = new Color32(0xF2, 0xB0, 0x1E, 255);      // dorado
    public Color colorFondo = new Color32(0x2B, 0x17, 0x66, 255);      // morado oscuro
    public Color colorFranja = new Color32(0xE8, 0x71, 0x9F, 255);     // rosa de techo
    public Color colorInsignia = new Color32(0xC9, 0x3A, 0x86, 255);   // magenta
    public Color colorTitulo = new Color32(0xF2, 0xB0, 0x1E, 255);     // dorado
    public Color colorTexto = new Color32(0xF4, 0xEE, 0xFF, 255);
    public Color colorAyuda = new Color32(0xB3, 0x9D, 0xDB, 255);      // lila
    public Color colorListo = new Color32(0x5F, 0xE0, 0xC8, 255);      // teal claro

    [Header("Sonido (opcional)")]
    public AudioClip sonidoPasoCompletado;

    // colores de las teclitas
    static readonly Color caraNormal = new Color32(0xF4, 0xEE, 0xFF, 255);
    static readonly Color lomoNormal = new Color32(0x8F, 0x78, 0xC8, 255);
    static readonly Color caraPulsada = new Color32(0xF2, 0xB0, 0x1E, 255);
    static readonly Color lomoPulsado = new Color32(0xB9, 0x7E, 0x0C, 255);

    // ---- Harper ----
    HarperMovimiento mov;
    HarperAgachar agachar;
    EnergiaSonica energia;

    // ---- Contadores (se reinician en cada paso) ----
    float distancia, tiempoAgachada, ultimaEnergia;
    int golpes;
    bool usoEspecial;
    Vector3 ultimaPos;
    bool terminando;

    class Tecla { public string rotulo; public Key[] keys; }
    class Cap { public Image cara, lomo; public Key[] keys; }
    class Paso
    {
        public string titulo, texto;
        public Tecla[] teclas;               // teclas que se dibujan a la derecha (puede ser null)
        public System.Func<bool> cumplido;   // null = esperar ENTER
    }
    static Tecla T(string rotulo, params Key[] keys) { return new Tecla { rotulo = rotulo, keys = keys }; }

    List<Paso> pasos;
    readonly List<Cap> caps = new List<Cap>();
    readonly List<Image> pips = new List<Image>();

    // ---- UI ----
    CanvasGroup grupo;
    Image bordeDorado;
    Text txtNumero, txtTitulo, txtTexto, txtAyuda;
    RectTransform bloqueTextos, contTeclas;
    bool esperaEnter;

    string K(string s) { return "<color=#" + ColorUtility.ToHtmlStringRGB(colorTitulo) + ">" + s + "</color>"; }

    void Start()
    {
        mov = FindAnyObjectByType<HarperMovimiento>();
        if (mov == null)
        {
            Debug.LogWarning("Tutorial: no encuentro a Harper en la escena");
            enabled = false;
            return;
        }
        agachar = mov.GetComponent<HarperAgachar>();
        energia = mov.GetComponent<EnergiaSonica>();
        ultimaPos = mov.transform.position;

        // Aquí puedes cambiar los textos o agregar/quitar pasos
        pasos = new List<Paso>
        {
            new Paso {
                titulo = "¡Esto es UNMUTED!",
                texto  = "Eres Harper y los robots sin alma quieren silenciar la música. Te enseñamos lo básico en un minuto.",
                teclas = new[] { T("ENTER", Key.Enter, Key.NumpadEnter) },
                cumplido = null },
            new Paso {
                titulo = "Muévete",
                texto  = "Usa " + K("W A S D") + " o las " + K("flechas") + " para caminar por el escenario.",
                teclas = new[] { T("W", Key.W, Key.UpArrow), T("A", Key.A, Key.LeftArrow), T("S", Key.S, Key.DownArrow), T("D", Key.D, Key.RightArrow) },
                cumplido = () => distancia >= 8f },
            new Paso {
                titulo = "Agáchate",
                texto  = "Mantén " + K("ESPACIO") + " o " + K("C") + " para agacharte y esquivar las balas que pasan por arriba.",
                teclas = new[] { T("ESPACIO", Key.Space), T("C", Key.C) },
                cumplido = () => tiempoAgachada >= 0.5f },
            new Paso {
                titulo = "Ataca",
                texto  = "Presiona " + K("J") + " o " + K("Z") + " para atacar. ¡Pruébalo unas cuantas veces!",
                teclas = new[] { T("J", Key.J), T("Z", Key.Z) },
                cumplido = () => golpes >= 3 },
            new Paso {
                titulo = "Carga tu energía",
                texto  = "Cada golpe llena la barra de energía sónica. Sigue atacando hasta llenarla.",
                teclas = new[] { T("J", Key.J), T("Z", Key.Z) },
                cumplido = () => energia != null && energia.EstaLlena },
            new Paso {
                titulo = "Poder especial",
                texto  = "¡La barra está llena! Presiona " + K("K") + " o " + K("X") + " para soltar tu poder especial.",
                teclas = new[] { T("K", Key.K), T("X", Key.X) },
                cumplido = () => usoEspecial },
            new Paso {
                titulo = "Cuídate",
                texto  = "Los robots disparan: esquiva o agáchate. Los corazones dan vida y las notas dan energía sónica.",
                teclas = new[] { T("ENTER", Key.Enter, Key.NumpadEnter) },
                cumplido = null },
            new Paso {
                titulo = "¡Listo!",
                texto  = "Ya sabes jugar. ¡Suerte en el Nivel 1!",
                teclas = null,
                cumplido = null },   // el último paso se cierra solo
        };

        CrearUI();
        StartCoroutine(Rutina());
    }

    void Update()
    {
        var k = Keyboard.current;
        if (k != null && k.escapeKey.wasPressedThisFrame) { Terminar(); return; }   // saltar tutorial

        // Medimos lo que hace Harper
        distancia += Vector3.Distance(mov.transform.position, ultimaPos);
        ultimaPos = mov.transform.position;

        if (agachar != null && agachar.Progreso >= 1f) tiempoAgachada += Time.deltaTime;

        if (energia != null)
        {
            if (energia.Actual > ultimaEnergia) golpes++;   // la energía sube con cada golpe
            if (ultimaEnergia >= energia.maxima && energia.Actual < energia.maxima) usoEspecial = true;   // se vació = usó el especial
            ultimaEnergia = energia.Actual;
        }

        // Las teclitas del cartel se iluminan cuando las presionas
        foreach (var c in caps)
        {
            bool pulsada = false;
            if (k != null)
                foreach (var key in c.keys)
                    if (k[key].isPressed) { pulsada = true; break; }
            c.cara.color = pulsada ? caraPulsada : caraNormal;
            c.lomo.color = pulsada ? lomoPulsado : lomoNormal;
        }

        // "Presiona ENTER" parpadea suavemente
        if (txtAyuda != null)
        {
            var c = colorAyuda;
            if (esperaEnter) c.a = 0.7f + 0.3f * Mathf.Sin(Time.time * 5f);
            txtAyuda.color = c;
        }
    }

    IEnumerator Rutina()
    {
        for (int i = 0; i < pasos.Count; i++)
        {
            var p = pasos[i];
            bool ultimo = i == pasos.Count - 1;

            Mostrar(p, i, ultimo);
            ReiniciarContadores();
            if (i == 0) StartCoroutine(Fundido(0f, 1f, 0.35f));
            yield return null;   // un cuadro de margen para que no cuente la tecla anterior

            if (ultimo)
            {
                yield return new WaitForSeconds(esperaFinal);
                yield return Fundido(1f, 0f, 0.35f);
                break;
            }

            if (p.cumplido == null) { while (!PresionoEnter()) yield return null; }
            else
            {
                while (!p.cumplido()) yield return null;
                bordeDorado.color = colorListo;
                txtTitulo.color = colorListo;
                txtTitulo.text += "  ¡BIEN!";
                ActualizarPips(i, true);
                Sonidos.Tocar(sonidoPasoCompletado);
                yield return new WaitForSeconds(0.9f);
            }
        }
        Terminar();
    }

    bool PresionoEnter()
    {
        var k = Keyboard.current;
        return k != null && (k.enterKey.wasPressedThisFrame || k.numpadEnterKey.wasPressedThisFrame);
    }

    void ReiniciarContadores()
    {
        distancia = 0f; tiempoAgachada = 0f; golpes = 0; usoEspecial = false;
        ultimaPos = mov.transform.position;
        ultimaEnergia = energia != null ? energia.Actual : 0f;
    }

    void Terminar()
    {
        if (terminando) return;
        terminando = true;

        if (!string.IsNullOrEmpty(escenaSiguiente)) { Transicion.IrA(escenaSiguiente); return; }

        int siguiente = SceneManager.GetActiveScene().buildIndex + 1;
        if (siguiente < SceneManager.sceneCountInBuildSettings) Transicion.IrA(siguiente);
        else Debug.LogWarning("Tutorial: no hay una escena siguiente en Build Profiles (¿agregaste TUTORIAL y LEVEL 1 a la lista?)");
    }

    // ---------- Mostrar un paso ----------
    void Mostrar(Paso p, int indice, bool ultimo)
    {
        bordeDorado.color = colorBorde;
        txtTitulo.color = colorTitulo;
        txtTitulo.text = p.titulo;
        txtTexto.text = p.texto;

        esperaEnter = p.cumplido == null && !ultimo;
        string ayuda = "ESC para saltar el tutorial";
        if (esperaEnter) ayuda = "Presiona ENTER para continuar   ·   " + ayuda;
        txtAyuda.text = ultimo ? "" : ayuda;

        txtNumero.text = (indice + 1).ToString();
        ActualizarPips(indice, false);
        ConstruirTeclas(p.teclas);

        StartCoroutine(Pop(bloqueTextos));
        StartCoroutine(Pop(contTeclas));
    }

    void ActualizarPips(int actual, bool completado)
    {
        for (int j = 0; j < pips.Count; j++)
        {
            if (j < actual) pips[j].color = colorListo;
            else if (j == actual) pips[j].color = completado ? colorListo : colorBorde;
            else pips[j].color = colorNegro;
        }
    }

    // Dibuja las teclitas de la derecha (se iluminan en Update cuando las presionas)
    void ConstruirTeclas(Tecla[] lista)
    {
        foreach (Transform hijo in contTeclas) Destroy(hijo.gameObject);
        caps.Clear();
        if (lista == null) return;

        foreach (var d in lista)
        {
            float ancho = Mathf.Max(64f, 30f + d.rotulo.Length * 20f);
            var r = RT("Tecla " + d.rotulo, contTeclas, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            r.sizeDelta = new Vector2(ancho, 64f);
            var contorno = r.gameObject.AddComponent<Image>();
            contorno.color = colorNegro;
            contorno.raycastTarget = false;

            var lomo = Caja("Lomo", r, lomoNormal, 3f);                       // la "parte de abajo" de la tecla
            var caraRT = RT("Cara", r, Vector2.zero, Vector2.one, new Vector2(3f, 10f), new Vector2(-3f, -3f));
            var cara = caraRT.gameObject.AddComponent<Image>();
            cara.color = caraNormal;
            cara.raycastTarget = false;

            var letra = CrearTexto(RT("Letra", caraRT, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero),
                                   28, FontStyle.Bold, colorFondo, TextAnchor.MiddleCenter, false);
            letra.text = d.rotulo;

            caps.Add(new Cap { cara = cara, lomo = lomo, keys = d.keys });
        }
    }

    // ---------- Animaciones ----------
    IEnumerator Pop(RectTransform rt)
    {
        const float dur = 0.18f;
        for (float t = 0f; t < dur; t += Time.deltaTime)
        {
            float p = t / dur;
            float e = 1f - (1f - p) * (1f - p);   // arranca rápido y frena
            rt.localScale = Vector3.one * Mathf.Lerp(0.9f, 1f, e);
            yield return null;
        }
        rt.localScale = Vector3.one;
    }

    IEnumerator Fundido(float desde, float hasta, float dur)
    {
        for (float t = 0f; t < dur; t += Time.deltaTime)
        {
            grupo.alpha = Mathf.Lerp(desde, hasta, t / dur);
            yield return null;
        }
        grupo.alpha = hasta;
    }

    // ---------- Interfaz (se arma sola, igual que el cartel de ControlNivel) ----------
    RectTransform RT(string nombre, Transform padre, Vector2 aMin, Vector2 aMax, Vector2 oMin, Vector2 oMax)
    {
        var go = new GameObject(nombre, typeof(RectTransform));
        var r = (RectTransform)go.transform;
        r.SetParent(padre, false);
        r.anchorMin = aMin; r.anchorMax = aMax;
        r.offsetMin = oMin; r.offsetMax = oMax;
        return r;
    }

    // Rectángulo de color que llena a su padre, dejando "margen" de borde
    Image Caja(string nombre, Transform padre, Color color, float margen)
    {
        var r = RT(nombre, padre, Vector2.zero, Vector2.one, new Vector2(margen, margen), new Vector2(-margen, -margen));
        var img = r.gameObject.AddComponent<Image>();
        img.color = color;
        img.raycastTarget = false;
        return img;
    }

    Text CrearTexto(RectTransform r, int tamano, FontStyle estilo, Color color, TextAnchor ancla, bool contorno)
    {
        var t = r.gameObject.AddComponent<Text>();
        t.font = fuente != null ? fuente : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        t.fontSize = tamano;
        t.fontStyle = estilo;
        t.color = color;
        t.alignment = ancla;
        t.supportRichText = true;
        t.horizontalOverflow = HorizontalWrapMode.Wrap;
        t.verticalOverflow = VerticalWrapMode.Overflow;
        t.raycastTarget = false;
        if (contorno)
        {
            var o = r.gameObject.AddComponent<Outline>();
            o.effectColor = colorNegro;
            o.effectDistance = new Vector2(3f, -3f);
        }
        return t;
    }

    void CrearUI()
    {
        var lienzo = new GameObject("CartelTutorial");
        var canvas = lienzo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 90;
        var escala = lienzo.AddComponent<CanvasScaler>();
        escala.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        escala.referenceResolution = new Vector2(1920, 1080);

        // Panel ancho y bajo
        float x0 = (1f - anchoPanel) * 0.5f;
        float y0 = panelAbajo ? margenBorde : 1f - margenBorde - altoPanel;
        var panel = RT("Panel", lienzo.transform, new Vector2(x0, y0), new Vector2(1f - x0, y0 + altoPanel), Vector2.zero, Vector2.zero);
        grupo = panel.gameObject.AddComponent<CanvasGroup>();
        grupo.alpha = 0f;
        grupo.interactable = false;
        grupo.blocksRaycasts = false;

        // Marco estilo pixel art: sombra, negro, dorado, negro, fondo morado y franja rosa arriba
        var sombra = Caja("Sombra", panel, new Color(0f, 0f, 0f, 0.4f), 0f);
        sombra.rectTransform.offsetMin = new Vector2(8f, -10f);
        sombra.rectTransform.offsetMax = new Vector2(8f, -10f);
        Caja("BordeNegro", panel, colorNegro, 0f);
        bordeDorado = Caja("BordeDorado", panel, colorBorde, 5f);
        Caja("BordeNegroInterno", panel, colorNegro, 10f);
        Caja("Fondo", panel, colorFondo, 13f);
        var franja = RT("Franja", panel, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(13f, -19f), new Vector2(-13f, -13f));
        var franjaImg = franja.gameObject.AddComponent<Image>();
        franjaImg.color = colorFranja;
        franjaImg.raycastTarget = false;

        var cont = RT("Contenido", panel, Vector2.zero, Vector2.one, new Vector2(26f, 22f), new Vector2(-26f, -30f));

        // Insignia con el número de paso y los cuadritos de progreso
        var ins = RT("Insignia", cont, new Vector2(0f, 0f), new Vector2(0f, 1f), Vector2.zero, new Vector2(120f, 0f));
        Caja("MarcoInsignia", ins, colorNegro, 0f);
        Caja("FondoInsignia", ins, colorInsignia, 4f);
        txtNumero = CrearTexto(RT("Numero", ins, new Vector2(0f, 0.32f), Vector2.one, Vector2.zero, Vector2.zero),
                               70, FontStyle.Bold, Color.white, TextAnchor.MiddleCenter, true);

        var filaPips = RT("Progreso", ins, Vector2.zero, new Vector2(1f, 0.3f), new Vector2(0f, 6f), Vector2.zero);
        var lg = filaPips.gameObject.AddComponent<HorizontalLayoutGroup>();
        lg.childAlignment = TextAnchor.MiddleCenter;
        lg.spacing = 3f;
        lg.childControlWidth = false; lg.childControlHeight = false;
        lg.childForceExpandWidth = false; lg.childForceExpandHeight = false;
        float lado = Mathf.Min(11f, (100f - 3f * (pasos.Count - 1)) / pasos.Count);
        for (int i = 0; i < pasos.Count; i++)
        {
            var pr = RT("Pip " + (i + 1), filaPips, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            pr.sizeDelta = new Vector2(lado, lado);
            var pim = pr.gameObject.AddComponent<Image>();
            pim.raycastTarget = false;
            pips.Add(pim);
        }

        // Textos: título, instrucción y ayuda (alineados a la izquierda, en horizontal)
        bloqueTextos = RT("Textos", cont, Vector2.zero, Vector2.one, new Vector2(142f, 0f), new Vector2(-400f, 0f));
        txtTitulo = CrearTexto(RT("Titulo", bloqueTextos, new Vector2(0f, 0.62f), Vector2.one, Vector2.zero, Vector2.zero),
                               tamanoTitulo, FontStyle.Bold, colorTitulo, TextAnchor.MiddleLeft, true);
        txtTexto = CrearTexto(RT("Texto", bloqueTextos, new Vector2(0f, 0.18f), new Vector2(1f, 0.62f), Vector2.zero, Vector2.zero),
                              tamanoTexto, FontStyle.Normal, colorTexto, TextAnchor.MiddleLeft, false);
        txtAyuda = CrearTexto(RT("Ayuda", bloqueTextos, Vector2.zero, new Vector2(1f, 0.18f), Vector2.zero, Vector2.zero),
                              tamanoAyuda, FontStyle.Italic, colorAyuda, TextAnchor.MiddleLeft, false);

        // Teclas a la derecha
        contTeclas = RT("Teclas", cont, new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(-390f, 0f), Vector2.zero);
        var hl = contTeclas.gameObject.AddComponent<HorizontalLayoutGroup>();
        hl.childAlignment = TextAnchor.MiddleRight;
        hl.spacing = 12f;
        hl.childControlWidth = false; hl.childControlHeight = false;
        hl.childForceExpandWidth = false; hl.childForceExpandHeight = false;
    }
}
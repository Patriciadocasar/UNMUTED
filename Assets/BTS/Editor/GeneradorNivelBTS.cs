// UNMUTED - Generador del Nivel 2: Estudio BTS
// Menú: UNMUTED > Generar Nivel 2 (BTS)
//
// Qué hace:
//  1. Configura los PNG de Assets/BTS/Sprites como pixel art (Point, sin compresión, PPU 8/16).
//  2. Crea los Tiles en Assets/BTS/Tiles.
//  3. Duplica LEVEL1.unity como LEVEL2_BTS.unity (conserva a Harper, el robot, la UI y la cámara).
//  4. Quita los props de Olivia Rodrigo, repinta los Tilemaps y coloca el escenario de BTS.
//  5. Añade luces 2D, el reloj musical (120 BPM), la zona caminable y las oleadas.
//  El nivel está a color desde el inicio.
//  6. Agrega la escena al Build Profile y la guarda.
// Puedes volver a ejecutarlo cuando quieras: regenera la escena desde cero.
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.Tilemaps;

public static class GeneradorNivelBTS
{
    const string RAIZ = "Assets/BTS";
    const string SPRITES_PROPS = RAIZ + "/Sprites/Props";
    const string SPRITES_TILES = RAIZ + "/Sprites/Tiles";
    const string CARPETA_TILES = RAIZ + "/Tiles";
    const string ESCENA_BASE = "Assets/Scenes/LEVEL1.unity";
    const string ESCENA_NUEVA = "Assets/Scenes/LEVEL2_BTS.unity";

    struct PropDef
    {
        public string nombre, sprite; public int frames; public float x, y; public int orden; public float fpp, latido;
        public PropDef(string n, string s, int f, float x, float y, int o, float fpp, float lat)
        { nombre = n; sprite = s; frames = f; this.x = x; this.y = y; orden = o; this.fpp = fpp; latido = lat; }
    }
    struct LuzDef
    {
        public string nombre; public float x, y; public Color color; public float radio, intensidad, pulso;
        public LuzDef(string n, float x, float y, float r, float g, float b, float radio, float i, float p)
        { nombre = n; this.x = x; this.y = y; color = new Color(r, g, b); this.radio = radio; intensidad = i; pulso = p; }
    }

    // ---- Disposición (generada desde layout.py; coordenadas = esquina inferior izquierda) ----
    static readonly PropDef[] PROPS =
    {
        new PropDef("Pantalla LED", "pantalla_led", 3, 8.0f, 6.1f, 1, 2.0f, 0.0f),
        new PropDef("Letrero Love", "letrero_love", 2, 9.25f, 10.7f, 2, 1.0f, 0.0f),
        new PropDef("Columna cristal L", "columna_cristal", 2, 5.3f, 6.0f, 2, 1.0f, 0.0f),
        new PropDef("Columna cristal R", "columna_cristal", 2, 22.2f, 6.0f, 2, 1.0f, 0.0f),
        new PropDef("Puerta BTS", "puerta_bts", 1, 0.2f, 5.8f, 2, 0.0f, 0.0f),
        new PropDef("Letrero Answer", "letrero_answer", 2, 0.0f, 12.3f, 2, 1.0f, 0.0f),
        new PropDef("Letrero Love Yourself", "letrero_love_yourself", 2, 23.3f, 12.4f, 2, 1.0f, 0.0f),
        new PropDef("Corazon Borahae", "corazon_borahae", 2, 24.9f, 8.8f, 2, 0.5f, 0.0f),
        new PropDef("ARMY Bomb", "army_bomb", 2, 25.4f, 5.9f, 3, 0.5f, 0.0f),
        new PropDef("Bocina R", "bocina", 2, 27.8f, 5.8f, 3, 1.0f, 0.0f),
        new PropDef("Truss", "truss", 1, 0.0f, 16.0f, 4, 0.0f, 0.0f),
        new PropDef("Foco 1", "foco", 1, 3.2f, 14.6f, 4, 0.0f, 0.0f),
        new PropDef("Foco 2", "foco", 1, 9.0f, 14.6f, 4, 0.0f, 0.0f),
        new PropDef("Foco 3", "foco", 1, 14.0f, 14.6f, 4, 0.0f, 0.0f),
        new PropDef("Foco 4", "foco", 1, 20.9f, 14.6f, 4, 0.0f, 0.0f),
        new PropDef("Microfono 1", "microfono", 1, 9.1f, 6.0f, 3, 0.0f, 0.0f),
        new PropDef("Microfono 2", "microfono", 1, 11.05f, 6.0f, 3, 0.0f, 0.0f),
        new PropDef("Microfono 3", "microfono", 1, 13.0f, 6.0f, 3, 0.0f, 0.0f),
        new PropDef("Microfono 4", "microfono", 1, 14.95f, 6.0f, 3, 0.0f, 0.0f),
        new PropDef("Microfono 5", "microfono", 1, 16.9f, 6.0f, 3, 0.0f, 0.0f),
        new PropDef("Microfono 6", "microfono", 1, 18.85f, 6.0f, 3, 0.0f, 0.0f),
        new PropDef("Microfono 7", "microfono", 1, 20.8f, 6.0f, 3, 0.0f, 0.0f),
    };

    static readonly LuzDef[] LUCES =
    {
        new LuzDef("Luz Pantalla LED", 15.0f, 10.5f, 1.0f, 0.32f, 0.38f, 11.0f, 1.1f, 0.35f),
        new LuzDef("Luz Letrero Love", 15.0f, 13.0f, 1.0f, 0.85f, 0.45f, 6.0f, 1.0f, 0.5f),
        new LuzDef("Luz Corazon", 26.9f, 10.9f, 0.7f, 0.4f, 1.0f, 4.5f, 1.2f, 0.6f),
        new LuzDef("Luz Columna L", 6.5f, 10.0f, 0.85f, 0.92f, 1.0f, 3.5f, 0.9f, 0.3f),
        new LuzDef("Luz Columna R", 23.5f, 10.0f, 0.85f, 0.92f, 1.0f, 3.5f, 0.9f, 0.3f),
        new LuzDef("Luz Escenario", 15.0f, 3.0f, 1.0f, 0.8f, 0.45f, 10.0f, 0.7f, 0.2f),
    };

    static readonly string[] TILES = { "pared", "pared_franja", "techo", "zocalo", "escenario", "escalon", "piso_azul", "piso_azul_borde" };
    static readonly Color FONDO_CAMARA = new Color(0.06f, 0.05f, 0.12f);

    static string TilePared(int x, int y)
    {
        if (y == 16) return "techo";
        if (y == 15 || y == 7) return "pared_franja";
        return "pared";
    }

    static string TilePiso(int x, int y)
    {
        bool centro = x >= 7 && x <= 22;          // tarima dorada con escalones
        if (y == 6) return "zocalo";
        if (y == 4 || y == 2) return centro ? "escalon" : "piso_azul_borde";
        return centro ? "escenario" : "piso_azul";
    }

    // =====================================================================

    [MenuItem("UNMUTED/Generar Nivel 2 (BTS)")]
    public static void Generar()
    {
        if (!AssetDatabase.IsValidFolder(RAIZ + "/Sprites"))
        {
            EditorUtility.DisplayDialog("Nivel BTS", "No encuentro " + RAIZ + "/Sprites. Copia la carpeta BTS dentro de Assets.", "OK");
            return;
        }
        if (!File.Exists(ESCENA_BASE))
        {
            EditorUtility.DisplayDialog("Nivel BTS", "No encuentro " + ESCENA_BASE + " (se usa como base).", "OK");
            return;
        }
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        if (File.Exists(ESCENA_NUEVA) &&
            !EditorUtility.DisplayDialog("Nivel BTS", ESCENA_NUEVA + " ya existe. ¿Regenerarla? (se reemplaza)", "Sí, regenerar", "Cancelar"))
            return;

        ConfigurarSprites();
        var tiles = CrearTiles();

        AssetDatabase.DeleteAsset(ESCENA_NUEVA);
        AssetDatabase.CopyAsset(ESCENA_BASE, ESCENA_NUEVA);
        var escena = EditorSceneManager.OpenScene(ESCENA_NUEVA, OpenSceneMode.Single);

        // ---- Material lit de los sprites existentes (para que reciban las luces 2D) ----
        Material matSprite = null;
        var harper = Object.FindFirstObjectByType<VidaHarper>();
        if (harper != null)
        {
            var srH = harper.GetComponentInChildren<SpriteRenderer>();
            if (srH != null) matSprite = srH.sharedMaterial;
        }

        // ---- Quitar lo del estudio de Olivia Rodrigo ----
        var propsViejos = GameObject.Find("Props");
        if (propsViejos != null) Object.DestroyImmediate(propsViejos);
        var viejo = GameObject.Find("Escenario BTS");
        if (viejo != null) Object.DestroyImmediate(viejo);

        // ---- Tilemaps ----
        Tilemap mapaPared = BuscarTilemap("Techo y Paredes");
        Tilemap mapaPiso = BuscarTilemap("Piso");
        if (mapaPared == null || mapaPiso == null)
        {
            var grid = GameObject.Find("Grid") ?? new GameObject("Grid", typeof(Grid));
            if (mapaPared == null) mapaPared = CrearTilemap(grid.transform, "Techo y Paredes", 0);
            if (mapaPiso == null) mapaPiso = CrearTilemap(grid.transform, "Piso", 0);
        }
        Pintar(mapaPared, tiles, TilePared, 0, 29, 7, 16);
        Pintar(mapaPiso, tiles, TilePiso, 0, 29, -2, 6);

        // ---- Props ----
        var raiz = new GameObject("Escenario BTS").transform;
        foreach (var p in PROPS)
        {
            var go = new GameObject(p.nombre);
            go.transform.SetParent(raiz);
            go.transform.position = new Vector3(p.x, p.y, 0f);
            var sr = go.AddComponent<SpriteRenderer>();
            if (matSprite != null) sr.sharedMaterial = matSprite;
            sr.sortingOrder = p.orden;

            var frames = new Sprite[p.frames];
            for (int i = 0; i < p.frames; i++)
                frames[i] = CargarSprite(SPRITES_PROPS + "/" + p.sprite + "_" + (i + 1) + ".png");
            sr.sprite = frames[0];

            var pm = go.AddComponent<PropMusical>();
            pm.frames = frames;
            pm.framesPorPulso = p.fpp;
            pm.latido = p.latido;

        }

        // ---- Luces 2D ----
        var raizLuces = new GameObject("Luces BTS").transform;
        raizLuces.SetParent(raiz);
        foreach (var l in LUCES)
        {
            var go = new GameObject(l.nombre);
            go.transform.SetParent(raizLuces);
            go.transform.position = new Vector3(l.x, l.y, 0f);
            var luz = go.AddComponent<Light2D>();
            luz.lightType = Light2D.LightType.Point;
            luz.color = l.color;
            luz.intensity = l.intensidad;
            luz.pointLightOuterRadius = l.radio;
            luz.pointLightInnerRadius = l.radio * 0.15f;
            luz.falloffIntensity = 0.6f;
            var lr = go.AddComponent<LuzRitmo>();
            lr.intensidadBase = l.intensidad;
            lr.pulso = l.pulso;
        }

        Light2D luzGlobal = null;
        foreach (var l in Object.FindObjectsByType<Light2D>(FindObjectsSortMode.None))
        {
            if (l.lightType == Light2D.LightType.Global) luzGlobal = l;
            if (l.gameObject.name == "Light Ambiente Nivel") l.color = new Color(1f, 0.9f, 0.95f);
        }

        // ---- Cámara ----
        var cam = Camera.main;
        if (cam != null) cam.backgroundColor = FONDO_CAMARA;

        // ---- Lógica del nivel ----
        var nivel = new GameObject("Nivel BTS");
        var pulso = nivel.AddComponent<PulsoRitmo>();
        pulso.bpm = 120f;
        var audio = nivel.AddComponent<AudioSource>();
        audio.loop = true;
        pulso.musica = audio;

        audio.playOnAwake = true;                   // la música suena desde el inicio (si asignas un clip)
        if (luzGlobal != null) luzGlobal.intensity = 1f;

        // Zona por donde se camina: solo el piso (no las paredes ni el techo)
        var zona = nivel.AddComponent<ZonaCaminable>();
        zona.piso = mapaPiso;
        zona.Calcular();
        zona.Aplicar();

        var oleadas = nivel.AddComponent<GestorOleadas>();
        oleadas.robotBase = Object.FindFirstObjectByType<Enemigo>();
        oleadas.barraRobot = Object.FindFirstObjectByType<BarraRobotUI>();
        if (oleadas.robotBase == null) Debug.LogWarning("Nivel BTS: no encontré el robot. Arrástralo a GestorOleadas > Robot Base.");

        EditorSceneManager.MarkSceneDirty(escena);
        EditorSceneManager.SaveScene(escena);
        AgregarABuild(ESCENA_NUEVA);

        Selection.activeGameObject = nivel;
        Debug.Log("Nivel 2 (BTS) generado en " + ESCENA_NUEVA);
        EditorUtility.DisplayDialog("Nivel BTS", "Listo: " + ESCENA_NUEVA + "\n\nDale Play para probar. Arrastra la canción al AudioSource de \"Nivel BTS\" para que suene y marque el ritmo.", "OK");
    }

    // Repara solo las baldosas de la escena abierta (sin regenerar todo el nivel)
    [MenuItem("UNMUTED/Reparar baldosas del Nivel 2 (BTS)")]
    public static void RepararBaldosas()
    {
        ConfigurarSprites();
        var tiles = CrearTiles();
        var pared = BuscarTilemap("Techo y Paredes");
        var piso = BuscarTilemap("Piso");
        if (pared == null || piso == null) { Debug.LogWarning("Abre LEVEL2_BTS primero."); return; }
        Pintar(pared, tiles, TilePared, 0, 29, 7, 16);
        Pintar(piso, tiles, TilePiso, 0, 29, -2, 6);
        var zona = Object.FindFirstObjectByType<ZonaCaminable>();
        if (zona != null) { zona.piso = piso; zona.Calcular(); zona.Aplicar(); }
        EditorSceneManager.MarkSceneDirty(piso.gameObject.scene);
        Debug.Log("Baldosas del Nivel 2 repintadas. Guarda la escena (Ctrl+S).");
    }

    // =====================================================================

    static void ConfigurarSprites()
    {
        foreach (var carpeta in new[] { SPRITES_PROPS, SPRITES_TILES })
        {
            foreach (var guid in AssetDatabase.FindAssets("t:Texture2D", new[] { carpeta }))
            {
                string ruta = AssetDatabase.GUIDToAssetPath(guid);
                var ti = AssetImporter.GetAtPath(ruta) as TextureImporter;
                if (ti == null) continue;
                bool esTile = carpeta == SPRITES_TILES;

                ti.textureType = TextureImporterType.Sprite;
                ti.spriteImportMode = SpriteImportMode.Single;
                ti.spritePixelsPerUnit = esTile ? 16 : 8;
                ti.filterMode = FilterMode.Point;
                ti.textureCompression = TextureImporterCompression.Uncompressed;
                ti.mipmapEnabled = false;
                ti.alphaIsTransparency = true;

                var s = new TextureImporterSettings();
                ti.ReadTextureSettings(s);
                s.spriteAlignment = (int)(esTile ? SpriteAlignment.Center : SpriteAlignment.BottomLeft);
                s.spriteMeshType = SpriteMeshType.FullRect;
                ti.SetTextureSettings(s);
                ti.SaveAndReimport();
            }
        }
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
    }

    static Dictionary<string, TileBase> CrearTiles()
    {
        if (!AssetDatabase.IsValidFolder(CARPETA_TILES)) AssetDatabase.CreateFolder(RAIZ, "Tiles");
        var dic = new Dictionary<string, TileBase>();
        var faltan = new List<string>();
        foreach (var n in TILES)
        {
            foreach (var nombre in new[] { n })
            {
                string ruta = CARPETA_TILES + "/" + nombre + ".asset";
                var tile = AssetDatabase.LoadAssetAtPath<Tile>(ruta);
                if (tile == null)
                {
                    tile = ScriptableObject.CreateInstance<Tile>();
                    AssetDatabase.CreateAsset(tile, ruta);
                }
                tile.sprite = CargarSprite(SPRITES_TILES + "/t_" + nombre + ".png");
                if (tile.sprite == null) faltan.Add(nombre);
                tile.colliderType = Tile.ColliderType.None;
                EditorUtility.SetDirty(tile);
                dic[nombre] = tile;
            }
        }
        AssetDatabase.SaveAssets();
        if (faltan.Count > 0)
            EditorUtility.DisplayDialog("Nivel BTS", "Estos tiles quedaron sin sprite (por eso no se ven): " + string.Join(", ", faltan) +
                "\n\nVuelve a ejecutar UNMUTED > Generar Nivel 2 (BTS).", "OK");
        return dic;
    }

    static Sprite CargarSprite(string ruta)
    {
        var s = AssetDatabase.LoadAssetAtPath<Sprite>(ruta);
        if (s == null)
        {
            // A veces el sprite aún no existe justo después de reimportar: forzar importación y reintentar
            AssetDatabase.ImportAsset(ruta, ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport);
            s = AssetDatabase.LoadAllAssetsAtPath(ruta).OfType<Sprite>().FirstOrDefault();
        }
        if (s == null) Debug.LogWarning("Nivel BTS: falta el sprite " + ruta);
        return s;
    }

    static Tilemap BuscarTilemap(string nombre)
    {
        foreach (var t in Object.FindObjectsByType<Tilemap>(FindObjectsSortMode.None))
            if (t.gameObject.name == nombre) return t;
        return null;
    }

    static Tilemap CrearTilemap(Transform grid, string nombre, int orden)
    {
        var go = new GameObject(nombre, typeof(Tilemap), typeof(TilemapRenderer));
        go.transform.SetParent(grid, false);
        go.GetComponent<TilemapRenderer>().sortingOrder = orden;
        return go.GetComponent<Tilemap>();
    }

    static void Pintar(Tilemap mapa, Dictionary<string, TileBase> tiles, System.Func<int, int, string> elegir,
                       int x0, int x1, int y0, int y1)
    {
        mapa.ClearAllTiles();
        for (int x = x0; x <= x1; x++)
            for (int y = y0; y <= y1; y++)
                mapa.SetTile(new Vector3Int(x, y, 0), tiles[elegir(x, y)]);
        mapa.CompressBounds();
    }

    static void AgregarABuild(string ruta)
    {
        var lista = EditorBuildSettings.scenes.ToList();
        if (lista.Any(s => s.path == ruta)) return;
        lista.Add(new EditorBuildSettingsScene(ruta, true));
        EditorBuildSettings.scenes = lista.ToArray();
    }
}

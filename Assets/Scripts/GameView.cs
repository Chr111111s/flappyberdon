using UnityEngine;

/// <summary>
/// Genera todo el arte del juego en tiempo de ejecucion (texturas, sprites,
/// retratos recortados) para no depender de configuracion manual del editor.
/// </summary>
public static class GameView
{
    public const float GroundHeight = 1.15f;

    public static readonly Color SkyTop = new Color(0.30f, 0.68f, 0.91f);
    public static readonly Color SkyBottom = new Color(0.80f, 0.94f, 0.99f);
    public static readonly Color Ink = new Color(0.06f, 0.12f, 0.14f);
    public static readonly Color Gold = new Color(1f, 0.84f, 0.32f);
    public static readonly Color PipeGreen = new Color(0.32f, 0.70f, 0.32f);
    public static readonly Color Dirt = new Color(0.84f, 0.72f, 0.47f);

    static Sprite _white;
    static Sprite _circle;
    static Sprite _softCircle;
    static Sprite _pipeBody;
    static Sprite _pipeCap;
    static Sprite _gradient;
    static Sprite _vignette;

    public static Sprite White
    {
        get
        {
            if (_white == null)
            {
                var tex = NewTex(2, 2);
                var px = new Color[4];
                for (int i = 0; i < px.Length; i++) px[i] = Color.white;
                tex.SetPixels(px);
                tex.Apply();
                _white = MakeSprite(tex, 1f);
                _white.name = "White";
            }
            return _white;
        }
    }

    public static Sprite CircleSprite
    {
        get
        {
            if (_circle == null) _circle = Circle(128, Color.white);
            return _circle;
        }
    }

    public static Sprite SoftCircleSprite
    {
        get
        {
            if (_softCircle == null) _softCircle = SoftCircle(128, Color.white);
            return _softCircle;
        }
    }

    public static Sprite PipeBody
    {
        get
        {
            if (_pipeBody == null) _pipeBody = BuildPipeBody();
            return _pipeBody;
        }
    }

    public static Sprite PipeCap
    {
        get
        {
            if (_pipeCap == null) _pipeCap = BuildPipeCap();
            return _pipeCap;
        }
    }

    public static Sprite SkyGradient
    {
        get
        {
            if (_gradient == null) _gradient = Gradient(SkyTop, SkyBottom);
            return _gradient;
        }
    }

    public static Sprite VignetteSprite
    {
        get
        {
            if (_vignette == null) _vignette = Vignette(256);
            return _vignette;
        }
    }

    public static void ClearCache()
    {
        _white = null;
        _circle = null;
        _softCircle = null;
        _pipeBody = null;
        _pipeCap = null;
        _gradient = null;
        _vignette = null;
    }

    // ------------------------------------------------------------------
    // Sprites de mundo
    // ------------------------------------------------------------------

    public static SpriteRenderer AddSprite(Transform parent, string name, Sprite sprite, int sortingOrder,
        Color tint, Vector3 localPosition, float width, float height)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPosition;

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.color = tint;
        sr.sortingOrder = sortingOrder;

        SetWorldSize(go.transform, sr, width, height);
        return sr;
    }

    public static void SetWorldSize(Transform t, SpriteRenderer sr, float width, float height)
    {
        if (sr == null || sr.sprite == null) return;
        var b = sr.sprite.bounds.size;
        t.localScale = new Vector3(
            width / Mathf.Max(0.0001f, b.x),
            height / Mathf.Max(0.0001f, b.y),
            1f);
    }

    // ------------------------------------------------------------------
    // Retratos
    // ------------------------------------------------------------------

    /// <summary>
    /// Carga una foto desde Resources (archivo .bytes), recorta un cuadrado
    /// alrededor de la cara y le aplica una mascara circular suave.
    /// </summary>
    public static Sprite PortraitSprite(string resourceName, float centerYNormalized, float zoom,
        float centerXNormalized = 0.5f)
    {
        var asset = Resources.Load<TextAsset>(resourceName);
        if (asset == null)
        {
            Debug.LogWarning("[Flappy] No se encontro Resources/" + resourceName + ".bytes");
            return Circle(64, new Color(0.55f, 0.55f, 0.55f, 1f));
        }

        var src = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        if (!src.LoadImage(asset.bytes))
        {
            Debug.LogWarning("[Flappy] No se pudo leer la imagen " + resourceName);
            Object.Destroy(src);
            return Circle(64, new Color(0.55f, 0.55f, 0.55f, 1f));
        }

        const int outputSize = 512;
        int side = Mathf.RoundToInt(Mathf.Min(src.width, src.height) * Mathf.Clamp(zoom, 0.2f, 1f));
        float centerX = src.width * Mathf.Clamp01(centerXNormalized);
        float centerY = src.height * Mathf.Clamp01(centerYNormalized);

        var tex = NewTex(outputSize, outputSize);
        var px = new Color[outputSize * outputSize];
        float radius = outputSize * 0.5f - 1f;
        float c = (outputSize - 1) * 0.5f;

        for (int y = 0; y < outputSize; y++)
        {
            float v = (y + 0.5f) / outputSize;
            for (int x = 0; x < outputSize; x++)
            {
                float u = (x + 0.5f) / outputSize;
                float sx = (centerX + (u - 0.5f) * side) / src.width;
                float sy = (centerY + (v - 0.5f) * side) / src.height;
                Color col = src.GetPixelBilinear(Mathf.Clamp01(sx), Mathf.Clamp01(sy));

                float dx = x - c;
                float dy = y - c;
                float dist = Mathf.Sqrt(dx * dx + dy * dy);
                col.a *= Mathf.Clamp01(radius - dist + 0.75f);
                px[y * outputSize + x] = col;
            }
        }

        tex.SetPixels(px);
        tex.Apply();
        Object.Destroy(src);

        var sprite = MakeSprite(tex, outputSize);
        sprite.name = "Portrait_" + resourceName;
        return sprite;
    }

    // ------------------------------------------------------------------
    // Generadores de texturas
    // ------------------------------------------------------------------

    static Texture2D NewTex(int w, int h)
    {
        var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.filterMode = FilterMode.Bilinear;
        tex.hideFlags = HideFlags.DontSave;
        return tex;
    }

    static Sprite MakeSprite(Texture2D tex, float ppu)
    {
        return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f),
            ppu, 0, SpriteMeshType.FullRect);
    }

    public static Sprite Circle(int size, Color color)
    {
        var tex = NewTex(size, size);
        var px = new Color[size * size];
        float c = (size - 1) * 0.5f;
        float rad = size * 0.5f - 1f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = x - c;
                float dy = y - c;
                float d = Mathf.Sqrt(dx * dx + dy * dy);
                Color col = color;
                col.a *= Mathf.Clamp01(rad - d + 0.5f);
                px[y * size + x] = col;
            }
        }

        tex.SetPixels(px);
        tex.Apply();
        return MakeSprite(tex, size);
    }

    public static Sprite SoftCircle(int size, Color color)
    {
        var tex = NewTex(size, size);
        var px = new Color[size * size];
        float c = (size - 1) * 0.5f;
        float rad = size * 0.5f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = (x - c) / rad;
                float dy = (y - c) / rad;
                float d = Mathf.Clamp01(Mathf.Sqrt(dx * dx + dy * dy));
                float a = 1f - d;
                a = a * a * (3f - 2f * a);
                Color col = color;
                col.a *= a;
                px[y * size + x] = col;
            }
        }

        tex.SetPixels(px);
        tex.Apply();
        return MakeSprite(tex, size);
    }

    static Sprite Gradient(Color top, Color bottom)
    {
        const int w = 8;
        const int h = 256;
        var tex = NewTex(w, h);
        var px = new Color[w * h];

        for (int y = 0; y < h; y++)
        {
            float t = y / (float)(h - 1);
            Color col = Color.Lerp(bottom, top, t);
            for (int x = 0; x < w; x++) px[y * w + x] = col;
        }

        tex.SetPixels(px);
        tex.Apply();
        var sprite = MakeSprite(tex, h);
        sprite.name = "SkyGradient";
        return sprite;
    }

    static Sprite BuildPipeBody()
    {
        const int w = 64;
        const int h = 64;
        var tex = NewTex(w, h);
        var px = new Color[w * h];

        Color dark = new Color(0.10f, 0.30f, 0.12f, 1f);
        Color body = PipeGreen;
        Color light = new Color(0.60f, 0.88f, 0.48f, 1f);
        Color shadow = new Color(0.20f, 0.50f, 0.22f, 1f);

        for (int x = 0; x < w; x++)
        {
            Color col;
            if (x < 3 || x >= w - 3) col = dark;
            else if (x < 7) col = Color.Lerp(dark, shadow, (x - 3) / 4f);
            else if (x < 16) col = Color.Lerp(shadow, body, (x - 7) / 9f);
            else if (x < 23) col = light;
            else if (x < 32) col = Color.Lerp(light, body, (x - 23) / 9f);
            else if (x < w - 14) col = body;
            else col = Color.Lerp(body, dark, (x - (w - 14)) / 11f);

            for (int y = 0; y < h; y++)
            {
                float shade = 0.98f + 0.02f * Mathf.Sin(y * 0.35f + x * 0.15f);
                Color final = col;
                final.r *= shade;
                final.g *= shade;
                final.b *= shade;
                px[y * w + x] = final;
            }
        }

        tex.SetPixels(px);
        tex.Apply();
        var sprite = MakeSprite(tex, h);
        sprite.name = "PipeBody";
        return sprite;
    }

    static Sprite BuildPipeCap()
    {
        const int w = 64;
        const int h = 40;
        var tex = NewTex(w, h);
        var px = new Color[w * h];

        Color dark = new Color(0.10f, 0.30f, 0.12f, 1f);
        Color body = PipeGreen;
        Color light = new Color(0.60f, 0.88f, 0.48f, 1f);
        Color shadow = new Color(0.20f, 0.50f, 0.22f, 1f);

        for (int y = 0; y < h; y++)
        {
            Color row;
            if (y < 4 || y >= h - 4) row = dark;
            else if (y < 9) row = light;
            else if (y < 14) row = Color.Lerp(light, body, (y - 9) / 5f);
            else if (y < h - 9) row = body;
            else row = Color.Lerp(body, shadow, (y - (h - 9)) / 6f);

            for (int x = 0; x < w; x++)
            {
                Color col = row;
                if (x < 4 || x >= w - 4) col = dark;
                else if (x < 9) col = Color.Lerp(dark, shadow, (x - 4) / 5f);
                else if (x < 20) col = Color.Lerp(shadow, body, (x - 9) / 11f);
                else if (x < 26) col = light;
                else if (x < 34) col = Color.Lerp(light, body, (x - 26) / 8f);
                else if (x >= w - 12) col = Color.Lerp(body, dark, (x - (w - 12)) / 8f);
                px[y * w + x] = col;
            }
        }

        tex.SetPixels(px);
        tex.Apply();
        var sprite = MakeSprite(tex, h);
        sprite.name = "PipeCap";
        return sprite;
    }

    static Sprite Vignette(int size)
    {
        var tex = NewTex(size, size);
        var px = new Color[size * size];
        float c = (size - 1) * 0.5f;
        float rad = size * 0.5f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = (x - c) / rad;
                float dy = (y - c) / rad;
                float d = Mathf.Sqrt(dx * dx + dy * dy);
                float a = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.35f, 0.78f, d));
                px[y * size + x] = new Color(1f, 1f, 1f, a);
            }
        }

        tex.SetPixels(px);
        tex.Apply();
        return MakeSprite(tex, 100f);
    }

    // ------------------------------------------------------------------
    // Paneles de UI (9-slice)
    // ------------------------------------------------------------------

    public static Sprite RoundedPanel(int size, int radius, Color fill, Color border, int borderWidth)
    {
        var tex = NewTex(size, size);
        var px = new Color[size * size];
        float c = (size - 1) * 0.5f;
        float half = size * 0.5f;
        float inner = half - radius;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float px0 = Mathf.Abs(x - c) - inner;
                float py0 = Mathf.Abs(y - c) - inner;
                float qx = Mathf.Max(px0, 0f);
                float qy = Mathf.Max(py0, 0f);
                float dist = Mathf.Sqrt(qx * qx + qy * qy) + Mathf.Min(Mathf.Max(px0, py0), 0f) - radius;
                float alpha = Mathf.Clamp01(0.5f - dist);

                Color col = fill;
                col.a *= alpha;

                if (borderWidth > 0 && dist > -borderWidth)
                {
                    Color b = border;
                    b.a *= alpha;
                    col = b;
                }

                px[y * size + x] = col;
            }
        }

        tex.SetPixels(px);
        tex.Apply();

        float b2 = radius + 2f;
        var sprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f),
            100f, 0, SpriteMeshType.FullRect, new Vector4(b2, b2, b2, b2));
        sprite.name = "RoundedPanel";
        return sprite;
    }
}

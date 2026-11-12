using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Interfaz del juego: menu, HUD, game over y pausa. Todo se construye por codigo.
/// </summary>
public class GameUI : MonoBehaviour
{
    static readonly Color White = new Color(1f, 1f, 1f, 1f);
    static readonly Color Gold = new Color(1f, 0.84f, 0.32f, 1f);
    static readonly Color Red = new Color(1f, 0.36f, 0.30f, 1f);

    GameObject hudRoot;
    GameObject menuRoot;
    GameObject overRoot;
    GameObject pauseRoot;

    Text scoreText;
    Text hudRecordText;
    Text warningText;
    Text menuRecordText;
    Text menuHintText;
    Text overCauseText;
    Text overScoreText;
    Text overRecordText;
    Text overNewRecordText;
    RectTransform overPanel;

    Image flashImage;
    Image vignetteImage;

    Font font;
    Sprite panelSprite;
    Sprite pillSprite;

    Color flashColor = new Color(1f, 1f, 1f, 0f);
    float flashAlpha;
    float popScore;
    float popPanel;
    float danger;
    float dangerTarget;
    float pulseTime;
    float warnTimer;

    public static GameUI Create()
    {
        var go = new GameObject("FlappyUI", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
        var canvas = go.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;

        var scaler = go.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        var ui = go.AddComponent<GameUI>();
        ui.Build();
        return ui;
    }

    void Build()
    {
        font = LoadFont();
        panelSprite = GameView.RoundedPanel(96, 26,
            new Color(0.05f, 0.10f, 0.12f, 0.96f), new Color(1f, 1f, 1f, 0.18f), 2);
        pillSprite = GameView.RoundedPanel(96, 40,
            new Color(0.04f, 0.09f, 0.11f, 0.55f), new Color(1f, 0.84f, 0.32f, 0.4f), 2);

        var canvas = (RectTransform)transform;

        BuildHud(canvas);
        BuildMenu(canvas);
        BuildGameOver(canvas);
        BuildPause(canvas);

        // Overlays por encima de todo.
        vignetteImage = FullScreenImage(canvas, "Vignette", GameView.VignetteSprite, new Color(1f, 0.12f, 0.08f, 0f));
        flashImage = FullScreenImage(canvas, "Flash", null, new Color(1f, 1f, 1f, 0f));
    }

    // ------------------------------------------------------------------
    // Construccion
    // ------------------------------------------------------------------

    void BuildHud(RectTransform canvas)
    {
        hudRoot = NewRoot(canvas, "HUD");

        scoreText = MakeText(hudRoot.transform, "Score", "0", 104, White,
            new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -48f),
            new Vector2(900f, 140f), TextAnchor.MiddleCenter, true);

        hudRecordText = MakeText(hudRoot.transform, "Record", "RÉCORD 0", 28,
            new Color(1f, 1f, 1f, 0.82f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
            new Vector2(0f, -186f), new Vector2(700f, 44f), TextAnchor.MiddleCenter, false);

        warningText = MakeText(hudRoot.transform, "Warning", "", 40, Red,
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 248f),
            new Vector2(1400f, 64f), TextAnchor.MiddleCenter, true);
        SetAlpha(warningText, 0f);

        MakeText(hudRoot.transform, "PauseHint", "ESC · pausa", 22,
            new Color(1f, 1f, 1f, 0.55f), new Vector2(0f, 0f), new Vector2(0f, 0f),
            new Vector2(26f, 22f), new Vector2(300f, 34f), TextAnchor.LowerLeft, false);
    }

    void BuildMenu(RectTransform canvas)
    {
        menuRoot = NewRoot(canvas, "Menu");

        MakeText(menuRoot.transform, "TitleSmall", "FLAPPY", 64, Gold,
            new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -62f),
            new Vector2(900f, 84f), TextAnchor.MiddleCenter, true);

        MakeText(menuRoot.transform, "TitleBig", "PASTRANA", 126, White,
            new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -148f),
            new Vector2(1200f, 160f), TextAnchor.MiddleCenter, true);

        MakeText(menuRoot.transform, "Subtitle", "esquivá los tubos · escapá del boss", 32,
            new Color(1f, 1f, 1f, 0.92f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
            new Vector2(0f, -300f), new Vector2(1000f, 48f), TextAnchor.MiddleCenter, false);

        var pill = MakePanel(menuRoot.transform, "RecordPill", pillSprite,
            new Color(1f, 1f, 1f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
            new Vector2(0f, -366f), new Vector2(400f, 78f));
        menuRecordText = MakeText(pill.transform, "RecordText", "RÉCORD 0", 34, Gold,
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 2f),
            new Vector2(380f, 60f), TextAnchor.MiddleCenter, false);

        menuHintText = MakeText(menuRoot.transform, "StartHint", "ESPACIO / CLIC para empezar", 46, White,
            new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 258f),
            new Vector2(1100f, 64f), TextAnchor.MiddleCenter, true);

        MakeText(menuRoot.transform, "Controls", "↑  ·  ESPACIO  ·  CLIC  ·  TOQUE", 26,
            new Color(1f, 1f, 1f, 0.78f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
            new Vector2(0f, 200f), new Vector2(1000f, 40f), TextAnchor.MiddleCenter, false);

        MakeText(menuRoot.transform, "BossHint", "cada tubo que pases empuja al boss hacia atrás", 24,
            new Color(1f, 1f, 1f, 0.66f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
            new Vector2(0f, 158f), new Vector2(1200f, 36f), TextAnchor.MiddleCenter, false);
    }

    void BuildGameOver(RectTransform canvas)
    {
        overRoot = NewRoot(canvas, "GameOver");
        FullScreenImage((RectTransform)overRoot.transform, "Dim", null, new Color(0f, 0f, 0f, 0.45f));

        MakePanel(overRoot.transform, "Shadow", panelSprite, new Color(0f, 0f, 0f, 0.45f),
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -16f),
            new Vector2(792f, 548f));

        var panel = MakePanel(overRoot.transform, "Panel", panelSprite, Color.white,
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero,
            new Vector2(780f, 536f));
        overPanel = (RectTransform)panel.transform;

        MakeText(overPanel, "Title", "GAME OVER", 72, Gold,
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 188f),
            new Vector2(700f, 90f), TextAnchor.MiddleCenter, true);

        overCauseText = MakeText(overPanel, "Cause", "CHOCASTE", 32, Red,
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 124f),
            new Vector2(700f, 48f), TextAnchor.MiddleCenter, false);

        MakeText(overPanel, "ScoreLabel", "PUNTAJE", 24, new Color(1f, 1f, 1f, 0.62f),
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 58f),
            new Vector2(600f, 36f), TextAnchor.MiddleCenter, false);

        overScoreText = MakeText(overPanel, "Score", "0", 100, White,
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -8f),
            new Vector2(600f, 120f), TextAnchor.MiddleCenter, true);

        overRecordText = MakeText(overPanel, "Record", "RÉCORD 0", 32, Gold,
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -102f),
            new Vector2(600f, 48f), TextAnchor.MiddleCenter, false);

        overNewRecordText = MakeText(overPanel, "NewRecord", "¡NUEVO RÉCORD!", 38, White,
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -156f),
            new Vector2(600f, 48f), TextAnchor.MiddleCenter, true);
        overNewRecordText.gameObject.SetActive(false);

        MakeText(overPanel, "Hint", "ESPACIO / CLIC: reintentar   ·   M: menú", 25,
            new Color(1f, 1f, 1f, 0.72f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(0f, -222f), new Vector2(720f, 40f), TextAnchor.MiddleCenter, false);

        overRoot.SetActive(false);
    }

    void BuildPause(RectTransform canvas)
    {
        pauseRoot = NewRoot(canvas, "Pause");
        FullScreenImage((RectTransform)pauseRoot.transform, "Dim", null, new Color(0f, 0f, 0f, 0.55f));

        MakeText(pauseRoot.transform, "Title", "PAUSA", 92, Gold,
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 40f),
            new Vector2(800f, 120f), TextAnchor.MiddleCenter, true);

        MakeText(pauseRoot.transform, "Hint", "ESC para continuar", 30, White,
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -50f),
            new Vector2(800f, 50f), TextAnchor.MiddleCenter, false);

        pauseRoot.SetActive(false);
    }

    // ------------------------------------------------------------------
    // API
    // ------------------------------------------------------------------

    public void ShowMenu(int record)
    {
        stateRoots(menu: true, hud: false, over: false, pause: false);
        menuRecordText.text = "RÉCORD " + record;
        scoreText.text = "0";
        scoreText.rectTransform.localScale = Vector3.one;
        hudRecordText.text = "RÉCORD " + record;
        danger = 0f;
        dangerTarget = 0f;
        vignetteImage.color = new Color(1f, 0.12f, 0.08f, 0f);
        warnTimer = 0f;
        SetAlpha(warningText, 0f);
    }

    public void ShowHud(int score, int record)
    {
        stateRoots(menu: false, hud: true, over: false, pause: false);
        SetScore(score);
        SetBest(record);
        warnTimer = 0f;
        SetAlpha(warningText, 0f);
    }

    public void SetScore(int score)
    {
        scoreText.text = score.ToString();
        popScore = 1f;
    }

    public void SetBest(int best)
    {
        hudRecordText.text = "RÉCORD " + best;
    }

    public void ShowGameOver(int score, int best, bool newRecord, bool caught)
    {
        stateRoots(menu: false, hud: false, over: true, pause: false);
        overCauseText.text = caught ? "TE ATRAPÓ EL BOSS" : "CHOCASTE";
        overScoreText.text = score.ToString();
        overRecordText.text = "RÉCORD " + best;
        overNewRecordText.gameObject.SetActive(newRecord);
        overPanel.localScale = Vector3.one * 0.72f;
        popPanel = 0f;
        danger = 0f;
        dangerTarget = 0f;
        warnTimer = 0f;
        SetAlpha(warningText, 0f);
    }

    public void SetPaused(bool paused)
    {
        pauseRoot.SetActive(paused);
    }

    public void SetDanger(float value)
    {
        dangerTarget = Mathf.Clamp01(value);
    }

    public void Warn(string message)
    {
        warningText.text = message;
        warnTimer = 2.4f;
    }

    public void Flash(Color color)
    {
        flashColor = color;
        flashAlpha = color.a;
        flashImage.color = new Color(color.r, color.g, color.b, flashAlpha);
    }

    public void Tick(float dt)
    {
        if (flashAlpha > 0f)
        {
            flashAlpha = Mathf.Max(0f, flashAlpha - dt * 2.8f);
            flashImage.color = new Color(flashColor.r, flashColor.g, flashColor.b, flashAlpha);
        }

        if (popScore > 0f)
        {
            popScore = Mathf.Max(0f, popScore - dt * 4f);
            float k = 1f + 0.28f * popScore * popScore;
            scoreText.rectTransform.localScale = new Vector3(k, k, 1f);
        }

        danger = Mathf.MoveTowards(danger, dangerTarget, dt * 3f);
        var vc = vignetteImage.color;
        vc.a = 0.52f * danger * danger;
        vignetteImage.color = vc;

        pulseTime += dt;

        if (warnTimer > 0f)
        {
            warnTimer = Mathf.Max(0f, warnTimer - dt);
            float alpha = Mathf.Clamp01(Mathf.Min((2.4f - warnTimer) * 5f, warnTimer * 3f));
            float k = 1f + 0.07f * Mathf.Sin(pulseTime * 10f);
            warningText.rectTransform.localScale = new Vector3(k, k, 1f);
            SetAlpha(warningText, alpha);
        }
        else
        {
            SetAlpha(warningText, 0f);
        }

        if (menuRoot.activeSelf)
        {
            SetAlpha(menuHintText, 0.5f + 0.5f * Mathf.Abs(Mathf.Sin(pulseTime * 2.2f)));
        }

        if (overRoot.activeSelf)
        {
            popPanel = Mathf.Min(1f, popPanel + dt * 5f);
            float e = EaseOutBack(popPanel);
            overPanel.localScale = Vector3.one * Mathf.LerpUnclamped(0.72f, 1f, e);

            if (overNewRecordText.gameObject.activeSelf)
            {
                float k = 1f + 0.04f * Mathf.Sin(pulseTime * 5f);
                overNewRecordText.rectTransform.localScale = new Vector3(k, k, 1f);
                SetAlpha(overNewRecordText, 0.75f + 0.25f * Mathf.Abs(Mathf.Sin(pulseTime * 4f)));
            }
        }
    }

    void stateRoots(bool menu, bool hud, bool over, bool pause)
    {
        menuRoot.SetActive(menu);
        hudRoot.SetActive(hud);
        overRoot.SetActive(over);
        pauseRoot.SetActive(pause);
    }

    // ------------------------------------------------------------------
    // Helpers
    // ------------------------------------------------------------------

    GameObject NewRoot(RectTransform canvas, string name)
    {
        var rt = NewRect(canvas, name);
        Stretch(rt);
        return rt.gameObject;
    }

    static RectTransform NewRect(Transform parent, string name)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return (RectTransform)go.transform;
    }

    static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    static Image FullScreenImage(RectTransform parent, string name, Sprite sprite, Color color)
    {
        var rt = NewRect(parent, name);
        Stretch(rt);
        var img = rt.gameObject.AddComponent<Image>();
        img.sprite = sprite;
        img.color = color;
        img.raycastTarget = false;
        return img;
    }

    static Image MakePanel(Transform parent, string name, Sprite sprite, Color color,
        Vector2 anchor, Vector2 pivot, Vector2 pos, Vector2 size)
    {
        var rt = NewRect(parent, name);
        rt.anchorMin = anchor;
        rt.anchorMax = anchor;
        rt.pivot = pivot;
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;

        var img = rt.gameObject.AddComponent<Image>();
        img.sprite = sprite;
        img.color = color;
        img.raycastTarget = false;
        if (sprite != null && sprite.border.sqrMagnitude > 0.001f) img.type = Image.Type.Sliced;
        return img;
    }

    Text MakeText(Transform parent, string name, string content, int size, Color color,
        Vector2 anchor, Vector2 pivot, Vector2 pos, Vector2 sizeDelta, TextAnchor alignment,
        bool outline)
    {
        var rt = NewRect(parent, name);
        rt.anchorMin = anchor;
        rt.anchorMax = anchor;
        rt.pivot = pivot;
        rt.anchoredPosition = pos;
        rt.sizeDelta = sizeDelta;

        var text = rt.gameObject.AddComponent<Text>();
        text.font = font;
        text.text = content;
        text.fontSize = size;
        text.fontStyle = FontStyle.Bold;
        text.color = color;
        text.alignment = alignment;
        text.horizontalOverflow = HorizontalWrapMode.Overflow;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        text.raycastTarget = false;
        text.supportRichText = true;

        var shadow = rt.gameObject.AddComponent<Shadow>();
        shadow.effectColor = new Color(0f, 0f, 0f, 0.45f);
        shadow.effectDistance = new Vector2(0f, -4f);

        if (outline)
        {
            var outlineEffect = rt.gameObject.AddComponent<Outline>();
            outlineEffect.effectColor = new Color(0.04f, 0.09f, 0.11f, 0.95f);
            outlineEffect.effectDistance = new Vector2(3.5f, -3.5f);
        }

        return text;
    }

    static void SetAlpha(Text text, float alpha)
    {
        if (text == null) return;
        Color c = text.color;
        c.a = Mathf.Clamp01(alpha);
        text.color = c;
    }

    static float EaseOutBack(float t)
    {
        const float c1 = 1.70158f;
        const float c3 = c1 + 1f;
        float x = t - 1f;
        return 1f + c3 * x * x * x + c1 * x * x;
    }

    static Font LoadFont()
    {
        Font f = null;
        try { f = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); }
        catch { }

        if (f == null)
        {
            try { f = Resources.GetBuiltinResource<Font>("Arial.ttf"); }
            catch { }
        }

        if (f == null)
        {
            f = Font.CreateDynamicFontFromOSFont(new[] { "Arial", "Segoe UI", "Roboto", "Helvetica" }, 48);
        }

        return f;
    }
}

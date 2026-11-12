using System.Collections.Generic;
using UnityEngine;

public enum GameState
{
    Menu,
    Playing,
    Dying,
    GameOver
}

/// <summary>
/// Núcleo del Flappy Pastrana. Construye el mundo, maneja estados, dificultad,
/// tubos, boss perseguidor, puntaje y récord.
/// </summary>
public class FlappyGame : MonoBehaviour
{
    public const float CamSize = 5.4f;
    public const float PlayerRadius = 0.52f;

    const float PipeHalfWidth = 0.9f;
    const float Gravity = -23f;
    const float FlapVelocity = 8f;
    const float GapStart = 3.55f;
    const float GapMin = 2.7f;
    const float PipeSpacing = 5.3f;
    const float BaseSpeed = 3.1f;
    const string RecordKey = "FlappyPastrana.Record";

    Camera cam;
    Transform worldRoot;
    Transform playerRoot;
    SpriteRenderer playerFace;
    Transform bossRoot;
    SpriteRenderer bossGlow;
    SpriteRenderer bossRing;
    SpriteRenderer bossFace;
    SimpleParticles particles;
    GameUI ui;
    Sfx sfx;

    readonly List<PipePair> pipes = new List<PipePair>();
    readonly List<Transform> clouds = new List<Transform>();
    readonly List<float> cloudParallax = new List<float>();
    readonly List<Transform> groundMarks = new List<Transform>();

    GameState state = GameState.Menu;
    bool paused;
    float stateTime;
    float menuTime;
    float runTime;

    int score;
    int record;
    bool caughtByBoss;
    bool newRecord;

    float camHalfW;
    float groundTop;
    float playerX;
    Vector3 menuBossPos;

    float playerY;
    float velocity;
    float tilt;
    float flapKick;

    float speed = BaseSpeed;
    float gapSize = GapStart;
    float spawnTimer;
    float lastGapCenter;

    bool bossActive;
    float bossX;
    float bossY;
    float bossVelY;
    float bossAppear;

    float shake;
    float trailTimer;

    void Awake()
    {
        Application.targetFrameRate = 60;

        record = PlayerPrefs.GetInt(RecordKey, 0);

        SetupCamera();
        BuildWorld();

        sfx = Sfx.Create(gameObject);
        particles = SimpleParticles.Create(worldRoot, GameView.SoftCircleSprite);
        ui = GameUI.Create();

        EnterMenu();
    }

    // ------------------------------------------------------------------
    // Setup
    // ------------------------------------------------------------------

    void SetupCamera()
    {
        cam = Camera.main;
        if (cam == null) cam = Object.FindAnyObjectByType<Camera>();

        if (cam == null)
        {
            var go = new GameObject("Main Camera");
            go.tag = "MainCamera";
            cam = go.AddComponent<Camera>();
            go.AddComponent<AudioListener>();
        }

        cam.orthographic = true;
        cam.orthographicSize = CamSize;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = GameView.SkyBottom;
        cam.nearClipPlane = 0.1f;
        cam.farClipPlane = 100f;
        cam.transform.position = new Vector3(0f, 0f, -10f);
        cam.transform.rotation = Quaternion.identity;

        camHalfW = CamSize * Mathf.Max(0.5f, cam.aspect);
        groundTop = -CamSize + GameView.GroundHeight;
        playerX = -camHalfW + Mathf.Clamp(camHalfW * 0.5f, 2.3f, 4.8f);
    }

    void BuildWorld()
    {
        worldRoot = new GameObject("World").transform;
        worldRoot.SetParent(transform, false);

        BuildBackground();
        BuildGround();
        BuildClouds();
        BuildPlayer();
        BuildBoss();
    }

    void BuildBackground()
    {
        float w = camHalfW * 2f + 5f;
        float h = CamSize * 2f + 4f;

        GameView.AddSprite(worldRoot, "Sky", GameView.SkyGradient, -100, Color.white,
            Vector3.zero, w, h);

        Vector3 sunPos = new Vector3(-camHalfW * 0.55f, CamSize * 0.55f, 0f);
        GameView.AddSprite(worldRoot, "SunGlow", GameView.SoftCircleSprite, -99,
            new Color(1f, 0.95f, 0.72f, 0.5f), sunPos, 7.5f, 7.5f);
        GameView.AddSprite(worldRoot, "Sun", GameView.CircleSprite, -98,
            new Color(1f, 0.98f, 0.84f, 0.95f), sunPos, 2.3f, 2.3f);

        GameView.AddSprite(worldRoot, "HillsFar", GameView.SoftCircleSprite, -96,
            new Color(0.62f, 0.85f, 0.71f, 0.55f),
            new Vector3(0f, groundTop + 2.7f, 0f), camHalfW * 3f + 5f, 7.5f);

        for (int i = 0; i < 5; i++)
        {
            float x = -camHalfW - 1.5f + i * (camHalfW * 2f + 3f) / 4f;
            float size = 5.5f + (i % 3) * 1.6f;
            GameView.AddSprite(worldRoot, "Hill", GameView.SoftCircleSprite, -93,
                new Color(0.46f, 0.76f, 0.53f, 0.55f),
                new Vector3(x, groundTop + 0.9f, 0f), size, size * 0.62f);
        }

        for (int i = 0; i < 4; i++)
        {
            float x = -camHalfW + 1.2f + i * (camHalfW * 2f + 2f) / 3f;
            GameView.AddSprite(worldRoot, "Bush", GameView.SoftCircleSprite, -91,
                new Color(0.36f, 0.66f, 0.45f, 0.5f),
                new Vector3(x, groundTop + 0.25f, 0f), 4.2f, 2.4f);
        }
    }

    void BuildGround()
    {
        float w = camHalfW * 2f + 5f;

        GameView.AddSprite(worldRoot, "GroundBase", GameView.White, 18,
            GameView.Dirt, new Vector3(0f, groundTop - GameView.GroundHeight * 0.5f, 0f),
            w, GameView.GroundHeight);

        GameView.AddSprite(worldRoot, "GroundShade", GameView.White, 19,
            new Color(0.77f, 0.64f, 0.40f, 1f),
            new Vector3(0f, groundTop - GameView.GroundHeight * 0.82f, 0f), w, 0.24f);

        GameView.AddSprite(worldRoot, "Grass", GameView.White, 23,
            new Color(0.47f, 0.77f, 0.35f, 1f),
            new Vector3(0f, groundTop - 0.11f, 0f), w, 0.22f);

        GameView.AddSprite(worldRoot, "GrassLine", GameView.White, 22,
            new Color(0.33f, 0.60f, 0.26f, 1f),
            new Vector3(0f, groundTop - 0.255f, 0f), w, 0.07f);

        for (float x = -camHalfW - 2f; x < camHalfW + 2.5f; x += 0.78f)
        {
            float markHeight = Random.Range(0.25f, 0.7f);
            var mark = GameView.AddSprite(worldRoot, "GroundMark", GameView.White, 20,
                new Color(0.76f, 0.64f, 0.40f, 0.9f),
                new Vector3(x, groundTop - 0.45f - Random.Range(0f, 0.45f), 0f),
                0.16f, markHeight);
            groundMarks.Add(mark.transform);
        }
    }

    void BuildClouds()
    {
        for (int i = 0; i < 11; i++)
        {
            var cloud = new GameObject("Cloud").transform;
            cloud.SetParent(worldRoot, false);

            float x = Random.Range(-camHalfW - 2f, camHalfW + 2f);
            float y = Random.Range(groundTop + 1.1f, CamSize - 0.3f);
            cloud.localPosition = new Vector3(x, y, 0f);

            float size = Random.Range(1.8f, 3.6f);
            GameView.AddSprite(cloud, "A", GameView.SoftCircleSprite, -80,
                new Color(1f, 1f, 1f, 0.45f), Vector3.zero, size, size * 0.42f);
            GameView.AddSprite(cloud, "B", GameView.SoftCircleSprite, -80,
                new Color(1f, 1f, 1f, 0.35f), new Vector3(size * 0.36f, size * 0.08f, 0f),
                size * 0.62f, size * 0.3f);

            clouds.Add(cloud);
            cloudParallax.Add(Random.Range(0.5f, 1.4f));
        }
    }

    void BuildPlayer()
    {
        playerRoot = new GameObject("Player").transform;
        playerRoot.SetParent(worldRoot, false);

        var portrait = GameView.PortraitSprite("pastrana", 0.49f, 0.88f);

        GameView.AddSprite(playerRoot, "Glow", GameView.SoftCircleSprite, 7,
            new Color(1f, 1f, 1f, 0.16f), Vector3.zero, 2f, 2f);
        GameView.AddSprite(playerRoot, "Ring", GameView.CircleSprite, 8,
            new Color(1f, 1f, 1f, 0.98f), Vector3.zero, 1.16f, 1.16f);
        playerFace = GameView.AddSprite(playerRoot, "Face", portrait, 9,
            Color.white, Vector3.zero, 1.02f, 1.02f);

        playerRoot.localPosition = new Vector3(playerX, 1.35f, 0f);
    }

    void BuildBoss()
    {
        bossRoot = new GameObject("Boss").transform;
        bossRoot.SetParent(worldRoot, false);

        var portrait = GameView.PortraitSprite("boss", 0.65f, 0.868f, 0.33f);

        bossGlow = GameView.AddSprite(bossRoot, "Glow", GameView.SoftCircleSprite, 3,
            new Color(1f, 0.22f, 0.18f, 0.16f), Vector3.zero, 3f, 3f);
        bossRing = GameView.AddSprite(bossRoot, "Ring", GameView.CircleSprite, 4,
            new Color(0.34f, 0.05f, 0.07f, 1f), Vector3.zero, 2f, 2f);
        bossFace = GameView.AddSprite(bossRoot, "Face", portrait, 6,
            Color.white, Vector3.zero, 1.72f, 1.72f);

        bossRoot.localPosition = new Vector3(-20f, 0f, 0f);
    }

    // ------------------------------------------------------------------
    // Loop principal
    // ------------------------------------------------------------------

    void Update()
    {
        float dt = Mathf.Min(Time.deltaTime, 0.033f);
        if (dt <= 0f) return;

        stateTime += dt;

        if (state == GameState.Playing && InputHelper.PausePressed())
        {
            paused = !paused;
            ui.SetPaused(paused);
        }

        if (!paused)
        {
            switch (state)
            {
                case GameState.Menu:
                    TickMenu(dt);
                    break;
                case GameState.Playing:
                    TickPlaying(dt);
                    break;
                case GameState.Dying:
                    TickDying(dt);
                    break;
                case GameState.GameOver:
                    TickGameOver(dt);
                    break;
            }

            float drift = state == GameState.Playing ? speed
                : state == GameState.Menu ? 0.6f
                : state == GameState.GameOver ? 0.3f
                : 0f;

            MoveClouds(dt, drift);
            MoveGroundMarks(dt, drift);
        }

        UpdateShake(dt);
        ui.Tick(dt);
    }

    void TickMenu(float dt)
    {
        menuTime += dt;

        float bob = Mathf.Sin(menuTime * 1.9f);
        playerY = 1.35f + bob * 0.25f;
        tilt = bob * 6f;
        flapKick = Mathf.MoveTowards(flapKick, 0f, dt * 3f);
        playerRoot.localPosition = new Vector3(playerX, playerY, 0f);
        playerRoot.localRotation = Quaternion.Euler(0f, 0f, tilt);
        float breathe = 1f + 0.02f * Mathf.Sin(menuTime * 2.3f);
        playerRoot.localScale = new Vector3(breathe, breathe, 1f);

        float bossBob = Mathf.Sin(menuTime * 1.5f + 0.7f);
        bossRoot.localPosition = new Vector3(menuBossPos.x, menuBossPos.y + bossBob * 0.28f, 0f);
        bossRoot.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(menuTime * 1.2f) * 4f);
        float bossScale = 1f + Mathf.Sin(menuTime * 2.4f) * 0.025f;
        bossRoot.localScale = new Vector3(bossScale, bossScale, 1f);
        bossGlow.color = new Color(1f, 0.22f, 0.18f, 0.13f + 0.05f * Mathf.Sin(menuTime * 2.4f));

        if (InputHelper.Press()) StartRun();
    }

    void TickPlaying(float dt)
    {
        runTime += dt;

        speed = BaseSpeed + Mathf.Min(score * 0.085f, 3.4f);
        gapSize = Mathf.Max(GapMin, GapStart - score * 0.035f);

        if (InputHelper.Press()) Flap();

        velocity += Gravity * dt;
        playerY += velocity * dt;

        if (playerY > CamSize - 0.55f)
        {
            playerY = CamSize - 0.55f;
            if (velocity > 0f) velocity = 0f;
        }

        tilt = Mathf.Lerp(tilt, Mathf.Clamp(velocity * 5f, -75f, 26f),
            1f - Mathf.Exp(-11f * dt));
        flapKick = Mathf.MoveTowards(flapKick, 0f, dt * 3.4f);
        ApplyPlayerTransform();

        if (playerY - PlayerRadius <= groundTop)
        {
            playerY = groundTop + PlayerRadius;
            ApplyPlayerTransform();
            Die(false);
            return;
        }

        TickPipes(dt);
        if (state != GameState.Playing) return;

        spawnTimer -= dt;
        if (spawnTimer <= 0f)
        {
            SpawnPipeAt(camHalfW + 2.2f);
            spawnTimer += PipeSpacing / Mathf.Max(0.1f, speed);
        }

        trailTimer -= dt;
        if (trailTimer <= 0f)
        {
            trailTimer = 0.085f;
            particles.Puff(new Vector3(playerX - 0.42f, playerY - 0.18f, 0f),
                new Color(1f, 1f, 1f, 0.2f), 0.42f, 0.5f);
        }

        TickBoss(dt);

        float danger = bossActive ? Mathf.Clamp01(1f - (playerX - bossX) / 6f) : 0f;
        ui.SetDanger(danger);
    }

    void TickPipes(float dt)
    {
        for (int i = pipes.Count - 1; i >= 0; i--)
        {
            var pipe = pipes[i];
            Vector3 p = pipe.transform.localPosition;
            p.x -= speed * dt;
            pipe.transform.localPosition = p;

            if (!pipe.Scored && p.x < playerX - 0.1f)
            {
                pipe.Scored = true;
                OnScore();
            }

            if (p.x < -camHalfW - 3f)
            {
                Destroy(pipe.gameObject);
                pipes.RemoveAt(i);
                continue;
            }

            if (HitPipe(pipe))
            {
                Die(false);
                return;
            }
        }
    }

    void TickBoss(float dt)
    {
        if (!bossActive)
        {
            if (runTime > 2f) ActivateBoss();
            return;
        }

        bossAppear = Mathf.MoveTowards(bossAppear, 1f, dt * 2.5f);

        float bossSpeed = 0.62f + score * 0.055f + Mathf.Min(runTime * 0.012f, 0.6f);
        bossX += bossSpeed * dt;

        bossY = Mathf.SmoothDamp(bossY, playerY + Mathf.Sin(runTime * 2.2f) * 0.16f,
            ref bossVelY, 0.3f, 6f, dt);
        bossY = Mathf.Clamp(bossY, groundTop + 1.0f, CamSize - 1.0f);

        float close = Mathf.Clamp01(1f - (playerX - bossX) / 7f);
        float pulse = 1f + 0.03f * Mathf.Sin(runTime * 8f) + 0.06f * close;

        bossRoot.localPosition = new Vector3(bossX, bossY, 0f);
        bossRoot.localScale = Vector3.one * (Mathf.Lerp(0.6f, 1f, bossAppear) * pulse);
        bossRoot.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(runTime * 5f) * 4f);

        bossGlow.color = new Color(1f, 0.22f, 0.18f, 0.12f + 0.42f * close);
        Color ringColor = new Color(0.34f, 0.05f, 0.07f, 1f);
        bossRing.color = ringColor;

        float dx = playerX - bossX;
        float dy = playerY - bossY;
        if (dx * dx + dy * dy < 1.08f * 1.08f)
        {
            Die(true);
        }
    }

    void TickDying(float dt)
    {
        velocity += Gravity * 1.05f * dt;
        playerY += velocity * dt;

        tilt = Mathf.MoveTowards(tilt, -90f, dt * 300f);
        flapKick = Mathf.MoveTowards(flapKick, 0f, dt * 3f);

        float floor = groundTop + PlayerRadius;
        if (playerY < floor)
        {
            playerY = floor;
            velocity = 0f;
        }

        ApplyPlayerTransform();

        if (caughtByBoss && bossRoot.gameObject.activeSelf)
        {
            bossX = Mathf.MoveTowards(bossX, playerX - 0.55f, dt * 5f);
            bossY = Mathf.MoveTowards(bossY, playerY, dt * 5f);
            bossRoot.localPosition = new Vector3(bossX, bossY, 0f);
        }

        if (stateTime > 0.8f) EndRun();
    }

    void TickGameOver(float dt)
    {
        if (stateTime < 0.35f) return;

        if (InputHelper.Press())
        {
            StartRun();
            return;
        }

        if (InputHelper.MenuPressed()) EnterMenu();
    }

    // ------------------------------------------------------------------
    // Estados
    // ------------------------------------------------------------------

    void EnterMenu()
    {
        state = GameState.Menu;
        stateTime = 0f;
        menuTime = 0f;
        paused = false;
        caughtByBoss = false;
        newRecord = false;

        ClearPipes();

        speed = BaseSpeed;
        gapSize = GapStart;
        velocity = 0f;
        tilt = 0f;
        flapKick = 0f;
        playerY = 1.35f;

        bossActive = false;
        bossAppear = 0f;
        bossRoot.gameObject.SetActive(true);

        float side = camHalfW > 5.5f ? -1f : 1f;
        float bx = playerX + side * 3.4f;
        bx = Mathf.Clamp(bx, -camHalfW + 1.1f, camHalfW - 1.1f);
        menuBossPos = new Vector3(bx, 0.75f, 0f);

        bossRoot.localScale = Vector3.one;
        bossRoot.localPosition = menuBossPos;
        bossGlow.color = new Color(1f, 0.22f, 0.18f, 0.14f);

        playerRoot.localPosition = new Vector3(playerX, playerY, 0f);
        playerRoot.localRotation = Quaternion.identity;
        playerRoot.localScale = Vector3.one;

        ui.SetPaused(false);
        ui.SetDanger(0f);
        ui.ShowMenu(record);
    }

    void StartRun()
    {
        state = GameState.Playing;
        stateTime = 0f;
        runTime = 0f;
        paused = false;
        score = 0;
        newRecord = false;
        caughtByBoss = false;

        speed = BaseSpeed;
        gapSize = GapStart;
        playerY = 0.7f;
        velocity = -1f;
        tilt = 0f;
        flapKick = 0.6f;

        bossActive = false;
        bossAppear = 0f;
        bossRoot.gameObject.SetActive(false);

        ClearPipes();
        lastGapCenter = 0f;

        float spawnX = camHalfW + 2.2f;
        int count = Mathf.FloorToInt((spawnX - (playerX + 5.5f)) / PipeSpacing);
        count = Mathf.Max(0, count);
        for (int i = 0; i <= count; i++) SpawnPipeAt(spawnX - i * PipeSpacing);

        spawnTimer = PipeSpacing / speed;
        trailTimer = 0f;

        ApplyPlayerTransform();
        ui.SetDanger(0f);
        ui.ShowHud(score, record);
        sfx.StartGame();
    }

    void Die(bool caught)
    {
        if (state != GameState.Playing) return;

        state = GameState.Dying;
        stateTime = 0f;
        caughtByBoss = caught;
        paused = false;

        ui.SetPaused(false);
        ui.SetDanger(0f);
        shake = caught ? 0.55f : 0.34f;

        velocity = caught ? 3.6f : Mathf.Min(velocity, 0.5f);

        sfx.Hit();
        ui.Flash(caught ? new Color(0.95f, 0.08f, 0.06f, 0.55f)
                        : new Color(1f, 1f, 1f, 0.4f));

        particles.Burst(new Vector3(playerX, playerY, 0f),
            caught ? new Color(1f, 0.35f, 0.28f, 1f) : new Color(1f, 0.9f, 0.45f, 1f),
            18, 5f, 0.22f, 0.9f, -9f);
    }

    void EndRun()
    {
        state = GameState.GameOver;
        stateTime = 0f;

        newRecord = score > record;
        if (newRecord)
        {
            record = score;
            PlayerPrefs.SetInt(RecordKey, record);
            PlayerPrefs.Save();
        }

        ui.SetDanger(0f);
        ui.ShowGameOver(score, record, newRecord, caughtByBoss);
    }

    // ------------------------------------------------------------------
    // Acciones
    // ------------------------------------------------------------------

    void Flap()
    {
        velocity = FlapVelocity;
        flapKick = 1f;
        sfx.Flap();

        particles.Puff(new Vector3(playerX - 0.5f, playerY - 0.25f, 0f),
            new Color(1f, 1f, 1f, 0.3f), 0.5f, 0.45f);
    }

    void OnScore()
    {
        score++;
        ui.SetScore(score);
        ui.SetBest(Mathf.Max(record, score));
        sfx.Score();

        if (score > record) newRecord = true;

        if (bossActive)
        {
            bossX = Mathf.Max(bossX - 1f, playerX - 12f);
            particles.Burst(new Vector3(bossX, bossY, 0f),
                new Color(1f, 0.35f, 0.28f, 0.85f), 5, 2.2f, 0.26f, 0.5f, 0f);
        }
    }

    void ActivateBoss()
    {
        bossActive = true;
        bossAppear = 0f;
        bossX = playerX - 7.5f;
        bossY = playerY;
        bossVelY = 0f;
        bossRoot.gameObject.SetActive(true);
        bossRoot.localPosition = new Vector3(bossX, bossY, 0f);
        bossRoot.localScale = Vector3.one * 0.6f;
        bossGlow.color = new Color(1f, 0.22f, 0.18f, 0.14f);

        shake = Mathf.Max(shake, 0.18f);
        sfx.Boss();
        ui.Flash(new Color(1f, 0.2f, 0.15f, 0.2f));
        ui.Warn("¡EL BOSS TE ESTÁ SIGUIENDO!");
    }

    void ApplyPlayerTransform()
    {
        float s = 1f + 0.2f * flapKick;
        playerRoot.localPosition = new Vector3(playerX, playerY, 0f);
        playerRoot.localRotation = Quaternion.Euler(0f, 0f, tilt);
        playerRoot.localScale = new Vector3(s, s, 1f);
    }

    // ------------------------------------------------------------------
    // Tubos / colisiones
    // ------------------------------------------------------------------

    void SpawnPipeAt(float x)
    {
        float half = gapSize * 0.5f;
        float min = groundTop + half + 0.9f;
        float max = CamSize - half - 0.75f;

        float center = Random.Range(min, max);
        if (pipes.Count > 0)
            center = Mathf.Clamp(center, lastGapCenter - 2.4f, lastGapCenter + 2.4f);
        center = Mathf.Clamp(center, min, max);
        lastGapCenter = center;

        pipes.Add(PipePair.Create(worldRoot, x, center, half, groundTop, CamSize + 3f));
    }

    void ClearPipes()
    {
        for (int i = 0; i < pipes.Count; i++)
        {
            if (pipes[i] != null) Destroy(pipes[i].gameObject);
        }
        pipes.Clear();
        lastGapCenter = 0f;
    }

    bool HitPipe(PipePair pipe)
    {
        float x = pipe.transform.localPosition.x - PipeHalfWidth;
        float w = PipeHalfWidth * 2f;
        float gapTop = pipe.GapCenter + pipe.GapHalf;
        float gapBottom = pipe.GapCenter - pipe.GapHalf;

        if (CircleRect(playerX, playerY, PlayerRadius, x, gapTop, w, (CamSize + 3f) - gapTop))
            return true;

        float bottomMin = groundTop - 3f;
        if (CircleRect(playerX, playerY, PlayerRadius, x, bottomMin, w, gapBottom - bottomMin))
            return true;

        return false;
    }

    static bool CircleRect(float cx, float cy, float radius, float x, float y, float w, float h)
    {
        float nx = Mathf.Clamp(cx, x, x + w);
        float ny = Mathf.Clamp(cy, y, y + h);
        float dx = cx - nx;
        float dy = cy - ny;
        return dx * dx + dy * dy < radius * radius;
    }

    // ------------------------------------------------------------------
    // Ambiente
    // ------------------------------------------------------------------

    void MoveClouds(float dt, float drift)
    {
        float amount = drift * 0.14f;
        for (int i = 0; i < clouds.Count; i++)
        {
            var cloud = clouds[i];
            Vector3 p = cloud.localPosition;
            p.x -= amount * cloudParallax[i] * dt;

            if (p.x < -camHalfW - 4f)
            {
                p.x = camHalfW + Random.Range(0.5f, 3f);
                p.y = Random.Range(groundTop + 1.1f, CamSize - 0.3f);
            }

            cloud.localPosition = p;
        }
    }

    void MoveGroundMarks(float dt, float drift)
    {
        float span = camHalfW * 2f + 5f;
        for (int i = 0; i < groundMarks.Count; i++)
        {
            var mark = groundMarks[i];
            Vector3 p = mark.localPosition;
            p.x -= drift * dt;
            if (p.x < -camHalfW - 2.5f) p.x += span;
            mark.localPosition = p;
        }
    }

    void UpdateShake(float dt)
    {
        if (shake <= 0f)
        {
            worldRoot.localPosition = Vector3.zero;
            return;
        }

        shake = Mathf.MoveTowards(shake, 0f, dt * 1.9f);
        Vector2 offset = Random.insideUnitCircle * shake;
        worldRoot.localPosition = new Vector3(offset.x, offset.y, 0f);
    }
}

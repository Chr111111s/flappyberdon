using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Sistema de particulas muy liviano: sprites suaves con velocidad, gravedad y fade.
/// </summary>
public class SimpleParticles : MonoBehaviour
{
    class Particle
    {
        public Transform t;
        public SpriteRenderer sr;
        public Vector3 vel;
        public float life;
        public float maxLife;
        public float gravity;
        public float drag;
        public float startSize;
        public float endSize;
        public Color color;
    }

    readonly List<Particle> list = new List<Particle>();
    Sprite sprite;

    public static SimpleParticles Create(Transform parent, Sprite sprite)
    {
        var go = new GameObject("Particles");
        go.transform.SetParent(parent, false);
        var particles = go.AddComponent<SimpleParticles>();
        particles.sprite = sprite;
        return particles;
    }

    public void Burst(Vector3 localPos, Color color, int count, float speed, float size, float life, float gravity)
    {
        for (int i = 0; i < count; i++)
        {
            Vector2 dir = Random.insideUnitCircle;
            if (dir.sqrMagnitude < 0.001f) dir = Vector2.up;
            dir.Normalize();

            Spawn(localPos + new Vector3(dir.x, dir.y, 0f) * Random.Range(0f, 0.25f),
                color,
                new Vector3(dir.x, dir.y, 0f) * Random.Range(speed * 0.35f, speed),
                size * Random.Range(0.65f, 1.4f),
                life * Random.Range(0.7f, 1.2f),
                gravity);
        }
    }

    public void Puff(Vector3 localPos, Color color, float size, float life = 0.5f)
    {
        Spawn(localPos, color,
            new Vector3(Random.Range(-0.35f, 0.35f), Random.Range(-0.35f, 0.35f), 0f),
            size, life, 0f);
    }

    void Spawn(Vector3 localPos, Color color, Vector3 velocity, float size, float life, float gravity)
    {
        if (list.Count > 120) return;

        var go = new GameObject("Particle");
        go.transform.SetParent(transform, false);
        go.transform.localPosition = localPos;

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.color = color;
        sr.sortingOrder = 12;

        var p = new Particle
        {
            t = go.transform,
            sr = sr,
            vel = velocity,
            life = Mathf.Max(0.05f, life),
            maxLife = Mathf.Max(0.05f, life),
            gravity = gravity,
            drag = 1.4f,
            startSize = size,
            endSize = size * 0.25f,
            color = color
        };

        GameView.SetWorldSize(go.transform, sr, size, size);
        list.Add(p);
    }

    void Update()
    {
        float dt = Mathf.Min(Time.deltaTime, 0.033f);

        for (int i = list.Count - 1; i >= 0; i--)
        {
            var p = list[i];
            p.life -= dt;

            if (p.life <= 0f)
            {
                Destroy(p.t.gameObject);
                list.RemoveAt(i);
                continue;
            }

            p.vel += Vector3.up * (p.gravity * dt);
            p.vel *= Mathf.Clamp01(1f - p.drag * dt);
            p.t.localPosition += p.vel * dt;

            float k = Mathf.Clamp01(p.life / p.maxLife);
            Color c = p.color;
            c.a *= k;
            p.sr.color = c;

            float size = Mathf.Lerp(p.endSize, p.startSize, k);
            GameView.SetWorldSize(p.t, p.sr, size, size);
        }
    }
}

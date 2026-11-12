using UnityEngine;

/// <summary>
/// Un par de tubos (arriba y abajo) con su hueco. Se mueve en X desde el juego.
/// </summary>
public class PipePair : MonoBehaviour
{
    public const float HalfWidth = 0.9f;

    public float GapCenter { get; private set; }
    public float GapHalf { get; private set; }
    public bool Scored { get; set; }

    public static PipePair Create(Transform parent, float x, float gapCenter, float gapHalf, float groundTop, float ceiling)
    {
        var go = new GameObject("Pipe");
        go.transform.SetParent(parent, false);
        go.transform.localPosition = new Vector3(x, 0f, 0f);

        var pipe = go.AddComponent<PipePair>();
        pipe.GapCenter = gapCenter;
        pipe.GapHalf = gapHalf;
        pipe.Build(groundTop, ceiling);
        return pipe;
    }

    void Build(float groundTop, float ceiling)
    {
        const float bodyWidth = 1.5f;
        const float capWidth = 1.8f;
        const float capHeight = 0.4f;

        float topEdge = GapCenter + GapHalf;
        float bottomEdge = GapCenter - GapHalf;
        float topMax = ceiling;
        float bottomMin = groundTop - 0.65f;

        if (topMax - topEdge > 0.05f)
        {
            var body = GameView.AddSprite(transform, "BodyTop", GameView.PipeBody, 1, Color.white,
                Vector3.zero, bodyWidth, topMax - topEdge);
            body.transform.localPosition = new Vector3(0f, (topEdge + topMax) * 0.5f, 0f);
        }

        GameView.AddSprite(transform, "CapTop", GameView.PipeCap, 2, Color.white,
            new Vector3(0f, topEdge + capHeight * 0.5f, 0f), capWidth, capHeight);

        if (bottomEdge - bottomMin > 0.05f)
        {
            var body = GameView.AddSprite(transform, "BodyBottom", GameView.PipeBody, 1, Color.white,
                Vector3.zero, bodyWidth, bottomEdge - bottomMin);
            body.transform.localPosition = new Vector3(0f, (bottomMin + bottomEdge) * 0.5f, 0f);
        }

        GameView.AddSprite(transform, "CapBottom", GameView.PipeCap, 2, Color.white,
            new Vector3(0f, bottomEdge - capHeight * 0.5f, 0f), capWidth, capHeight);
    }
}

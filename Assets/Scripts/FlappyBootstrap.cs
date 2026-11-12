using UnityEngine;

/// <summary>
/// Arranca el juego automaticamente al entrar en Play, en cualquier escena.
/// No hace falta tocar la escena ni arrastrar componentes.
/// </summary>
public static class FlappyBootstrap
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics()
    {
        GameView.ClearCache();
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Launch()
    {
        if (Object.FindAnyObjectByType<FlappyGame>() != null) return;

        var go = new GameObject("Flappy Pastrana");
        go.AddComponent<FlappyGame>();
    }
}

using UnityEngine;

namespace OneMoreSip
{
    /// <summary>
    /// Entry point. Uses RuntimeInitializeOnLoadMethod so the ENTIRE game spins up
    /// the moment you press Play - even on a completely empty scene.
    /// No manual scene setup, prefabs, or wiring required.
    /// </summary>
    public static class GameBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Launch()
        {
            // Avoid double-spawning if a GameManager somehow already exists.
            if (Object.FindObjectOfType<GameManager>() != null) return;

            var go = new GameObject("[OneMoreSip GameManager]");
            go.AddComponent<GameManager>();
        }
    }
}

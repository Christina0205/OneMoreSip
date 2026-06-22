/// <summary>
/// Tiny static carrier for data that must survive a scene change
/// (e.g. the Drunk Level the player reached before entering the bathroom).
/// Static fields persist across SceneManager.LoadScene.
/// </summary>
public static class GameSession
{
    public static int DrunkLevel = 0;
}

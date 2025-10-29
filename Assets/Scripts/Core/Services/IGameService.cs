using UnityEngine;

/// <summary>
/// Basic lifecycle contract for persistent services that live across scene loads.
/// </summary>
public interface IGameService
{
    /// <summary>
    /// Called once immediately after the service has been registered.
    /// Use this to perform any initialization that previously lived in Start.
    /// </summary>
    void Initialize();

    /// <summary>
    /// Called when the service is being torn down.
    /// Use this to release resources or unsubscribe from events.
    /// </summary>
    void Shutdown();
}

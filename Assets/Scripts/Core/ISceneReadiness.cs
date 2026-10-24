namespace PeliCode.Core
{
    /// <summary>
    /// Una escena que necesita preparar algo antes de mostrarse (por ejemplo, el minijuego
    /// cargando sus preguntas). Mientras IsSceneReady sea false, la pantalla de carga sigue.
    /// </summary>
    public interface ISceneReadiness
    {
        bool IsSceneReady { get; }
    }
}

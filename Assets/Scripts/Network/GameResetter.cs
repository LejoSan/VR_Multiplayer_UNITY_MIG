using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.Netcode;

public class GameResetter : MonoBehaviour
{
    [Header("Configuración de Escenas")]
    public string nombreEscenaLobby = "SampleScene";

    // --- FUNCIÓN 1: Botón "Finalizar/Terminar" del Host (Bloque 5) ---
    public void BTN_HostFinalizarYResetear()
    {
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer)
        {
            Debug.Log("<color=red><b>[GAME RESET]</b></color> Host finaliza la partida. Cerrando red...");
            NetworkManager.Singleton.Shutdown();
            EjecutarLimpiezaAbsolutaLocamente();
        }
    }

    // --- FUNCIÓN 2: Botón de "Reinicio de Emergencia" ---
    public void BTN_ReinicioLocalAbsoluto()
    {
        Debug.Log("<color=yellow><b>[APP RESET]</b></color> Ejecutando reinicio local...");

        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.Shutdown();
        }

        EjecutarLimpiezaAbsolutaLocamente();
    }

    private void EjecutarLimpiezaAbsolutaLocamente()
    {
        Debug.Log("Recargando escena inicial y reconstruyendo jerarquía...");
        // Al recargar la escena en modo Single, Unity destruirá automáticamente 
        // los objetos antiguos y ejecutará los Awake() limpios.
        SceneManager.LoadScene(nombreEscenaLobby, LoadSceneMode.Single);
    }
}
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
        Debug.Log("Destruyendo Singletons antiguos y recargando escena inicial...");

        // 🌟 Destruir Singletons persistentes para que no contaminen la escena limpia
        if (MainGameManager.Instance != null)
        {
            Destroy(MainGameManager.Instance.gameObject);
        }

        if (NetworkManager.Singleton != null)
        {
            Destroy(NetworkManager.Singleton.gameObject);
        }

        // Recargar la escena limpia recreará la cámara y el Passthrough de las Quest 3 desde cero
        SceneManager.LoadScene(nombreEscenaLobby, LoadSceneMode.Single);
    }
}
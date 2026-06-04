using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP; // Importante para la IP

public class GameResetter : MonoBehaviour
{
    [Header("Configuración de Escenas")]
    public string nombreEscenaLobby = "SampleScene"; // Asegúrate de que coincida con tu nombre real

    // --- FUNCIÓN 1: Botón "Finalizar/Terminar" del Host (Bloque 5) ---
    // Esta función se debe sincronizar. El host la pulsa y desconecta a todos.
    public void BTN_HostFinalizarYResetear()
    {
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer)
        {
            Debug.Log("<color=red><b>[GAME RESET]</b></color> Host finaliza la partida. Cerrando conexión de red...");

            // Al hacer Shutdown en el server, NGO desconecta automáticamente a todos los clientes.
            NetworkManager.Singleton.Shutdown();

            // Iniciamos el proceso de limpieza local y recarga de escena en el Host
            // (Los clientes deberán invocar su propia limpieza al detectar la desconexión)
            EjecutarLimpiezaAbsolutaLocamente();
        }
    }

    // --- FUNCIÓN 2: Botón de "Reinicio de Emergencia" (Cache/App) ---
    // Esta función es puramente local. Se pulsa y resetea la app instantáneamente.
    public void BTN_ReinicioLocalAbsoluto()
    {
        Debug.Log("<color=yellow><b>[APP RESET]</b></color> Ejecutando reinicio de cache y escena local...");

        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.Shutdown();
        }

        EjecutarLimpiezaAbsolutaLocamente();
    }

    // --- EL NÚCLEO DEL RESETEO (Limpieza de Cache y Memoria) ---
    private void EjecutarLimpiezaAbsolutaLocamente()
    {
        Debug.Log("Iniciando limpieza de Singletons y estáticos...");

        // 1. 🧹 LIMPIEZA DE SINGLETONS (Elimina el "Cache" de lógica de juego)
        // Esto soluciona problemas de datos persistentes viejos que contaminan la nueva partida.

        // Asignamos NULL a las instancias estáticas para que Unity las limpie de memoria.
        // Asegúrate de que estos nombres coincidan con los nombres de tus scripts Managers.
        if (LobbyManager.Instance != null) LobbyManager.Instance = null;
        if (GameplayManager.Instance != null) GameplayManager.Instance = null;
        if (MainGameManager.Instance != null) MainGameManager.Instance = null;

        // Si tienes otros Singletons (Audio, UI Global, etc.), añádelos aquí:
        // Ej: MiMusicManager.Instance = null;

        Debug.Log("Singletons limpiados. Recargando escena inicial...");

        // 2. 🌀 RECARGA DE ESCENA (Elimina el "Robot Fantasma" y jerarquías físicas)
        // Al cargar la escena de nuevo, Unity reconstruye todo desde cero (Awake/Start).
        SceneManager.LoadScene(nombreEscenaLobby);
    }
}
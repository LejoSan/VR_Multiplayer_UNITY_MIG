using UnityEngine;
using UnityEngine.InputSystem; // <--- NECESARIO PARA EL NUEVO SISTEMA

public class DevSkipper : MonoBehaviour
{
    void Update()
    {
        // Verificamos si hay un teclado conectado para evitar errores
        if (Keyboard.current == null) return;

        // --- COMANDO 1: F1 (Saltar Intro) ---
        if (Keyboard.current.f1Key.wasPressedThisFrame)
        {
            Debug.Log("🔧 DEV: [F1] Saltando Intro...");
            if (MainGameManager.Instance != null)
            {
                MainGameManager.Instance.Debug_SaltarAlGameplay();
            }
        }

        // --- COMANDO 2: F2 (Ganar / Fin de Tiempo) ---
        if (Keyboard.current.f2Key.wasPressedThisFrame)
        {
            Debug.Log("🔧 DEV: [F2] Forzando Fin de Partida...");
            if (GameplayManager.Instance != null)
            {
                // Asegúrate de haber creado este método en GameplayManager
                // O usa: GameplayManager.Instance.tiempoDeJuego = 0;
                GameplayManager.Instance.Debug_ForzarFinal();
            }

        }

        // --- COMANDO 3: F3 (Reiniciar Escena) ---
        if (Keyboard.current.f3Key.wasPressedThisFrame)
        {
            Debug.Log("🔧 DEV: [F3] Recargando Escena...");
            UnityEngine.SceneManagement.SceneManager.LoadScene(
                UnityEngine.SceneManagement.SceneManager.GetActiveScene().name
            );
        }


    }
}
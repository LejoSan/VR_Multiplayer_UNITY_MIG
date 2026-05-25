using UnityEngine;
using Unity.Netcode;

public class PlayerNetworkSetup : NetworkBehaviour
{
    [Header("Componentes locales a desactivar si no es el dueño")]
    public GameObject cameraVisuals; // Tu Main Camera de VR
    public AudioListener audioListener;

    // Lista de componentes de XR Interaction Toolkit que controlan el Input
    private MonoBehaviour[] componentesXR;

    void Start()
    {
        // Buscamos todos los componentes de XR en este Rig (manos, lococión, etc.)
        componentesXR = GetComponentsInChildren<MonoBehaviour>();

        if (!IsOwner)
        {
            // --- JUGADOR REMOTO (El "NPC" que ves de tus amigos) ---
            Debug.Log("Configurando clon de red como avatar visual.");

            // Desactivamos su cámara y su audio para no escuchar ni ver a través de él
            if (cameraVisuals != null) cameraVisuals.SetActive(false);
            if (audioListener != null) audioListener.enabled = false;

            // Desactivamos sus scripts de tracking e input para que no controlen nuestro visor
            foreach (var comp in componentesXR)
            {
                // Desactivamos componentes de XRI, tracking de manos, etc.
                if (comp.GetType().Namespace != null && comp.GetType().Namespace.Contains("XR"))
                {
                    comp.enabled = false;
                }
            }
        }
        else
        {
            // --- JUGADOR LOCAL (Tú mismo) ---
            Debug.Log("¡Este soy yo! Control total activado.");
            // Aquí puedes activar mallas visuales invisibles para ti mismo si no quieres ver tu propio cuerpo flotando.
        }
    }
}
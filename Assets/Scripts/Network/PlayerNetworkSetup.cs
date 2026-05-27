using UnityEngine;
using System.Collections;
using Unity.Netcode;

public class PlayerNetworkSetup : NetworkBehaviour
{
    [Header("Componentes a apagar en los clones")]
    public Camera camaraVR;
    public AudioListener audioListener;
    public GameObject[] controlesYRayos;

    public override void OnNetworkSpawn()
    {
        if (IsOwner)
        {
            // 1. Tomamos la posición del maniquí del lobby
            GameObject maniquiLobby = GameObject.Find("XR_Lobby");
            if (maniquiLobby != null)
            {
                transform.position = maniquiLobby.transform.position;
                transform.rotation = maniquiLobby.transform.rotation;

                // 2. Desactivamos (no destruimos) el maniquí
                maniquiLobby.SetActive(false);
            }
        }
        else
        {
            // 3. Apagamos los componentes de otros jugadores con un pequeño retraso
            // para no interferir con la inicialización del visor VR
            StartCoroutine(ApagarComponentesConRetraso());
        }
    }

    private IEnumerator ApagarComponentesConRetraso()
    {
        // Esperamos un frame para que la cámara y el visor terminen su calibración inicial
        yield return null;

        if (camaraVR != null) camaraVR.enabled = false;

        // Dejamos el AudioListener activo por ahora para descartar errores de sonido,
        // pero si escuchas ecos raros, puedes descomentar la siguiente línea:
        // if (audioListener != null) audioListener.enabled = false;

        foreach (var control in controlesYRayos)
        {
            if (control != null) control.SetActive(false);
        }
    }
}
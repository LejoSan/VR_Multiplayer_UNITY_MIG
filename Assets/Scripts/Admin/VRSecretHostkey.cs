using UnityEngine;
using Unity.Netcode;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class VRSecretHostKey : MonoBehaviour
{
    [Header("Configuración")]
    public float tiempoMantener = 2.0f;
    private float contadorTiempo = 0f;

    void Update()
    {
        // Si la red ya arrancó, ignoramos
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening) return;

        bool modoDevSolicitado = false;

        // 🌟 1. TECLA PARA SIMULADOR DE UNITY EN PC:
        // Mantén presionada la tecla 'H' durante 2 segundos
#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current != null && Keyboard.current.hKey.isPressed)
        {
            modoDevSolicitado = true;
        }
#endif

        // 🌟 2. MANDO REAL VR (SÓLO DENTRO DE LAS META QUEST FÍSICAS):
        if (!modoDevSolicitado)
        {
            UnityEngine.XR.InputDevice manoDerecha = UnityEngine.XR.InputDevices.GetDeviceAtXRNode(UnityEngine.XR.XRNode.RightHand);

            if (manoDerecha.isValid)
            {
                manoDerecha.TryGetFeatureValue(UnityEngine.XR.CommonUsages.gripButton, out bool gripBool);
                manoDerecha.TryGetFeatureValue(UnityEngine.XR.CommonUsages.triggerButton, out bool triggerBool);
                manoDerecha.TryGetFeatureValue(UnityEngine.XR.CommonUsages.grip, out float gripFloat);
                manoDerecha.TryGetFeatureValue(UnityEngine.XR.CommonUsages.trigger, out float triggerFloat);

                if ((gripBool || gripFloat > 0.5f) && (triggerBool || triggerFloat > 0.5f))
                {
                    modoDevSolicitado = true;
                }
            }
        }

        // 3. PROCESAR CONTEO
        if (modoDevSolicitado)
        {
            EjecutarConteo();
        }
        else
        {
            contadorTiempo = 0f;
        }
    }

    private void EjecutarConteo()
    {
        contadorTiempo += Time.deltaTime;
        Debug.Log($"<color=cyan>[SECRET KEY]</color> Manteniendo activador... ({contadorTiempo:F1}s / {tiempoMantener:F1}s)");

        if (contadorTiempo >= tiempoMantener)
        {
            contadorTiempo = 0f;

            if (LobbyManager.Instance != null)
            {
                Debug.Log("<color=green><b>[DEV MODE KEY]</b></color> ¡Modo desarrollador activado!");
                LobbyManager.Instance.ActivarMenuModoDev();
            }
            else
            {
                Debug.LogError("[SECRET KEY ERROR] 'LobbyManager.Instance' es NULL. Revisa la asignación de LobbyManager.");
            }
        }
    }
}
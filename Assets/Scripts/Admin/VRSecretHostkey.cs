using UnityEngine;
using UnityEngine.XR;
using Unity.Netcode;

public class VRSecretHostKey : MonoBehaviour
{
    [Tooltip("Tiempo en segundos manteniendo presionada la combinación para desbloquear Dev Mode")]
    public float tiempoMantener = 3.0f;
    private float contadorTiempo = 0f;

    void Update()
    {
        // Si ya nos conectamos a la red, no hace falta escuchar el comando
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening) return;

        InputDevice manoIzquierda = InputDevices.GetDeviceAtXRNode(XRNode.LeftHand);
        InputDevice manoDerecha = InputDevices.GetDeviceAtXRNode(XRNode.RightHand);

        bool gripIzquierdo = false;
        bool gripDerecho = false;
        bool botonB = false;

        manoIzquierda.TryGetFeatureValue(CommonUsages.gripButton, out gripIzquierdo);
        manoDerecha.TryGetFeatureValue(CommonUsages.gripButton, out gripDerecho);
        manoDerecha.TryGetFeatureValue(CommonUsages.secondaryButton, out botonB);

        // Combinación: Grip Izquierdo + Grip Derecho + Botón B (Mando Derecho)
        if (gripIzquierdo && gripDerecho && botonB)
        {
            contadorTiempo += Time.deltaTime;

            if (contadorTiempo >= tiempoMantener)
            {
                contadorTiempo = 0f;
                if (LobbyManager.Instance != null)
                {
                    LobbyManager.Instance.ActivarMenuModoDev();
                }
            }
        }
        else
        {
            contadorTiempo = 0f;
        }
    }
}
using Unity.Netcode;
using UnityEngine;

public class PlayerNetworkState : NetworkBehaviour
{
    [Header("--- Datos Sincronizados de Red ---")]
    public NetworkVariable<int> puntuacion = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public NetworkVariable<Vector4> colorJugadorNet = new NetworkVariable<Vector4>(Vector4.one, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    [Header("--- Sincronización de Transformaciones (Owner Write) ---")]
    public NetworkVariable<Vector3> posCabeza = new NetworkVariable<Vector3>(Vector3.zero, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    public NetworkVariable<Quaternion> rotCabeza = new NetworkVariable<Quaternion>(Quaternion.identity, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

    public NetworkVariable<Vector3> posManoIzq = new NetworkVariable<Vector3>(Vector3.zero, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    public NetworkVariable<Quaternion> rotManoIzq = new NetworkVariable<Quaternion>(Quaternion.identity, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

    public NetworkVariable<Vector3> posManoDer = new NetworkVariable<Vector3>(Vector3.zero, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    public NetworkVariable<Quaternion> rotManoDer = new NetworkVariable<Quaternion>(Quaternion.identity, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

    [Header("--- Estados de Power-ups ---")]
    public NetworkVariable<int> multiplicadorPuntos = new NetworkVariable<int>(1, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public NetworkVariable<bool> puedeDisparar = new NetworkVariable<bool>(true, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    [Header("--- Referencias Visuales de las Manos ---")]
    public Renderer meshManoIzquierda;
    public Renderer meshManoDerecha;

    public override void OnNetworkSpawn()
    {
        if (IsOwner)
        {
            EstablecerColorInicialServerRpc(NetworkManager.Singleton.LocalClientId);
        }

        colorJugadorNet.OnValueChanged += (oldVal, newVal) => AplicarColorMallas(newVal);
        AplicarColorMallas(colorJugadorNet.Value);
    }

    [ServerRpc]
    private void EstablecerColorInicialServerRpc(ulong idCliente)
    {
        if (LobbyManager.Instance != null)
        {
            Color colorElegido = LobbyManager.Instance.ObtenerColorPorID(idCliente);
            colorJugadorNet.Value = new Vector4(colorElegido.r, colorElegido.g, colorElegido.b, colorElegido.a);
        }
    }

    private void AplicarColorMallas(Vector4 vectorColor)
    {
        Color colorFinal = new Color(vectorColor.x, vectorColor.y, vectorColor.z, vectorColor.w);

        // Soporte universal para materiales estándar y URP (_BaseColor)
        if (meshManoIzquierda != null && meshManoIzquierda.material != null)
        {
            if (meshManoIzquierda.material.HasProperty("_BaseColor")) meshManoIzquierda.material.SetColor("_BaseColor", colorFinal);
            else meshManoIzquierda.material.color = colorFinal;
        }

        if (meshManoDerecha != null && meshManoDerecha.material != null)
        {
            if (meshManoDerecha.material.HasProperty("_BaseColor")) meshManoDerecha.material.SetColor("_BaseColor", colorFinal);
            else meshManoDerecha.material.color = colorFinal;
        }
    }

    public void ModificarPuntuacionServer(int cantidadBase)
    {
        if (!IsServer) return;
        puntuacion.Value += cantidadBase * multiplicadorPuntos.Value;
    }
}
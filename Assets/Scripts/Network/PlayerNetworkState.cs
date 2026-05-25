using UnityEngine;
using Unity.Netcode;

public class PlayerNetworkState : NetworkBehaviour
{
    [Header("--- Datos Sincronizados de Red ---")]
    // La puntuación individual de este jugador
    public NetworkVariable<int> puntuacion = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    // El color asignado al jugador (enviado como Vector4 porque Color no es nativo de NetworkVariable)
    public NetworkVariable<Vector4> colorJugadorNet = new NetworkVariable<Vector4>(Vector4.one, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    [Header("--- Estados de Power-ups (Futuro) ---")]
    public NetworkVariable<int> multiplicadorPuntos = new NetworkVariable<int>(1, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public NetworkVariable<bool> puedeDisparar = new NetworkVariable<bool>(true, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    [Header("--- Referencias Visuales del Avatar ---")]
    public Renderer meshManoIzquierda;
    public Renderer meshManoDerecha;

    public override void OnNetworkSpawn()
    {
        // Si soy el dueño de este avatar VR, le pido al servidor que me asigne mi color del lobby
        if (IsOwner)
        {
            EstablecerColorInicialServerRpc(NetworkManager.Singleton.LocalClientId);
        }

        // Nos suscribimos al cambio de color para que, cuando el servidor lo asigne, se pinte en las manos de todos
        colorJugadorNet.OnValueChanged += (oldVal, newVal) => AplicarColorMallas(newVal);

        // Aplicar el color actual por si entramos tarde (Late-join)
        AplicarColorMallas(colorJugadorNet.Value);
    }

    // El cliente le pide al servidor: "Oye, búscame en el LobbyManager y mira qué color elegí"
    [ServerRpc]
    private void EstablecerColorInicialServerRpc(ulong idCliente)
    {
        if (LobbyManager.Instance != null)
        {
            Color colorElegido = LobbyManager.Instance.ObtenerColorPorID(idCliente);
            // Convertimos Color a Vector4 para guardarlo en la NetworkVariable
            colorJugadorNet.Value = new Vector4(colorElegido.r, colorElegido.g, colorElegido.b, colorElegido.a);
        }
    }

    private void AplicarColorMallas(Vector4 vectorColor)
    {
        Color colorFinal = new Color(vectorColor.x, vectorColor.y, vectorColor.z, vectorColor.w);

        // Pintamos el material de las manos virtuales del color seleccionado
        if (meshManoIzquierda != null) meshManoIzquierda.material.color = colorFinal;
        if (meshManoDerecha != null) meshManoDerecha.material.color = colorFinal;
    }

    // --- MÉTODOS PÚBLICOS PARA EL GAMEPLAY ---

    // Función autoritaria para sumar puntos (Saber si aplica x2)
    public void ModificarPuntuacionServer(int cantidadBase)
    {
        if (!IsServer) return;
        puntuacion.Value += cantidadBase * multiplicadorPuntos.Value;
    }

    // Funciones que usaremos en el Bloque 7 para los Power-ups
    public void SetMultiplicadorServer(int nuevoMultiplicador)
    {
        if (!IsServer) return;
        multiplicadorPuntos.Value = nuevoMultiplicador;
    }

    public void SetEstadoDisparoServer(bool estado)
    {
        if (!IsServer) return;
        puedeDisparar.Value = estado;
    }
}
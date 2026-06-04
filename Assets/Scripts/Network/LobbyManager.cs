using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using TMPro;

public class LobbyManager : NetworkBehaviour
{
    public static LobbyManager Instance;

    [Header("--- Paneles del Lobby Bloque 1 ---")]
    public GameObject panelConexion;
    public GameObject panelColores;
    public GameObject panelEspera;

    // ¡NUEVO! Referencia al maniquí para despertarlo
    public GameObject xrLobbyManiqui;

    [Header("--- Botones de Colores (Paso 2) ---")]
    public Button btnRojo;
    public Button btnAzul;
    public Button btnVerde;
    public Button btnAmarillo;
    public Button btnContinuar;

    [Header("--- Sala de Espera (Paso 3) ---")]
    public GameObject btnIniciarPartida;
    public TextMeshProUGUI textoEstadoEspera;
    public TextMeshProUGUI textoListaJugadores;

    [Header("--- NUMERO DE LA IP / ORDENADOR - VR HEADSET ---")]
    [Tooltip("Si usas el panel dinámico con teclado, puedes dejar esto vacío.")]
    public string Ipnumero = "";
    public ushort Puerto = 7778;


    // Variables de red para sincronizar los colores (999 = Color Libre)
    private NetworkVariable<ulong> dueñoRojo = new NetworkVariable<ulong>(999, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    private NetworkVariable<ulong> dueñoAzul = new NetworkVariable<ulong>(999, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    private NetworkVariable<ulong> dueñoVerde = new NetworkVariable<ulong>(999, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    private NetworkVariable<ulong> dueñoAmarillo = new NetworkVariable<ulong>(999, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    private int colorSeleccionadoLocal = -1;



    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        // Forzar estado inicial de las pantallas de UI
        panelConexion.SetActive(true);
        panelColores.SetActive(false);
        panelEspera.SetActive(false);
        if (btnContinuar != null) btnContinuar.interactable = false;

        // Suscribir el filtro de entrada al NetworkManager de Unity 6
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.ConnectionApprovalCallback = ApprovalCheck;
        }
    }


    // === PASO 1: LÓGICA DE CONEXIÓN LAN (CON PAYLOAD)


    public void BTN_IniciarHost()
    {
        EnviarIdentificacionDispositivo("VR");
        NetworkManager.Singleton.StartHost();
        IrAPanelColores();
    }

    public void BTN_IniciarClienteVR()
    {
        // 1. Accedemos de forma segura al componente de transporte de red
        if (NetworkManager.Singleton != null)
        {
            UnityTransport transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
            if (transport != null)
            {
                // 🌟 ESCUDO MULTIJUGADOR DIÁMICO:
                // Si la IP del transporte sigue siendo la de fábrica (127.0.0.1 o vacía), significa que no usamos el teclado virtual.
                // En ese caso, aplicamos la IP por defecto de las variables de este script como respaldo.
                if (transport.ConnectionData.Address == "127.0.0.1" || string.IsNullOrEmpty(transport.ConnectionData.Address))
                {
                    if (!string.IsNullOrEmpty(Ipnumero)) transport.ConnectionData.Address = Ipnumero;
                    if (Puerto != 0) transport.ConnectionData.Port = Puerto;
                }

                // Sincronizamos las variables locales de este script para que muestren la IP real que se está usando
                Ipnumero = transport.ConnectionData.Address;
                Puerto = transport.ConnectionData.Port;

                Debug.Log($"<color=cyan><b>[LOBBY]</b></color> Intentando conectar al Servidor en la dirección -> IP: {Ipnumero} | Puerto: {Puerto}");
            }
        }

        // 2. Tu flujo original intacto
        EnviarIdentificacionDispositivo("VR");
        NetworkManager.Singleton.StartClient();
        IrAPanelColores();
    }

    public void BTN_IniciarClienteAdminMovil()
    {
        // Función dedicada para tu futura App de Móvil
        EnviarIdentificacionDispositivo("ADMIN");
        NetworkManager.Singleton.StartClient();

        // Al móvil no lo mandamos a elegir color, saltamos directo a su pantalla de control
        panelConexion.SetActive(false);
        panelEspera.SetActive(true);
        btnIniciarPartida.SetActive(true); // El admin móvil sí podrá iniciar el juego
        textoEstadoEspera.text = "Modo Administrador Activo.\nVisualizando sesión en tiempo real.";
    }

    private void EnviarIdentificacionDispositivo(string tipo)
    {
        byte[] payload = System.Text.Encoding.ASCII.GetBytes(tipo);
        NetworkManager.Singleton.NetworkConfig.ConnectionData = payload;
    }

    // --- BOTÓN DE ATRÁS / DESCONEXIÓN ---
    public void BTN_Atras_Desconectar()
    {
        // 1. Apagamos el motor de red y destruimos el cuerpo oficial
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.Shutdown();
        }

        // 2. ¡Despertamos al maniquí para recuperar las manos y la cámara!
        if (xrLobbyManiqui != null)
        {
            xrLobbyManiqui.SetActive(true);
        }

        // 3. Restauramos la UI
        if (panelConexion) panelConexion.SetActive(true);
        if (panelColores) panelColores.SetActive(false);
        if (panelEspera) panelEspera.SetActive(false);
    }
    private void IrAPanelColores()
    {
        panelConexion.SetActive(false);
        panelColores.SetActive(true);
    }

    // El "Guardia de Seguridad" del Servidor
    private void ApprovalCheck(NetworkManager.ConnectionApprovalRequest request, NetworkManager.ConnectionApprovalResponse response)
    {
        string tipoDispositivo = System.Text.Encoding.ASCII.GetString(request.Payload);

        // Si es el Administrador Móvil, entra directo sin generar un avatar físico en el mapa
        if (tipoDispositivo == "ADMIN")
        {
            response.Approved = true;
            response.CreatePlayerObject = false; // REGLA DE ORO: No le crea un XR Origin de VR
            response.Pending = false;
            return;
        }

        // Si es un visor VR, contamos cuántos visores reales (con avatar) hay dentro
        int visoresVR = 0;
        foreach (var client in NetworkManager.Singleton.ConnectedClientsList)
        {
            if (client.PlayerObject != null) visoresVR++;
        }

        if (visoresVR >= 4)
        {
            response.Approved = false;
            response.Reason = "Sala llena. Máximo 4 jugadores VR.";
        }
        else
        {
            response.Approved = true;
            response.CreatePlayerObject = true; // A los visores SÍ les crea su XR Origin en red
        }
        response.Pending = false;
    }


    // === PASO 2: SELECCIÓN DE COLOR DE RED

    public override void OnNetworkSpawn()
    {
        // Escuchar cuando cambien los dueños de los colores para actualizar la UI de todos
        dueñoRojo.OnValueChanged += (oldVal, newVal) => AlCambiarLobby();
        dueñoAzul.OnValueChanged += (oldVal, newVal) => AlCambiarLobby();
        dueñoVerde.OnValueChanged += (oldVal, newVal) => AlCambiarLobby();
        dueñoAmarillo.OnValueChanged += (oldVal, newVal) => AlCambiarLobby();

        AlCambiarLobby();
    }

    private void AlCambiarLobby()
    {
        ActualizarUIBotonesColores();
        ActualizarListaJugadoresUI();
    }

    public void BTN_SeleccionarColor(int indiceColor)
    {
        colorSeleccionadoLocal = indiceColor;
        if (btnContinuar != null) btnContinuar.interactable = true; // Ya puede avanzar
        SolicitarColorServerRpc(indiceColor, NetworkManager.Singleton.LocalClientId);
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    private void SolicitarColorServerRpc(int colorIndex, ulong idJugador)
    {
        LiberarColoresPrevios(idJugador);

        if (colorIndex == 0 && dueñoRojo.Value == 999) dueñoRojo.Value = idJugador;
        else if (colorIndex == 1 && dueñoAzul.Value == 999) dueñoAzul.Value = idJugador;
        else if (colorIndex == 2 && dueñoVerde.Value == 999) dueñoVerde.Value = idJugador;
        else if (colorIndex == 3 && dueñoAmarillo.Value == 999) dueñoAmarillo.Value = idJugador;
    }

    private void LiberarColoresPrevios(ulong idJugador)
    {
        if (dueñoRojo.Value == idJugador) dueñoRojo.Value = 999;
        if (dueñoAzul.Value == idJugador) dueñoAzul.Value = 999;
        if (dueñoVerde.Value == idJugador) dueñoVerde.Value = 999;
        if (dueñoAmarillo.Value == idJugador) dueñoAmarillo.Value = 999;
    }

    private void ActualizarUIBotonesColores()
    {
        ulong miID = NetworkManager.Singleton != null ? NetworkManager.Singleton.LocalClientId : 0;

        btnRojo.interactable = (dueñoRojo.Value == 999 || dueñoRojo.Value == miID);
        btnAzul.interactable = (dueñoAzul.Value == 999 || dueñoAzul.Value == miID);
        btnVerde.interactable = (dueñoVerde.Value == 999 || dueñoVerde.Value == miID);
        btnAmarillo.interactable = (dueñoAmarillo.Value == 999 || dueñoAmarillo.Value == miID);
    }


    // === PASO 3: FEEDBACK Y SALA DE ESPERA


    private void ActualizarListaJugadoresUI()
    {
        if (textoListaJugadores == null) return;

        string listaText = "<size=110%>CONECTADOS:</size>\n";
        if (dueñoRojo.Value != 999) listaText += "<color=red>■</color> Jugador VR (Rojo)\n";
        if (dueñoAzul.Value != 999) listaText += "<color=blue>■</color> Jugador VR (Azul)\n";
        if (dueñoVerde.Value != 999) listaText += "<color=green>■</color> Jugador VR (Verde)\n";
        if (dueñoAmarillo.Value != 999) listaText += "<color=yellow>■</color> Jugador VR (Amarillo)\n";

        textoListaJugadores.text = listaText;
    }

    public void BTN_ContinuarAPanelEspera()
    {
        if (colorSeleccionadoLocal == -1) return;

        panelColores.SetActive(false);
        panelEspera.SetActive(true);

        if (IsServer)
        {
            btnIniciarPartida.SetActive(true);
            textoEstadoEspera.text = "Eres el HOST (Líder de la sala).";
        }
        else
        {
            btnIniciarPartida.SetActive(false);
            textoEstadoEspera.text = "Esperando que el líder inicie la simulación...";
        }
    }

    public void BTN_HostIniciarJuego()
    {
        if (!IsServer) return;
        CerrarLobbyEnTodosLosClientesClientRpc();

        if (MainGameManager.Instance != null) MainGameManager.Instance.IniciarExperienciaDesdeLobby();
    }

    [ClientRpc]
    private void CerrarLobbyEnTodosLosClientesClientRpc()
    {
        gameObject.SetActive(false);
        Debug.Log("Lobby cerrado. ¡Comienza la experiencia!");
    }

    public Color ObtenerColorPorID(ulong idJugador)
    {
        if (dueñoRojo.Value == idJugador) return Color.red;
        if (dueñoAzul.Value == idJugador) return Color.blue;
        if (dueñoVerde.Value == idJugador) return Color.green;
        if (dueñoAmarillo.Value == idJugador) return Color.yellow;
        return Color.white;
    }
}
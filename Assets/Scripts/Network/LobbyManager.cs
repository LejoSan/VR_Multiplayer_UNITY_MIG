using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using TMPro;

public class LobbyManager : NetworkBehaviour
{
    public static LobbyManager Instance;

    [Header("--- UI PANELS (QUEST VR) ---")]
    public GameObject panelInicioSimplificado; // Nuevo panel con "JUGAR" y "CAMBIAR IP"
    public GameObject panelDevModo;            // Panel antiguo (Host / Cliente)
    public GameObject panelColores;            // Paso 2: Elección de color
    public GameObject panelEspera;             // Paso 3: Lista de conectados

    [Header("--- UI MOBILE ADMIN ---")]
    public GameObject canvasMobileAdmin;       // Canvas 2D (Screen Space - Overlay)
    public Camera camaraEspectadoraMovil;      // Cámara 3D Espectadora

    [Header("--- Elementos UI Inicio ---")]
    public TextMeshProUGUI textoIPActual;      // Muestra la IP actual guardada
    public GameObject panelTecladoIP;          // NonNativeKeyboard para emergencias

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

    [Header("--- CONFIGURACIÓN DE RED ---")]
    public string IpnumeroDefecto = "192.168.20.152";
    public ushort Puerto = 7778;

    private string ipFinalTrabajo;

    // Variables de red para sincronizar colores (999 = Libre)
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
        // 1. Cargar la IP guardada o usar la por defecto
        ipFinalTrabajo = PlayerPrefs.GetString("SAVED_HOST_IP", IpnumeroDefecto);
        ActualizarTextoIPUI();

        // 2. Estado inicial de paneles
        if (canvasMobileAdmin) canvasMobileAdmin.SetActive(false);
        if (camaraEspectadoraMovil) camaraEspectadoraMovil.gameObject.SetActive(false);

        panelInicioSimplificado.SetActive(true);
        if (panelDevModo) panelDevModo.SetActive(false);
        if (panelColores) panelColores.SetActive(false);
        if (panelEspera) panelEspera.SetActive(false);
        if (btnContinuar != null) btnContinuar.interactable = false;

        // Suscribir filtro de aprobación de red
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.ConnectionApprovalCallback = ApprovalCheck;
        }
    }

    // === METODOS DE INICIO DE CONEXIÓN ===

    // Botón principal "JUGAR" de las Quest
    public void BTN_UsuarioVR_JugarDirecto()
    {
        AplicarIPAlTransporte(ipFinalTrabajo);
        EnviarIdentificacionDispositivo("VR");
        NetworkManager.Singleton.StartClient();
        IrAPanelColores();
    }

    // Botón del Móvil Admin (Host Operador)
    public void BTN_IniciarHostAdminMovil()
    {
        // Desactivar XR Origin de VR en el móvil
        GameObject miXR = GameObject.Find("XR_Origin_LOCAL");
        if (miXR != null) miXR.SetActive(false);

        // Activar vista 3D Espectadora y Canvas 2D
        if (camaraEspectadoraMovil) camaraEspectadoraMovil.gameObject.SetActive(true);
        if (canvasMobileAdmin) canvasMobileAdmin.SetActive(true);

        // Ocultar paneles 3D del visor
        panelInicioSimplificado.SetActive(false);
        if (panelDevModo) panelDevModo.SetActive(false);

        // Iniciar Host Servidor
        EnviarIdentificacionDispositivo("ADMIN");
        NetworkManager.Singleton.StartHost();
    }

    // Abrir menú Secreto Dev (Invocado desde VRSecretHostKey)
    public void ActivarMenuModoDev()
    {
        if (panelInicioSimplificado) panelInicioSimplificado.SetActive(false);
        if (panelDevModo) panelDevModo.SetActive(true);
        Debug.Log("<color=yellow>[DEV MODE]</color> Panel secreto desbloqueado.");
    }

    // === GESTIÓN DE IP DE EMERGENCIA ===

    public void GuardarNuevaIPDesdeTeclado(string nuevaIP)
    {
        if (string.IsNullOrEmpty(nuevaIP)) return;

        ipFinalTrabajo = nuevaIP;
        PlayerPrefs.SetString("SAVED_HOST_IP", ipFinalTrabajo);
        PlayerPrefs.Save();

        ActualizarTextoIPUI();
        if (panelTecladoIP) panelTecladoIP.SetActive(false);
    }

    private void ActualizarTextoIPUI()
    {
        if (textoIPActual != null)
        {
            textoIPActual.text = $"IP Servidor: <color=yellow>{ipFinalTrabajo}</color>";
        }
    }

    private void AplicarIPAlTransporte(string ip)
    {
        if (NetworkManager.Singleton != null)
        {
            UnityTransport transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
            if (transport != null)
            {
                transport.ConnectionData.Address = ip;
                transport.ConnectionData.Port = Puerto;
                Debug.Log($"<color=cyan>[RED]</color> Apuntando a IP: {ip}:{Puerto}");
            }
        }
    }

    // === MÉTODOS ORIGINALES MANTENIDOS ===

    public void BTN_IniciarHost()
    {
        EnviarIdentificacionDispositivo("VR");
        NetworkManager.Singleton.StartHost();
        IrAPanelColores();
    }

    public void BTN_IniciarClienteVR()
    {
        AplicarIPAlTransporte(ipFinalTrabajo);
        EnviarIdentificacionDispositivo("VR");
        NetworkManager.Singleton.StartClient();
        IrAPanelColores();
    }

    private void EnviarIdentificacionDispositivo(string tipo)
    {
        byte[] payload = System.Text.Encoding.ASCII.GetBytes(tipo);
        NetworkManager.Singleton.NetworkConfig.ConnectionData = payload;
    }

    private void ApprovalCheck(NetworkManager.ConnectionApprovalRequest request, NetworkManager.ConnectionApprovalResponse response)
    {
        string tipoDispositivo = System.Text.Encoding.ASCII.GetString(request.Payload);

        if (tipoDispositivo == "ADMIN")
        {
            response.Approved = true;
            response.CreatePlayerObject = false;
            response.Pending = false;
            return;
        }

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
            response.CreatePlayerObject = true;
        }
        response.Pending = false;
    }

    private void IrAPanelColores()
    {
        panelInicioSimplificado.SetActive(false);
        if (panelDevModo) panelDevModo.SetActive(false);
        if (panelColores) panelColores.SetActive(true);
    }

    public override void OnNetworkSpawn()
    {
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
        if (btnContinuar != null) btnContinuar.interactable = true;
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

        if (panelColores) panelColores.SetActive(false);
        if (panelEspera) panelEspera.SetActive(true);

        if (IsServer)
        {
            if (btnIniciarPartida) btnIniciarPartida.SetActive(true);
            if (textoEstadoEspera) textoEstadoEspera.text = "Eres el HOST (Líder de la sala).";
        }
        else
        {
            if (btnIniciarPartida) btnIniciarPartida.SetActive(false);
            if (textoEstadoEspera) textoEstadoEspera.text = "Esperando que el operador inicie desde el móvil...";
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
        if (panelInicioSimplificado) panelInicioSimplificado.SetActive(false);
        if (panelDevModo) panelDevModo.SetActive(false);
        if (panelColores) panelColores.SetActive(false);
        if (panelEspera) panelEspera.SetActive(false);
        Debug.Log("Lobby cerrado con éxito.");
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
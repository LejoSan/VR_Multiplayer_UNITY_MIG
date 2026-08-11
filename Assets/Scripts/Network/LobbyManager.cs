using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using TMPro;

public class LobbyManager : NetworkBehaviour
{
    public static LobbyManager Instance;
    [Header("--- CONFIGURACIÓN DE PLATAFORMA ---")]
    [Tooltip("Marca esta casilla para probar como META QUEST (VR). Desmárcala para MÓVIL ADMIN.")]
    public bool forzarModoVR = false;

    [Header("--- UI PANELS (QUEST VR) ---")]
    public GameObject panelInicioSimplificado;
    public GameObject panelDevModo;
    public GameObject panelColores;
    public GameObject panelEspera;

    [Header("--- UI MOBILE ADMIN ---")]
    public GameObject canvasMobileAdmin;
    public Camera camaraEspectadoraMovil;
    public GameObject panelMovilBotonInicio;
    public GameObject panelMovilDashboard;
    public TextMeshProUGUI textoEstadoServidorMovil;
    public TextMeshProUGUI textoIPInicioMovil; // Muestra la IP antes de iniciar
    public TMP_InputField inputIPManualMovil; // Arrastra el InputField si deseas usar IP manual
    public GameObject btnReiniciarSimulacion;

    [Header("--- Elementos UI Inicio ---")]
    public TextMeshProUGUI textoIPActual;
    public GameObject panelTecladoIP;

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

    //void Start()
    //{
    //    ipFinalTrabajo = PlayerPrefs.GetString("SAVED_HOST_IP", IpnumeroDefecto);
    //    ActualizarTextoIPUI();

    //    if (canvasMobileAdmin) canvasMobileAdmin.SetActive(false);
    //    if (camaraEspectadoraMovil) camaraEspectadoraMovil.gameObject.SetActive(false);

    //    if (panelInicioSimplificado) panelInicioSimplificado.SetActive(true);
    //    if (panelDevModo) panelDevModo.SetActive(false);
    //    if (panelColores) panelColores.SetActive(false);
    //    if (panelEspera) panelEspera.SetActive(false);
    //    if (btnContinuar != null) btnContinuar.interactable = false;

    //    if (NetworkManager.Singleton != null)
    //    {
    //        NetworkManager.Singleton.ConnectionApprovalCallback = ApprovalCheck;
    //    }
    //}
    void Start()
    {
        ipFinalTrabajo = PlayerPrefs.GetString("SAVED_HOST_IP", IpnumeroDefecto);
        ActualizarTextoIPUI();

        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.ConnectionApprovalCallback = ApprovalCheck;
        }

        // 🌟 EVALUACIÓN DE PLATAFORMA: Usa la casilla del Inspector O la autodetección
        bool esVisorVR = forzarModoVR || UnityEngine.XR.XRSettings.isDeviceActive;

        if (!esVisorVR)
        {
            // 📱 MODO MÓVIL ADMIN
            GameObject miXR = GameObject.Find("XR_Origin_LOCAL");
            if (miXR != null) miXR.SetActive(false);

            if (panelInicioSimplificado) panelInicioSimplificado.SetActive(false);
            if (panelDevModo) panelDevModo.SetActive(false);
            if (panelColores) panelColores.SetActive(false);
            if (panelEspera) panelEspera.SetActive(false);

            if (camaraEspectadoraMovil) camaraEspectadoraMovil.gameObject.SetActive(true);
            if (canvasMobileAdmin) canvasMobileAdmin.SetActive(true);
            if (panelMovilBotonInicio) panelMovilBotonInicio.SetActive(true);
            if (panelMovilDashboard) panelMovilDashboard.SetActive(false);

            // 🌟 EL BOTÓN DE RESET SIEMPRE VISIBLE EN MÓVIL
            if (btnReiniciarSimulacion != null) btnReiniciarSimulacion.SetActive(true);

            string ipAuto = ObtenerIPLocalLAN();
            if (textoIPInicioMovil != null)
            {
                textoIPInicioMovil.text = $"IP Wi-Fi Detectada: <color=yellow><b>{ipAuto}</b></color>";
            }
        }
        else
        {
            // 🥽 MODO META QUEST VR
            Debug.Log("<color=green>[MODO CONFIGURADO]</color> Ejecutando en MODO VR (META QUEST).");

            GameObject miXR = GameObject.Find("XR_Origin_LOCAL");
            if (miXR != null) miXR.SetActive(true);

            if (canvasMobileAdmin) canvasMobileAdmin.SetActive(false);
            if (camaraEspectadoraMovil) camaraEspectadoraMovil.gameObject.SetActive(false);

            if (panelInicioSimplificado) panelInicioSimplificado.SetActive(true);
            if (panelDevModo) panelDevModo.SetActive(false);
            if (panelColores) panelColores.SetActive(false);
            if (panelEspera) panelEspera.SetActive(false);
        }

        if (btnContinuar != null) btnContinuar.interactable = false;
    }

    // === PROTECCIÓN DE RED: LIMPIEZA PREVIA DE SESIONES ===

    private void ReiniciarRedYEjecutar(System.Action accionInicio)
    {
        StartCoroutine(RutinaReiniciarRed(accionInicio));
    }

    private System.Collections.IEnumerator RutinaReiniciarRed(System.Action accionInicio)
    {
        // 1. Si la red está activa, ordenamos apagar
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
        {
            Debug.Log("<color=yellow>[RED]</color> Apagando sesión previa para reiniciar red limpiamente...");
            NetworkManager.Singleton.Shutdown();

            // 2. Esperamos a que Netcode termine de liberar los puertos en memoria
            while (NetworkManager.Singleton.IsListening)
            {
                yield return null;
            }

            // 3. Pausa de seguridad de 0.2 segundos
            yield return new WaitForSeconds(0.2f);
        }

        // 4. Ejecutamos la nueva conexión (StartClient / StartHost)
        accionInicio?.Invoke();
    }

    // === MÉTODOS DE INICIO DE CONEXIÓN ===

    // 1. Botón principal "JUGAR" (Pantalla simplificada VR)
    public void BTN_UsuarioVR_JugarDirecto()
    {
        ReiniciarRedYEjecutar(() =>
        {
            AplicarIPAlTransporte(ipFinalTrabajo);
            EnviarIdentificacionDispositivo("VR");
            NetworkManager.Singleton.StartClient();
            IrAPanelColores();
        });
    }

    // 2. Botón "INICIAR SERVIDOR OPERADOR" (Móvil Admin)
    public void BTN_IniciarHostAdminMovil()
    {
        ReiniciarRedYEjecutar(() =>
        {
            GameObject miXR = GameObject.Find("XR_Origin_LOCAL");
            if (miXR != null) miXR.SetActive(false);

            if (camaraEspectadoraMovil)
            {
                camaraEspectadoraMovil.gameObject.SetActive(true);
                MobileSpectatorCamera scriptCam = camaraEspectadoraMovil.GetComponent<MobileSpectatorCamera>();
                if (scriptCam != null) scriptCam.enabled = true;
            }

            if (canvasMobileAdmin) canvasMobileAdmin.SetActive(true);

            if (panelInicioSimplificado) panelInicioSimplificado.SetActive(false);
            if (panelDevModo) panelDevModo.SetActive(false);

            if (panelMovilBotonInicio) panelMovilBotonInicio.SetActive(false);
            if (panelMovilDashboard) panelMovilDashboard.SetActive(true);

            // 🌟 EVALUACIÓN DE IP: ¿Se escribió una IP manual de emergencia o usamos la automática?
            string miIP = (inputIPManualMovil != null && !string.IsNullOrEmpty(inputIPManualMovil.text))
                ? inputIPManualMovil.text.Trim()
                : ObtenerIPLocalLAN();

            AplicarIPAlTransporte(miIP);
            EnviarIdentificacionDispositivo("ADMIN");
            NetworkManager.Singleton.StartHost();

            if (textoEstadoServidorMovil != null)
            {
                textoEstadoServidorMovil.text = $"<color=green><b>● SERVIDOR OPERADOR ACTIVO</b></color>\n" +
                                                $"IP LAN: <color=yellow><b>{miIP}</b></color> | Puerto: {Puerto}\n\n" +
                                                $"<i>Las Meta Quest deben conectarse a esta IP.</i>";
            }

            Debug.Log($"<color=green>[HOST ADMIN]</color> Servidor activo en IP: {miIP}");
        });
    }

    // 3. Botón "HOST" del Panel Dev Secreto
    public void BTN_IniciarHost()
    {
        ReiniciarRedYEjecutar(() =>
        {
            EnviarIdentificacionDispositivo("VR");
            NetworkManager.Singleton.StartHost();
            IrAPanelColores();
        });
    }

    // 4. Botón "CLIENTE" del Panel Dev Secreto
    public void BTN_IniciarClienteVR()
    {
        ReiniciarRedYEjecutar(() =>
        {
            AplicarIPAlTransporte(ipFinalTrabajo);
            EnviarIdentificacionDispositivo("VR");
            NetworkManager.Singleton.StartClient();
            IrAPanelColores();
        });
    }

    public void ActualizarTextoIPEnPantalla(string ip)
    {
        ipFinalTrabajo = ip;
        ActualizarTextoIPUI();
    }

    public void ActualizarTextoIPUIExterno(string nuevaIP)
    {
        ipFinalTrabajo = nuevaIP;
        ActualizarTextoIPUI();
    }

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

    private string ObtenerIPLocalLAN()
    {
        try
        {
            var host = System.Net.Dns.GetHostEntry(System.Net.Dns.GetHostName());
            foreach (var ip in host.AddressList)
            {
                if (ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                {
                    return ip.ToString();
                }
            }
        }
        catch { }
        return "127.0.0.1";
    }

    // === PANEL DEV SECRETO ===

    public void ActivarMenuModoDev()
    {
        if (panelInicioSimplificado) panelInicioSimplificado.SetActive(false);
        if (panelDevModo) panelDevModo.SetActive(true);
        Debug.Log("<color=yellow>[DEV MODE]</color> Panel secreto desbloqueado.");
    }

    // === CONFIGURACIÓN Y APROBACIÓN NETCODE ===

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
        if (panelInicioSimplificado) panelInicioSimplificado.SetActive(false);
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

    // === SELECCIÓN DE COLORES Y RPCs ===

    public void BTN_SeleccionarColor(int indiceColor)
    {
        if (!IsSpawned || NetworkManager.Singleton == null || (!NetworkManager.Singleton.IsConnectedClient && !IsServer))
        {
            Debug.LogWarning("<color=yellow>[ALERTA RED]</color> No estás conectado a ningún Host/Servidor activo.");
            return;
        }

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

        if (btnRojo) btnRojo.interactable = (dueñoRojo.Value == 999 || dueñoRojo.Value == miID);
        if (btnAzul) btnAzul.interactable = (dueñoAzul.Value == 999 || dueñoAzul.Value == miID);
        if (btnVerde) btnVerde.interactable = (dueñoVerde.Value == 999 || dueñoVerde.Value == miID);
        if (btnAmarillo) btnAmarillo.interactable = (dueñoAmarillo.Value == 999 || dueñoAmarillo.Value == miID);
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

    // === AVANCE A SALA DE ESPERA E INICIO DE JUEGO ===

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

    // Invocado al pulsar "INICIAR SIMULACIÓN"
    //public void BTN_HostIniciarJuego()
    //{
    //    if (!IsServer) return;

    //    // 1. Ocultar la interfaz del dashboard en la pantalla del móvil para dejar la vista 3D limpia
    //    if (panelMovilDashboard != null) panelMovilDashboard.SetActive(false);
    //    if (canvasMobileAdmin != null) canvasMobileAdmin.SetActive(false);

    //    // 2. Avisar a las Meta Quest de que arranquen la simulación
    //    CerrarLobbyEnTodosLosClientesClientRpc();

    //    // 3. Arrancar la lógica principal del juego
    //    if (MainGameManager.Instance != null) MainGameManager.Instance.IniciarExperienciaDesdeLobby();

    //    Debug.Log("<color=green>[SIMULACIÓN]</color> Partida iniciada por el Operador. Pantalla limpia para la vista espectadora.");
    //}

    public void BTN_HostIniciarJuego()
    {
        if (!IsServer) return;

        if (canvasMobileAdmin != null) canvasMobileAdmin.SetActive(true);
        if (panelMovilDashboard != null) panelMovilDashboard.SetActive(false);

        // Activamos el botón flotante de reiniciar en la pantalla del móvil
        if (btnReiniciarSimulacion != null) btnReiniciarSimulacion.SetActive(true);

        // Activamos la cámara con gestos táctiles
        if (camaraEspectadoraMovil != null)
        {
            MobileSpectatorCamera scriptCam = camaraEspectadoraMovil.GetComponent<MobileSpectatorCamera>();
            if (scriptCam != null) scriptCam.enabled = true;
        }

        CerrarLobbyEnTodosLosClientesClientRpc();

        if (MainGameManager.Instance != null) MainGameManager.Instance.IniciarExperienciaDesdeLobby();

        Debug.Log("<color=green>[SIMULACIÓN]</color> Partida iniciada. Gestos táctiles activos.");
    }

    [ClientRpc]
    private void CerrarLobbyEnTodosLosClientesClientRpc()
    {
        if (panelInicioSimplificado) panelInicioSimplificado.SetActive(false);
        if (panelDevModo) panelDevModo.SetActive(false);
        if (panelColores) panelColores.SetActive(false);
        if (panelEspera) panelEspera.SetActive(false);
    }

    // 🌟 AQUÍ PEGAS TU NUEVO CÓDIGO 🌟

    // Invocado al pulsar "🔄 REINICIAR LOBBY" en el móvil
    public void BTN_ReiniciarSimulacionAdmin()
    {
        if (!IsServer) return;

        // 1. Liberar todas las selecciones de colores en el servidor
        dueñoRojo.Value = 999;
        dueñoAzul.Value = 999;
        dueñoVerde.Value = 999;
        dueñoAmarillo.Value = 999;

        // 2. Mandar orden a todos los visores y al móvil para reiniciar sus UIs
        ReiniciarSimulacionClientRpc();

        Debug.Log("<color=yellow>[REINICIO]</color> El operador ha reiniciado la partida.");
    }

    [ClientRpc]
    private void ReiniciarSimulacionClientRpc()
    {
        colorSeleccionadoLocal = -1;

        bool esVisorVR = forzarModoVR || UnityEngine.XR.XRSettings.isDeviceActive;

        if (!esVisorVR)
        {
            // 📱 EN EL MÓVIL: Vuelve al Dashboard inicial y MANTIENE el botón de Reset activo
            if (panelMovilDashboard != null) panelMovilDashboard.SetActive(true);
            if (btnReiniciarSimulacion != null) btnReiniciarSimulacion.SetActive(true); // 👈 Mantener activo
        }
        else
        {
            // 🥽 EN LAS META QUEST: Vuelven a la pantalla de Selección de Color
            if (panelInicioSimplificado) panelInicioSimplificado.SetActive(false);
            if (panelDevModo) panelDevModo.SetActive(false);
            if (panelColores) panelColores.SetActive(true);
            if (panelEspera) panelEspera.SetActive(false);
            if (btnContinuar != null) btnContinuar.interactable = false;
        }
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
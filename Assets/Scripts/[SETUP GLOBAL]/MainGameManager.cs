using UnityEngine;
using System.Collections;
using UnityEngine.SceneManagement;
using Unity.Netcode;
using System.Linq;
using TMPro;

public class MainGameManager : NetworkBehaviour
{
    public static MainGameManager Instance;

    public enum EstadoJuego { EsperandoLobby, FaseIntroRobot, FaseInstrucciones, FaseGameplay, FaseVictoria }

    public NetworkVariable<EstadoJuego> estadoActual = new NetworkVariable<EstadoJuego>(
        EstadoJuego.EsperandoLobby, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    public NetworkVariable<int> jugadoresListos = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    [Header("Bloques de Escena")]
    public GameObject bloqueInicio;
    public GameObject bloqueRobot;
    public GameObject bloqueInstrucciones;
    public GameObject bloqueGameplay;
    public GameObject bloqueVictoria;
    public GameObject entornoVR;

    [Header("Referencias Robot y Audio")]
    public Animator robotAnimator;
    public AudioSource audioSource;
    public AudioClip audioIntroduccion;
    public AudioClip audioInstrucciones;
    public AudioClip audioVictoria;

    [Header("UI Jugador / Ready Check")]
    public GameObject botonJugarIndividual;
    public TextMeshProUGUI textoContadorListos; // El texto que dirá "0/2", "1/2", etc.

    [Header("UI Final / Podio")]
    public TextMeshProUGUI textoResultados;
    public GameObject botonReiniciarHost;

    public float tiempoEntrada = 5.0f;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            // 🌟 FUSIÓN: Solo el mánager único y original sobrevive al cambio de escena
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            // Los clones duplicados que intenten colarse al recargar el mapa se eliminan en el acto
            Destroy(gameObject);
        }
    }

    public override void OnNetworkSpawn()
    {
        estadoActual.OnValueChanged += AlCambiarEstadoGlobal;
        jugadoresListos.OnValueChanged += (viejo, nuevo) => ActualizarTextoListosVisual(nuevo);

        if (estadoActual.Value == EstadoJuego.FaseInstrucciones)
        {
            EjecutarFaseInstruccionesLocal();
        }
        else if (estadoActual.Value == EstadoJuego.FaseGameplay)
        {
            SaltarDirectoAGameplayLocal();
        }
    }

    public void IniciarExperienciaDesdeLobby()
    {
        if (IsServer) estadoActual.Value = EstadoJuego.FaseIntroRobot;
    }

    private void AlCambiarEstadoGlobal(EstadoJuego estadoAnterior, EstadoJuego estadoNuevo)
    {
        switch (estadoNuevo)
        {
            case EstadoJuego.FaseIntroRobot: EjecutarFaseIntroLocal(); break;
            case EstadoJuego.FaseInstrucciones: EjecutarFaseInstruccionesLocal(); break;
            case EstadoJuego.FaseGameplay: EjecutarTransicionAVRLocal(); break;
            case EstadoJuego.FaseVictoria: EjecutarVictoriaLocal(); break;
        }
    }

    private void EjecutarFaseIntroLocal()
    {
        if (bloqueInicio) bloqueInicio.SetActive(false);
        if (bloqueRobot) bloqueRobot.SetActive(true);
        StartCoroutine(SecuenciaIntroCorrutina());
    }

    IEnumerator SecuenciaIntroCorrutina()
    {
        if (robotAnimator) robotAnimator.SetTrigger("Trig_Entrar");
        yield return new WaitForSeconds(tiempoEntrada);

        if (audioSource && audioIntroduccion)
        {
            audioSource.clip = audioIntroduccion;
            audioSource.Play();
            if (robotAnimator) robotAnimator.SetBool("EsHablando", true);
            yield return new WaitForSeconds(audioIntroduccion.length);
            if (robotAnimator) robotAnimator.SetBool("EsHablando", false);
        }

        if (IsServer)
        {
            estadoActual.Value = EstadoJuego.FaseInstrucciones;
            ForzarFaseInstruccionesEnClientesClientRpc();
        }
    }

    [ClientRpc]
    private void ForzarFaseInstruccionesEnClientesClientRpc()
    {
        EjecutarFaseInstruccionesLocal();
    }

    private void EjecutarFaseInstruccionesLocal()
    {
        if (bloqueInstrucciones) bloqueInstrucciones.SetActive(true);

        if (botonJugarIndividual != null)
        {
            botonJugarIndividual.SetActive(true);
            UnityEngine.UI.Button btnComp = botonJugarIndividual.GetComponent<UnityEngine.UI.Button>();
            if (btnComp != null) btnComp.interactable = true;
        }

        if (textoContadorListos != null)
        {
            textoContadorListos.gameObject.SetActive(true);
        }

        StartCoroutine(RefrescarTextoInstruccionesConRetraso());

        if (audioInstrucciones && audioSource)
        {
            audioSource.clip = audioInstrucciones;
            audioSource.Play();
        }
    }

    private IEnumerator RefrescarTextoInstruccionesConRetraso()
    {
        yield return new WaitForSeconds(0.2f);
        ActualizarTextoListosVisual(jugadoresListos.Value);
    }

    private void ActualizarTextoListosVisual(int listos)
    {
        if (textoContadorListos != null && NetworkManager.Singleton != null)
        {
            int totalJugadoresVR = 0;
            foreach (var client in NetworkManager.Singleton.ConnectedClientsList)
            {
                if (client.PlayerObject != null) totalJugadoresVR++;
            }

            if (totalJugadoresVR == 0) totalJugadoresVR = NetworkManager.Singleton.ConnectedClientsIds.Count;

            textoContadorListos.text = $"Jugadores listos: {listos} / {totalJugadoresVR}";
            Debug.Log($"[READY CHECK] UI: {listos} de {totalJugadoresVR} jugadores VR.");
        }
    }

    public void BTN_JugadorListo()
    {
        if (botonJugarIndividual != null) botonJugarIndividual.SetActive(false);
        AumentarContadorListosServerRpc();
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    private void AumentarContadorListosServerRpc()
    {
        jugadoresListos.Value++;

        int totalJugadoresVR = 0;
        foreach (var client in NetworkManager.Singleton.ConnectedClientsList)
        {
            if (client.PlayerObject != null) totalJugadoresVR++;
        }

        if (jugadoresListos.Value >= totalJugadoresVR)
        {
            estadoActual.Value = EstadoJuego.FaseGameplay;
        }
    }

    private void EjecutarTransicionAVRLocal()
    {
        if (textoContadorListos != null) textoContadorListos.gameObject.SetActive(false);
        if (bloqueInstrucciones) bloqueInstrucciones.SetActive(false);
        StartCoroutine(SecuenciaTransicionAVR());
    }

    IEnumerator SecuenciaTransicionAVR()
    {
        if (robotAnimator) robotAnimator.SetTrigger("Trig_Jugar");
        yield return new WaitForSeconds(2.0f);
        SaltarDirectoAGameplayLocal();
    }

    private void SaltarDirectoAGameplayLocal()
    {
        if (bloqueRobot) bloqueRobot.SetActive(false);
        ActivarModoVR();
        if (entornoVR) entornoVR.SetActive(true);
        if (bloqueGameplay) bloqueGameplay.SetActive(true);

        if (IsServer && GameplayManager.Instance != null)
        {
            GameplayManager.Instance.IniciarPartida();
        }
    }

    public void FinalizarExperienciaCompleta()
    {
        if (IsServer) estadoActual.Value = EstadoJuego.FaseVictoria;
    }

    private void EjecutarVictoriaLocal()
    {
        if (audioVictoria && audioSource) audioSource.PlayOneShot(audioVictoria);
        if (bloqueGameplay) bloqueGameplay.SetActive(false);
        if (bloqueVictoria) bloqueVictoria.SetActive(true);

        GenerarPodioDeJugadores();

        if (botonReiniciarHost != null)
        {
            botonReiniciarHost.SetActive(IsServer);
        }
    }
    private void GenerarPodioDeJugadores()
    {
        if (textoResultados == null) return;

        // Cabecera limpia y estilizada al estilo de tu menú de conectados
        string podioText = "<size=110%><b>PODIO DE LA SIMULACIÓN:</b></size>\n\n";

        // 1. Filtramos los clientes conectados con un avatar físico real
        var listaJugadoresValidos = new System.Collections.Generic.List<NetworkClient>();
        foreach (var cliente in NetworkManager.Singleton.ConnectedClientsList)
        {
            if (cliente.PlayerObject != null)
            {
                listaJugadoresValidos.Add(cliente);
            }
        }

        // 2. Ordenamos el podio por puntuación de mayor a menor leyendo desde PlayerAvatarSync
        var jugadoresOrdenados = listaJugadoresValidos.OrderByDescending(c => {
            PlayerNetworkState estado = c.PlayerObject.GetComponent<PlayerNetworkState>();
            return estado != null ? estado.puntuacion.Value : 0;
        }).ToList();

        int puesto = 1;
        foreach (var cliente in jugadoresOrdenados)
        {
            PlayerNetworkState estado = cliente.PlayerObject.GetComponent<PlayerNetworkState>();
            if (estado != null)
            {
                string nombreColorTexto = "VR";
                string colorTag = "white";

                if (LobbyManager.Instance != null)
                {
                    Color colorRealDelJugador = LobbyManager.Instance.ObtenerColorPorID(cliente.ClientId);
                    if (colorRealDelJugador == Color.red) { colorTag = "red"; nombreColorTexto = "Rojo"; }
                    else if (colorRealDelJugador == Color.blue) { colorTag = "blue"; nombreColorTexto = "Azul"; }
                    else if (colorRealDelJugador == Color.green) { colorTag = "green"; nombreColorTexto = "Verde"; }
                    else if (colorRealDelJugador == Color.yellow) { colorTag = "yellow"; nombreColorTexto = "Amarillo"; }
                }

                podioText += $"<size=130%><color={colorTag}>■</color></size>  <color=white><b>Puesto {puesto}</b>  -  Jugador VR ({nombreColorTexto}):  <b>{estado.puntuacion.Value} pts</b></color>\n\n";
            }
            puesto++;
        }

        // 4. Inyectamos el string definitivo en el Canvas del podio final
        textoResultados.text = podioText;
    }

    public void BTN_Host_ReiniciarJuego()
    {
        if (IsServer)
        {
            NetworkManager.Singleton.SceneManager.LoadScene(SceneManager.GetActiveScene().name, LoadSceneMode.Single);
        }
    }

    void ActivarModoVR()
    {
        Camera camaraLocal = Camera.main;
        if (camaraLocal != null) camaraLocal.clearFlags = CameraClearFlags.Skybox;
    }

    public void Debug_SaltarAlGameplay()
    {
        CancelInvoke();
        StopAllCoroutines();

        if (audioSource != null) audioSource.Stop();
        if (bloqueRobot != null) bloqueRobot.SetActive(false);
        if (bloqueInicio != null) bloqueInicio.SetActive(false);
        if (bloqueInstrucciones != null) bloqueInstrucciones.SetActive(false);
        if (bloqueVictoria != null) bloqueVictoria.SetActive(false);

        if (entornoVR) entornoVR.SetActive(true);
        if (bloqueGameplay) bloqueGameplay.SetActive(true);

        if (GameplayManager.Instance != null)
        {
            GameplayManager.Instance.IniciarPartida();
        }
    }

    // 🌟 MÉTODO DE RESETEO TOTAL A ESTADO CERO
    public void ReiniciarJuego()
    {
        // 1. Detener corrutinas y audios activos
        StopAllCoroutines();
        CancelInvoke();
        if (audioSource != null) audioSource.Stop();

        // 2. 🌟 ENCENDER EL BLOQUE 1 (Contiene el Canvas con todos los paneles VR)
        if (bloqueInicio != null) bloqueInicio.SetActive(true);

        // 3. Desactivar los bloques secundarios de la simulación 3D
        if (bloqueRobot != null) bloqueRobot.SetActive(false);
        if (bloqueInstrucciones != null) bloqueInstrucciones.SetActive(false);
        if (bloqueGameplay != null) bloqueGameplay.SetActive(false);
        if (bloqueVictoria != null) bloqueVictoria.SetActive(false);
        if (entornoVR != null) entornoVR.SetActive(false);

        // 4. Si eres el Servidor, resetear las variables de red
        if (IsServer)
        {
            estadoActual.Value = EstadoJuego.EsperandoLobby;
            jugadoresListos.Value = 0;
        }

        // 5. Mandar a limpiar las pistolas y asteroides a GameplayManager
        if (GameplayManager.Instance != null)
        {
            GameplayManager.Instance.LimpiarGameplayParaReset();
        }

        Debug.Log("<color=cyan>[MAIN GAME MANAGER]</color> Bloque 1 reactivado y proyecto restablecido a estado cero.");
    }
}
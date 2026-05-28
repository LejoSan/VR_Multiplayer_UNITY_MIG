using UnityEngine;
using System.Collections;
using UnityEngine.SceneManagement;
using Unity.Netcode;
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
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    // 🌟 REESTRUCTURADO: Los clientes y el host se preparan aquí para escuchar la red
    public override void OnNetworkSpawn()
    {
        estadoActual.OnValueChanged += AlCambiarEstadoGlobal;
        jugadoresListos.OnValueChanged += (viejo, nuevo) => ActualizarTextoListosVisual(nuevo);

        // SEGURO DE RED: Si el cliente entra tarde y el juego ya avanzó de fase, se auto-sincroniza
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

    // --- FASE 1: INTRO (Flujo directo automatizado) ---
    private void EjecutarFaseIntroLocal()
    {
        if (bloqueInicio) bloqueInicio.SetActive(false);
        if (bloqueRobot) bloqueRobot.SetActive(true);
        StartCoroutine(SecuenciaIntroCorrutina());
    }

    IEnumerator SecuenciaIntroCorrutina()
    {
        // 1. Animación de entrada del Robot
        if (robotAnimator) robotAnimator.SetTrigger("Trig_Entrar");
        yield return new WaitForSeconds(tiempoEntrada);

        // 2. Audio de Introducción
        if (audioSource && audioIntroduccion)
        {
            audioSource.clip = audioIntroduccion;
            audioSource.Play();
            if (robotAnimator) robotAnimator.SetBool("EsHablando", true);
            yield return new WaitForSeconds(audioIntroduccion.length);
            if (robotAnimator) robotAnimator.SetBool("EsHablando", false);
        }

        // 🌟 EL GRAN CAMBIO AUTOMÁTICO: Cuando termina la intro, el servidor cambia el estado global 
        // e invoca un RPC directo para despertar las instrucciones en todas las gafas a la vez.
        if (IsServer)
        {
            estadoActual.Value = EstadoJuego.FaseInstrucciones;
            ForzarFaseInstruccionesEnClientesClientRpc();
        }
    }

    [ClientRpc]
    private void ForzarFaseInstruccionesEnClientesClientRpc()
    {
        // Despierta localmente la fase en los clientes evitando la trampa del retraso por tiempo
        EjecutarFaseInstruccionesLocal();
    }

    // --- FASE 2: INSTRUCCIONES Y READY CHECK ---
    private void EjecutarFaseInstruccionesLocal()
    {
        // Encendemos el bloque de instrucciones para todos
        if (bloqueInstrucciones) bloqueInstrucciones.SetActive(true);

        // Forzamos que el botón "¡JUGUEMOS!" sea visible y cliqueable por el láser de todos los visores
        if (botonJugarIndividual != null)
        {
            botonJugarIndividual.SetActive(true);
            UnityEngine.UI.Button btnComp = botonJugarIndividual.GetComponent<UnityEngine.UI.Button>();
            if (btnComp != null) btnComp.interactable = true;
        }

        // Activamos e inicializamos el texto en pantalla (Ej: 0 / 2)
        if (textoContadorListos != null)
        {
            textoContadorListos.gameObject.SetActive(true);
        }

        // Damos un margen de 200ms para procesar los avatares en red y pintar el número correcto
        StartCoroutine(RefrescarTextoInstruccionesConRetraso());

        // Audio de las instrucciones
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

            // Seguro por si el cliente lee la red antes de auto-registrarse
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

    // --- FASE 3: GAMEPLAY ---
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

    // --- FASE 4: VICTORIA Y REINICIO ---
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

        string podioText = "<size=120%>PUNTUACIONES FINALES</size>\n\n";

        foreach (var cliente in NetworkManager.Singleton.ConnectedClientsList)
        {
            if (cliente.PlayerObject != null)
            {
                PlayerNetworkState estado = cliente.PlayerObject.GetComponent<PlayerNetworkState>();
                if (estado != null)
                {
                    Color colorJugador = new Color(estado.colorJugadorNet.Value.x, estado.colorJugadorNet.Value.y, estado.colorJugadorNet.Value.z);
                    string hexColor = ColorUtility.ToHtmlStringRGB(colorJugador);
                    podioText += $"<color=#{hexColor}>■</color> Jugador {(cliente.ClientId == 0 ? "Líder" : cliente.ClientId.ToString())}: <b>{estado.puntuacion.Value} pts</b>\n";
                }
            }
        }

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
}
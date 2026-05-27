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

    // ¡NUEVO! Contador de jugadores que le han dado al botón "Jugar" en las instrucciones
    public NetworkVariable<int> jugadoresListos = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    [Header("Bloques de Escena")]
    public GameObject bloqueInicio;
    public GameObject bloqueRobot;
    public GameObject bloqueUI_Decision;
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

    [Header("UI Jugador")]
    public GameObject botonJugarIndividual; // El botón que pulsará cada jugador en la fase de instrucciones

    [Header("UI Final / Podio")]
    public TextMeshProUGUI textoResultados; // El texto donde saldrá la lista de puntos
    public GameObject botonReiniciarHost;   // El botón de Volver a Jugar

    public float tiempoEntrada = 5.0f;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public override void OnNetworkSpawn()
    {
        estadoActual.OnValueChanged += AlCambiarEstadoGlobal;

        if (estadoActual.Value == EstadoJuego.FaseGameplay)
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

    // --- FASE 1: INTRO ---
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
            yield return new WaitForEndOfFrame();
            if (robotAnimator) robotAnimator.SetBool("EsHablando", false);
            yield return new WaitForSeconds(audioIntroduccion.length);
        }

        if (IsServer && bloqueUI_Decision) bloqueUI_Decision.SetActive(true);
    }

    public void BTN_Host_ContinuarAInstrucciones()
    {
        if (IsServer) estadoActual.Value = EstadoJuego.FaseInstrucciones;
    }

    // --- FASE 2: INSTRUCCIONES Y READY CHECK ---
    private void EjecutarFaseInstruccionesLocal()
    {
        if (bloqueUI_Decision) bloqueUI_Decision.SetActive(false);
        if (bloqueInstrucciones) bloqueInstrucciones.SetActive(true);
        if (botonJugarIndividual) botonJugarIndividual.SetActive(true); // Se le muestra a cada jugador

        if (audioInstrucciones && audioSource)
        {
            audioSource.clip = audioInstrucciones;
            audioSource.Play();
        }
    }

    // ¡NUEVO! Cada jugador pulsa este botón con su láser VR
    public void BTN_JugadorListo()
    {
        if (botonJugarIndividual) botonJugarIndividual.SetActive(false); // Lo ocultamos para que no le dé 2 veces
        AumentarContadorListosServerRpc();
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    private void AumentarContadorListosServerRpc()
    {
        jugadoresListos.Value++;

        // Contamos cuántos jugadores VR reales hay (ignorando al admin móvil si lo hubiera)
        int jugadoresVR = 0;
        foreach (var client in NetworkManager.Singleton.ConnectedClientsList)
        {
            if (client.PlayerObject != null) jugadoresVR++;
        }

        // Si todos los VR le han dado al botón, ¡arrancamos!
        if (jugadoresListos.Value >= jugadoresVR)
        {
            estadoActual.Value = EstadoJuego.FaseGameplay;
        }
    }

    // --- FASE 3: GAMEPLAY ---
    private void EjecutarTransicionAVRLocal()
    {
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

        // 1. Generar la lista de puntuaciones
        GenerarPodioDeJugadores();

        // 2. Control exclusivo del botón para el Host
        if (botonReiniciarHost != null)
        {
            botonReiniciarHost.SetActive(IsServer);
        }
    }

    private void GenerarPodioDeJugadores()
    {
        if (textoResultados == null) return;

        string podioText = "<size=120%>PUNTUACIONES FINALES</size>\n\n";

        // Recorremos a todos los jugadores que están conectados en la partida
        foreach (var cliente in NetworkManager.Singleton.ConnectedClientsList)
        {
            if (cliente.PlayerObject != null)
            {
                PlayerNetworkState estado = cliente.PlayerObject.GetComponent<PlayerNetworkState>();
                if (estado != null)
                {
                    // Convertimos su Vector4 de color a un color real
                    Color colorJugador = new Color(estado.colorJugadorNet.Value.x, estado.colorJugadorNet.Value.y, estado.colorJugadorNet.Value.z);
                    string hexColor = ColorUtility.ToHtmlStringRGB(colorJugador);

                    // Añadimos al texto: "■ Jugador X: 150 pts" pintado de su color
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

    // --- ZONA DE DESARROLLADOR (DEBUG) ---

    public void Debug_SaltarAlGameplay()
    {
        // 1. Cancelar cualquier Invoke pendiente (para que no salte la intro tarde)
        CancelInvoke();
        StopAllCoroutines();

        // 2. Callar al Robot y ocultarlo
        if (audioSource != null) audioSource.Stop();
        if (bloqueRobot != null) bloqueRobot.SetActive(false);
        if (bloqueInicio != null) bloqueInicio.SetActive(false);
        if (bloqueInstrucciones != null) bloqueInstrucciones.SetActive(false);
        if (bloqueVictoria != null) bloqueVictoria.SetActive(false);

        // 3. Activar el Entorno VR (Si lo tienes separado)
        if (entornoVR != null) entornoVR.SetActive(true);

        // 4. Activar bloque Gameplay
        if (bloqueGameplay != null) bloqueGameplay.SetActive(true);

        // 5. ¡ARRANCAR EL JUEGO YA!
        if (GameplayManager.Instance != null)
        {
            GameplayManager.Instance.IniciarPartida();
        }
        else
        {
            Debug.LogError("No encuentro el GameplayManager para iniciar.");
        }
    }
}
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

    [Header("--- Efecto Dissolve / Materialización ---")]
    public EnvironmentDissolveController dissolveController;
    public float tiempoAnimacionPreviaDisparo = 3.0f; // Tiempo de respaldo si no hay cuenta regresiva

    [Header("--- Cuenta Regresiva VR (3, 2, 1) ---")]
    public CuentaRegresivaVR cuentaRegresiva;

    [Header("--- Audio Locución Final ---")]
    public AudioClip audioVamosADisparar;

    //void Awake()
    //{
    //    if (Instance == null)
    //    {
    //        Instance = this;
    //        // 🌟 FUSIÓN: Solo el mánager único y original sobrevive al cambio de escena
    //        DontDestroyOnLoad(gameObject);
    //    }
    //    else
    //    {
    //        // Los clones duplicados que intenten colarse al recargar el mapa se eliminan en el acto
    //        Destroy(gameObject);
    //    }
    //}
    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);

            // 🌟 SOLUCIÓN DE AUDIO: Garantiza un AudioSource propio en GameManager que NUNCA se desactiva
            AudioSource audioPropio = GetComponent<AudioSource>();
            if (audioPropio == null)
            {
                audioPropio = gameObject.AddComponent<AudioSource>();
            }
            audioSource = audioPropio;
        }
        else
        {
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
        // 1. Animación de salida del Robot
        if (robotAnimator) robotAnimator.SetTrigger("Trig_Jugar");
        yield return new WaitForSeconds(2.0f);

        // 2. Apagar elementos de inicio
        if (bloqueRobot) bloqueRobot.SetActive(false);
        if (bloqueInicio) bloqueInicio.SetActive(false);

        // 3. Activar el entorno VR y el modo de vista VR
        ActivarModoVR();
        if (entornoVR) entornoVR.SetActive(true);
        if (bloqueGameplay) bloqueGameplay.SetActive(true);

        // -------------------------------------------------------------
        // 🌟 PASO 1: GENERACIÓN DEL MUNDO (Shader + SFX del mundo)
        // -------------------------------------------------------------
        if (dissolveController != null)
        {
            dissolveController.ResetearAInvisibilidad();
            dissolveController.IniciarAparicionGradual();

            // Esperamos los segundos exactos que dura la animación del shader
            yield return new WaitForSeconds(dissolveController.duracionAparicion);
        }

        // 🛑 PAUSA DE 1 SEGUNDO ENTRE MUNDO Y CONTEO
        yield return new WaitForSeconds(1.0f);

        // -------------------------------------------------------------
        // 🌟 PASO 2: CONTEO 3, 2, 1 (1 segundo por cada número con Beep)
        // -------------------------------------------------------------
        if (cuentaRegresiva != null)
        {
            // 🔧 Enciende explícitamente el GameObject de la cuenta atrás desde MainGameManager
            cuentaRegresiva.gameObject.SetActive(true);

            yield return StartCoroutine(cuentaRegresiva.RutinaCuentaRegresiva());
        }

        //// -------------------------------------------------------------
        //// 🌟 PASO 3: LOCUCIÓN "MAYRIT VAMOS A DISPARAR"
        //// -------------------------------------------------------------
        //if (audioVamosADisparar != null)
        //{
        //    // Si por alguna razón el AudioSource fallara, usamos PlayClipAtPoint como respaldo indestructible
        //    if (audioSource != null && audioSource.enabled && audioSource.gameObject.activeInHierarchy)
        //    {
        //        audioSource.PlayOneShot(audioVamosADisparar);
        //    }
        //    else
        //    {
        //        Vector3 posCamara = Camera.main != null ? Camera.main.transform.position : Vector3.zero;
        //        AudioSource.PlayClipAtPoint(audioVamosADisparar, posCamara);
        //    }

        //    Debug.Log("<color=green>[AUDIO SUCCESS]</color> Reproduciendo locución: Vamos a disparar.");

        //    // Esperamos los segundos exactos de la voz
        //    yield return new WaitForSeconds(audioVamosADisparar.length);
        //}
        //else
        //{
        //    Debug.LogWarning("<color=orange>[AUDIO ALERTA]</color> Falta asignar 'Audio Vamos A Disparar' en el Inspector.");
        //    yield return new WaitForSeconds(1.0f);
        //}

        //// -------------------------------------------------------------
        //// 🌟 PASO 4: ¡EMPIEZA EL JUEGO Y LOS DISPAROS!
        //// -------------------------------------------------------------
        //if (IsServer && GameplayManager.Instance != null)
        //{
        //    GameplayManager.Instance.IniciarPartida();
        //}
        // -------------------------------------------------------------
        // 🌟 PASO 3: LOCUCIÓN "MAYRIT VAMOS A DISPARAR"
        // -------------------------------------------------------------
        if (audioVamosADisparar != null)
        {
            if (audioSource != null && audioSource.enabled && audioSource.gameObject.activeInHierarchy)
            {
                audioSource.PlayOneShot(audioVamosADisparar);
            }
            else
            {
                Vector3 posCamara = Camera.main != null ? Camera.main.transform.position : Vector3.zero;
                AudioSource.PlayClipAtPoint(audioVamosADisparar, posCamara);
            }

            yield return new WaitForSeconds(audioVamosADisparar.length);
        }

        // -------------------------------------------------------------
        // 🌟 PASO 4: CAMBIO A MANOS E INICIO DE PARTIDA
        // -------------------------------------------------------------
        // 🖐️ Cambiamos la visualización de los Mandos a las Manos justo al iniciar los disparos
        if (VRHandControllerSwitcher.Instance != null)
        {
            VRHandControllerSwitcher.Instance.ActivarModoManos();
        }

        if (IsServer && GameplayManager.Instance != null)
        {
            GameplayManager.Instance.IniciarPartida();
        }
    }

    private void SaltarDirectoAGameplayLocal()
    {
        if (bloqueRobot) bloqueRobot.SetActive(false);
        ActivarModoVR();
        if (entornoVR) entornoVR.SetActive(true);
        if (bloqueGameplay) bloqueGameplay.SetActive(true);

        if (dissolveController != null)
        {
            dissolveController.IniciarAparicionGradual();
        }

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

        string podioText = "<size=110%><b>PODIO DE LA SIMULACIÓN:</b></size>\n\n";

        // 1. Filtramos los clientes conectados con un avatar VR físico real
        var listaJugadoresValidos = new System.Collections.Generic.List<NetworkClient>();
        if (NetworkManager.Singleton != null)
        {
            foreach (var cliente in NetworkManager.Singleton.ConnectedClientsList)
            {
                if (cliente.PlayerObject != null)
                {
                    listaJugadoresValidos.Add(cliente);
                }
            }
        }

        // 2. Ordenamos el podio por puntuación de mayor a menor leyendo desde PlayerNetworkState
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
                string hexColor = "FFFFFF";

                if (LobbyManager.Instance != null)
                {
                    Color colorReal = LobbyManager.Instance.ObtenerColorPorID(cliente.ClientId);
                    hexColor = ColorUtility.ToHtmlStringRGB(colorReal);

                    if (colorReal == Color.red) nombreColorTexto = "Rojo";
                    else if (colorReal == Color.blue) nombreColorTexto = "Azul";
                    else if (colorReal == Color.green) nombreColorTexto = "Verde";
                    else if (colorReal == Color.yellow) nombreColorTexto = "Amarillo";
                    else if (colorReal == (Color)new Color32(245, 128, 39, 255) || colorReal == new Color(1.0f, 0.5f, 0.0f)) nombreColorTexto = "Naranja";
                    else if (colorReal == new Color(0.5f, 0.0f, 0.5f)) nombreColorTexto = "Morado";
                }

                podioText += $"<size=130%><color=#{hexColor}>■</color></size>  <color=white><b>Puesto {puesto}</b>  -  Jugador VR ({nombreColorTexto}):  <b>{estado.puntuacion.Value} pts</b></color>\n\n";
            }
            puesto++;
        }

        // 3. Inyectamos el texto en el Canvas final
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

        if (dissolveController != null)
        {
            dissolveController.IniciarAparicionGradual();
        }

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

        // Al reiniciar a estado cero, volvemos a poner los mandos/joysticks iniciales
        if (VRHandControllerSwitcher.Instance != null)
        {
            VRHandControllerSwitcher.Instance.ActivarModoMandos();
        }

        Debug.Log("<color=cyan>[MAIN GAME MANAGER]</color> Bloque 1 reactivado y proyecto restablecido a estado cero.");
    }
}
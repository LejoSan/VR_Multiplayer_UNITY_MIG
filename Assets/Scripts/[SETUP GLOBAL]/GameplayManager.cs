using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;

public class GameplayManager : NetworkBehaviour
{
    public static GameplayManager Instance;

    [Header("--- Configuración de la Partida ---")]
    public float tiempoDeJuego = 30f;
    public NetworkVariable<float> tiempoRestanteNet = new NetworkVariable<float>(30f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    private bool juegoActivo = false;

    [Header("--- Entorno ---")]
    public GameObject entornoJuego;

    [Header("--- Spawners de Asteroides (Host-Only) ---")]
    public GameObject[] listaDeObjetivos;
    public Transform contenedorSpawners;
    public float intervaloSpawn = 1.5f;
    private List<Transform> puntosDeSpawnAsteroides = new List<Transform>();

    [Header("--- Puntos de Spawn JUGADORES ---")]
    public Transform contenedorSpawnJugadores;
    private List<Transform> puntosDeSpawnJugadores = new List<Transform>();

    [Header("--- Arma de los Jugadores ---")]
    public GameObject prefabArma;
    private List<NetworkObject> armasSpawneadas = new List<NetworkObject>();
    private List<WeaponDisplay> pantallasDeArmas = new List<WeaponDisplay>();

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        // Llenamos los puntos de spawn de asteroides
        if (contenedorSpawners != null)
        {
            foreach (Transform child in contenedorSpawners) puntosDeSpawnAsteroides.Add(child);
        }

        // Llenamos los puntos de spawn de jugadores (Asegúrate de tener 6 objetos hijos en Unity)
        if (contenedorSpawnJugadores != null)
        {
            foreach (Transform child in contenedorSpawnJugadores) puntosDeSpawnJugadores.Add(child);
        }
    }

    public void RegistrarPantallaArma(WeaponDisplay display)
    {
        if (!pantallasDeArmas.Contains(display))
        {
            pantallasDeArmas.Add(display);
            display.ActualizarTiempo(Mathf.CeilToInt(tiempoRestanteNet.Value));
        }
    }

    // --- INICIO DEL JUEGO (Bloque 4) ---
    public void IniciarPartida()
    {
        if (!IsServer) return;

        tiempoRestanteNet.Value = tiempoDeJuego;
        juegoActivo = true;

        if (entornoJuego) entornoJuego.SetActive(true);

        Debug.Log("[SERVER] Bloque 4 Iniciado. Teletransportando jugadores VR y creando armas...");

        // 1. Obtener la lista exclusiva de IDs de visores VR (excluyendo el Móvil Admin)
        List<ulong> idsJugadoresVR = ObtenerIDsJugadoresVR();

        // 2. Enviamos la orden a todos los clientes pasando la lista ordenada de visores VR
        MoverJugadoresAPuntosClientRpc(idsJugadoresVR.ToArray());

        // 3. El servidor crea las armas asignando correctamente cada punto a su jugador VR
        SpawnArmasEnPuntos(idsJugadoresVR);

        // 4. Arrancamos los asteroides
        StartCoroutine(RutinaSpawn());
    }

    private List<ulong> ObtenerIDsJugadoresVR()
    {
        List<ulong> listaVR = new List<ulong>();
        if (NetworkManager.Singleton != null)
        {
            foreach (var client in NetworkManager.Singleton.ConnectedClientsList)
            {
                // Solo incluimos a los clientes que son Visores VR (tienen PlayerObject instanciado)
                if (client.PlayerObject != null)
                {
                    listaVR.Add(client.ClientId);
                }
            }
        }
        return listaVR;
    }

    [ClientRpc]
    private void MoverJugadoresAPuntosClientRpc(ulong[] idsJugadoresVR)
    {
        GameObject miXR = GameObject.Find("XR_Origin_LOCAL");
        if (miXR != null && NetworkManager.Singleton != null)
        {
            ulong miID = NetworkManager.Singleton.LocalClientId;

            // Buscamos cuál es nuestro índice relativo entre los jugadores VR conectados (0, 1, 2, 3, 4 o 5)
            int miIndiceVR = System.Array.IndexOf(idsJugadoresVR, miID);

            if (miIndiceVR >= 0 && miIndiceVR < puntosDeSpawnJugadores.Count)
            {
                miXR.transform.position = puntosDeSpawnJugadores[miIndiceVR].position;
                miXR.transform.rotation = puntosDeSpawnJugadores[miIndiceVR].rotation;
                Debug.Log($"[CLIENTE] Teletransportado con éxito al punto de spawn VR índice: {miIndiceVR}");
            }
        }

        // Forzamos a que todos los avatares despierten sus mallas y colores
        var todosLosAvatares = FindObjectsByType<PlayerAvatarSync>(FindObjectsSortMode.None);
        foreach (var avatar in todosLosAvatares)
        {
            if (avatar != null)
            {
                avatar.ActivarVisibilidadEnPartida();
            }
        }
    }

    private void SpawnArmasEnPuntos(List<ulong> idsJugadoresVR)
    {
        if (!IsServer || prefabArma == null) return;

        armasSpawneadas.Clear();

        for (int i = 0; i < idsJugadoresVR.Count; i++)
        {
            if (i < puntosDeSpawnJugadores.Count)
            {
                ulong idClienteVR = idsJugadoresVR[i];
                Transform puntoSpawn = puntosDeSpawnJugadores[i];

                // El Servidor instancia el arma en el punto de spawn del jugador VR correspondiente
                GameObject miArma = Instantiate(prefabArma, puntoSpawn.position, puntoSpawn.rotation);
                NetworkObject netObj = miArma.GetComponent<NetworkObject>();

                if (netObj != null)
                {
                    netObj.SpawnWithOwnership(idClienteVR, true);
                    armasSpawneadas.Add(netObj);

                    Debug.Log($"[SERVER] Arma creada en Punto {i} y asignada al Jugador VR ID: {idClienteVR}");
                }
            }
        }
    }

    void Update()
    {
        if (IsServer && juegoActivo)
        {
            tiempoRestanteNet.Value -= Time.deltaTime;

            if (tiempoRestanteNet.Value <= 0)
            {
                tiempoRestanteNet.Value = 0;
                FinalizarPartida();
            }
        }

        if (juegoActivo)
        {
            foreach (var pantalla in pantallasDeArmas)
            {
                if (pantalla != null) pantalla.ActualizarTiempo(Mathf.CeilToInt(tiempoRestanteNet.Value));
            }
        }
    }

    IEnumerator RutinaSpawn()
    {
        while (juegoActivo)
        {
            SpawnObjetivoEnRed();
            yield return new WaitForSeconds(Random.Range(0.5f, intervaloSpawn));
        }
    }

    void SpawnObjetivoEnRed()
    {
        if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsListening)
        {
            Debug.LogWarning("[GAMEPLAY MANAGER] Esperando que el NetworkManager esté completamente activo...");
            return;
        }

        if (puntosDeSpawnAsteroides.Count == 0 || listaDeObjetivos.Length == 0) return;

        Transform puntoAleatorio = puntosDeSpawnAsteroides[Random.Range(0, puntosDeSpawnAsteroides.Count)];
        GameObject asteroideElegido = listaDeObjetivos[Random.Range(0, listaDeObjetivos.Length)];

        if (asteroideElegido != null)
        {
            if (asteroideElegido.GetComponent<NetworkObject>() == null)
            {
                Debug.LogError($"[GAMEPLAY MANAGER] ¡Alerta Crítica! El prefab '{asteroideElegido.name}' no tiene un componente NetworkObject.");
                return;
            }

            GameObject nuevoAsteroide = Instantiate(asteroideElegido, puntoAleatorio.position, puntoAleatorio.rotation);
            NetworkObject netObj = nuevoAsteroide.GetComponent<NetworkObject>() ?? nuevoAsteroide.GetComponentInChildren<NetworkObject>();

            if (netObj != null && NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer)
            {
                netObj.Spawn(true);
            }
        }
    }

    void FinalizarPartida()
    {
        if (!IsServer) return;

        juegoActivo = false;
        StopAllCoroutines();

        foreach (var armaNetObj in armasSpawneadas)
        {
            if (armaNetObj != null && armaNetObj.IsSpawned) armaNetObj.Despawn();
        }
        armasSpawneadas.Clear();

        var objetivos = FindObjectsByType<AsteroidTarget>(FindObjectsSortMode.None);
        foreach (var obj in objetivos)
        {
            if (obj != null && obj.GetComponent<NetworkObject>().IsSpawned) obj.GetComponent<NetworkObject>().Despawn();
        }

        if (MainGameManager.Instance != null) MainGameManager.Instance.FinalizarExperienciaCompleta();
    }

    public void LimpiarGameplayParaReset()
    {
        juegoActivo = false;
        StopAllCoroutines();

        if (entornoJuego != null) entornoJuego.SetActive(false);

        if (IsServer)
        {
            foreach (var armaNetObj in armasSpawneadas)
            {
                if (armaNetObj != null && armaNetObj.IsSpawned) armaNetObj.Despawn();
            }
            armasSpawneadas.Clear();

            var objetivos = FindObjectsByType<AsteroidTarget>(FindObjectsSortMode.None);
            foreach (var obj in objetivos)
            {
                if (obj != null && obj.GetComponent<NetworkObject>() != null && obj.GetComponent<NetworkObject>().IsSpawned)
                {
                    obj.GetComponent<NetworkObject>().Despawn();
                }
            }
        }

        pantallasDeArmas.Clear();
        Debug.Log("<color=orange>[GAMEPLAY MANAGER]</color> Armas y asteroides despawneados con éxito.");
    }
}
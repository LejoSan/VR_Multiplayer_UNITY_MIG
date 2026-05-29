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

        // Llenamos los puntos de spawn de jugadores
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

        Debug.Log("[SERVER] Bloque 4 Iniciado. Teletransportando jugadores locales y creando armas...");

        // 1. Enviamos la orden a todos los clientes para que muevan su XR_Origin_LOCAL físico
        MoverJugadoresAPuntosClientRpc();

        // 2. El servidor crea las armas frente a los puntos de spawn correspondientes
        SpawnArmasEnPuntos();

        // 3. Arrancamos los asteroides
        StartCoroutine(RutinaSpawn());

        // Añadir al final de la función IniciarPartida() en GameplayManager.cs
        var todosLosAvatares = FindObjectsByType<PlayerAvatarSync>(FindObjectsSortMode.None);
        foreach (var avatar in todosLosAvatares)
        {
            if (avatar != null) avatar.ActivarVisibilidadEnPartida();
        }
    }

    [ClientRpc]
    private void MoverJugadoresAPuntosClientRpc()
    {
        // Cada jugador busca su propio XR_Origin_LOCAL en su escena
        GameObject miXR = GameObject.Find("XR_Origin_LOCAL");
        if (miXR != null)
        {
            // Conseguimos el ID de este cliente para saber qué número de spawn le toca
            int miID = (int)NetworkManager.Singleton.LocalClientId;

            // Evitamos errores de índice si hay más jugadores que puntos de spawn
            if (miID < puntosDeSpawnJugadores.Count)
            {
                miXR.transform.position = puntosDeSpawnJugadores[miID].position;
                miXR.transform.rotation = puntosDeSpawnJugadores[miID].rotation;
                Debug.Log($"[CLIENTE] Teletransportado con éxito al punto de spawn: {miID}");
            }
        }
    }

    private void SpawnArmasEnPuntos()
    {
        if (prefabArma == null) return;

        // Limpiamos la lista previa de armas para evitar fugas de memoria
        armasSpawneadas.Clear();

        // Recorremos la lista oficial de clientes conectados en la sesión
        foreach (var client in NetworkManager.Singleton.ConnectedClientsList)
        {
            // El ClientId (0, 1, 2, 3) nos dice exactamente qué número de jugador es
            int idJugador = (int)client.ClientId;

            // Aseguramos que el jugador tenga un punto de spawn asignado en el mapa
            if (idJugador < puntosDeSpawnJugadores.Count)
            {
                // Sacamos el punto de spawn exacto que le pertenece a ESTE ID de red
                Transform puntoSpawn = puntosDeSpawnJugadores[idJugador];

                // Calculamos el espacio modular: 50cm al frente de sus ojos y a 1.2m de altura del suelo
                Vector3 posicionArma = puntoSpawn.position + (puntoSpawn.forward * 0.5f) + (Vector3.up * 1.2f);

                // El servidor crea la instancia física del arma
                GameObject miArma = Instantiate(prefabArma, posicionArma, puntoSpawn.rotation);
                NetworkObject netObj = miArma.GetComponent<NetworkObject>();

                if (netObj != null)
                {
                    // 🌟 LA REGLA DE ORO: Spawneamos el arma asignándole el Ownership (Dueño) 
                    // exclusivo al idJugador correspondiente. ¡Un arma por persona, sin duplicados!
                    netObj.SpawnWithOwnership(client.ClientId);
                    armasSpawneadas.Add(netObj);

                    Debug.Log($"[SERVER] Arma spawneada y asignada con éxito al Jugador ID: {idJugador} de forma exclusiva.");
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
        if (puntosDeSpawnAsteroides.Count == 0 || listaDeObjetivos.Length == 0) return;

        Transform puntoAleatorio = puntosDeSpawnAsteroides[Random.Range(0, puntosDeSpawnAsteroides.Count)];
        GameObject asteroideElegido = listaDeObjetivos[Random.Range(0, listaDeObjetivos.Length)];

        if (asteroideElegido != null)
        {
            GameObject nuevoAsteroide = Instantiate(asteroideElegido, puntoAleatorio.position, puntoAleatorio.rotation);
            NetworkObject netObj = nuevoAsteroide.GetComponent<NetworkObject>();
            if (netObj != null) netObj.Spawn();
        }
    }

    void FinalizarPartida()
    {
        if (!IsServer) return;

        juegoActivo = false;
        StopAllCoroutines();

        // Limpieza de armas (Bloque 5)
        foreach (var armaNetObj in armasSpawneadas)
        {
            if (armaNetObj != null && armaNetObj.IsSpawned) armaNetObj.Despawn();
        }
        armasSpawneadas.Clear();

        // Limpieza de asteroides
        var objetivos = FindObjectsByType<AsteroidTarget>(FindObjectsSortMode.None);
        foreach (var obj in objetivos)
        {
            if (obj != null && obj.GetComponent<NetworkObject>().IsSpawned) obj.GetComponent<NetworkObject>().Despawn();
        }

        if (MainGameManager.Instance != null) MainGameManager.Instance.FinalizarExperienciaCompleta();
    }
}
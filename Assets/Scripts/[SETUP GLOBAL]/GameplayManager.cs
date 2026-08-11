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

        // 1. Enviamos la orden a todos los clientes (Mueve la posición y activa los colores de los avatares)
        MoverJugadoresAPuntosClientRpc();

        // 2. El servidor crea las armas frente a los puntos de spawn correspondientes
        SpawnArmasEnPuntos();

        // 3. Arrancamos los asteroides
        StartCoroutine(RutinaSpawn());
    }

    [ClientRpc]
    private void MoverJugadoresAPuntosClientRpc()
    {
        // Cada jugador busca su propio XR_Origin_LOCAL en su escena
        GameObject miXR = GameObject.Find("XR_Origin_LOCAL");
        if (miXR != null)
        {
            int miID = (int)NetworkManager.Singleton.LocalClientId;

            if (miID < puntosDeSpawnJugadores.Count)
            {
                miXR.transform.position = puntosDeSpawnJugadores[miID].position;
                miXR.transform.rotation = puntosDeSpawnJugadores[miID].rotation;
                Debug.Log($"[CLIENTE] Teletransportado con éxito al punto de spawn: {miID}");
            }
        }

        // 🌟 SOLUCIÓN CLIENTES MULTIJUGADOR: Forzamos a que todos los clientes y el host 
        // despierten las mallas y pinten los colores de los avatares en sus propias pantallas al mismo tiempo
        var todosLosAvatares = FindObjectsByType<PlayerAvatarSync>(FindObjectsSortMode.None);
        foreach (var avatar in todosLosAvatares)
        {
            if (avatar != null)
            {
                avatar.ActivarVisibilidadEnPartida();
            }
        }
    }

    private void SpawnArmasEnPuntos()
    {
        if (!IsServer) return;
        if (prefabArma == null) return;

        armasSpawneadas.Clear();

        foreach (var client in NetworkManager.Singleton.ConnectedClientsList)
        {
            int idJugador = (int)client.ClientId;

            if (idJugador < puntosDeSpawnJugadores.Count)
            {
                Transform puntoSpawn = puntosDeSpawnJugadores[idJugador];

                // Calculamos la posición frente al punto de spawn del jugador
                Vector3 posicionArma = puntoSpawn.position + (puntoSpawn.forward * 0.5f) + (Vector3.up * 1.2f);

                // El Servidor instancia la pistola
                GameObject miArma = Instantiate(prefabArma, posicionArma, puntoSpawn.rotation);
                NetworkObject netObj = miArma.GetComponent<NetworkObject>();

                if (netObj != null)
                {
                    // Al nacer con Ownership, el script WeaponDisplay sabrá autónomamente de quién es
                    netObj.SpawnWithOwnership(client.ClientId, true);
                    armasSpawneadas.Add(netObj);

                    Debug.Log($"[SERVER] Arma creada y firmada legalmente por el Servidor para el Jugador ID: {idJugador}");
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
        // 🌟 SEGURO MULTIJUGADOR GLOBAL: Si el gestor de red está apagado o colapsado, abortamos para no congelar el juego
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
            // Comprobación previa del componente de red en el prefab original
            if (asteroideElegido.GetComponent<NetworkObject>() == null)
            {
                Debug.LogError($"[GAMEPLAY MANAGER] ¡Alerta Crítica! El prefab '{asteroideElegido.name}' no tiene un componente NetworkObject.");
                return;
            }

            GameObject nuevoAsteroide = Instantiate(asteroideElegido, puntoAleatorio.position, puntoAleatorio.rotation);

            // Buscamos de forma ultra-segura el componente en el clon creado
            NetworkObject netObj = nuevoAsteroide.GetComponent<NetworkObject>() ?? nuevoAsteroide.GetComponentInChildren<NetworkObject>();

            // 🌟 VALIDACIÓN DE SEGURIDAD: Verificamos de forma independiente que existan tanto el objeto como el servidor
            if (netObj != null && NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer)
            {
                netObj.Spawn(true); // Spawnea de forma segura en toda la red
            }
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

    // 🌟 MÉTODO DE LIMPIEZA DE ARMAS Y ASTEROIDES
    public void LimpiarGameplayParaReset()
    {
        juegoActivo = false;
        StopAllCoroutines();

        if (entornoJuego != null) entornoJuego.SetActive(false);

        // Limpieza exclusiva del servidor (Despawnear objetos en la red)
        if (IsServer)
        {
            // Borrar armas instanciadas
            foreach (var armaNetObj in armasSpawneadas)
            {
                if (armaNetObj != null && armaNetObj.IsSpawned) armaNetObj.Despawn();
            }
            armasSpawneadas.Clear();

            // Borrar asteroides en pantalla
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
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

    [Header("--- Spawners (Host-Only) ---")]
    public GameObject[] listaDeObjetivos;
    public Transform contenedorSpawners;
    public float intervaloSpawn = 1.5f;
    private List<Transform> puntosDeSpawn = new List<Transform>();

    [Header("--- Arma de los Jugadores ---")]
    public GameObject prefabArma;


    // ¡NUEVO! Lista interna del servidor para recordar qué armas ha creado
    private List<NetworkObject> armasSpawneadas = new List<NetworkObject>();

    // Lista de pantallas de armas registradas para actualizarles el reloj localmente
    private List<WeaponDisplay> pantallasDeArmas = new List<WeaponDisplay>();

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        if (contenedorSpawners != null)
        {
            foreach (Transform child in contenedorSpawners) puntosDeSpawn.Add(child);
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



    // --- INICIO DEL JUEGO (Llamado solo cuando se cumple la condición de jugar) ---
    public void IniciarPartida()
    {
        if (!IsServer) return;

        tiempoRestanteNet.Value = tiempoDeJuego;
        juegoActivo = true;

        if (entornoJuego) entornoJuego.SetActive(true);

        // --- NUEVO: Spawn de jugadores en puntos concretos ---
        SpawnearJugadoresEnPuntos();

        // --- NUEVO: Spawn de armas ---
        SpawnArmasParaTodos();

        StartCoroutine(RutinaSpawn());
    }

    private void SpawnearJugadoresEnPuntos()
    {
        if (!IsServer) return;

        // Recorremos a todos los jugadores conectados
        int i = 0;
        foreach (var client in NetworkManager.Singleton.ConnectedClientsList)
        {
            // 1. Instanciamos el prefab original del jugador
            // Asegúrate de que el Prefab del Jugador esté en tu carpeta de Assets y no en la escena
            GameObject playerInstance = Instantiate(NetworkManager.Singleton.NetworkConfig.PlayerPrefab);

            // 2. Le asignamos la posición del punto de spawn
            // Si tienes 4 puntos, nos aseguramos de no salirnos del índice
            if (i < puntosDeSpawn.Count)
            {
                playerInstance.transform.position = puntosDeSpawn[i].position;
                playerInstance.transform.rotation = puntosDeSpawn[i].rotation;
            }

            // 3. Lo registramos en la red como el objeto de ese jugador
            playerInstance.GetComponent<NetworkObject>().SpawnAsPlayerObject(client.ClientId);

            i++;
        }
    }
    private void SpawnArmasParaTodos()
    {
        if (prefabArma == null) return;

        // Limpiamos la lista por si acaso venimos de una partida anterior
        armasSpawneadas.Clear();

        foreach (var client in NetworkManager.Singleton.ConnectedClientsList)
        {
            if (client.PlayerObject != null)
            {
                Transform playerTransform = client.PlayerObject.transform;
                Vector3 posicionArma = playerTransform.position + (playerTransform.forward * 0.5f) + (Vector3.up * 1.2f);

                GameObject miArma = Instantiate(prefabArma, posicionArma, playerTransform.rotation);
                NetworkObject netObj = miArma.GetComponent<NetworkObject>();

                if (netObj != null)
                {
                    // Le damos el arma al jugador
                    netObj.SpawnWithOwnership(client.ClientId);
                    // 🎯 LA GUARDAMOS EN LA MEMORIA DEL SERVIDOR
                    armasSpawneadas.Add(netObj);
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
        if (puntosDeSpawn.Count == 0 || listaDeObjetivos.Length == 0) return;

        Transform puntoAleatorio = puntosDeSpawn[Random.Range(0, puntosDeSpawn.Count)];
        GameObject asteroideElegido = listaDeObjetivos[Random.Range(0, listaDeObjetivos.Length)];

        if (asteroideElegido != null)
        {
            GameObject nuevoAsteroide = Instantiate(asteroideElegido, puntoAleatorio.position, puntoAleatorio.rotation);
            NetworkObject netObj = nuevoAsteroide.GetComponent<NetworkObject>();
            if (netObj != null) netObj.Spawn();
        }
    }

    // --- TRANSICIÓN AL PANEL DE RESULTADOS (BLOQUE 5) ---
    void FinalizarPartida()
    {
        if (!IsServer) return;

        juegoActivo = false;
        StopAllCoroutines();

        Debug.Log("[SERVER] Tiempo agotado. Limpiando escena para el panel de resultados...");

        // 💥 LIMPIEZA 1: Eliminar todas las armas de los jugadores de la red
        foreach (var armaNetObj in armasSpawneadas)
        {
            if (armaNetObj != null && armaNetObj.IsSpawned)
            {
                armaNetObj.Despawn(); // Desaparece instantáneamente de todas las gafas
            }
        }
        armasSpawneadas.Clear(); // Vaciamos la lista para la siguiente ronda

        // 💥 LIMPIEZA 2: Eliminar todos los asteroides sobrantes de la red
        var objetivos = FindObjectsByType<AsteroidTarget>(FindObjectsSortMode.None);
        foreach (var obj in objetivos)
        {
            if (obj != null && obj.GetComponent<NetworkObject>().IsSpawned)
            {
                obj.GetComponent<NetworkObject>().Despawn();
            }
        }

        // 3. Avisamos al MainGameManager para que encienda el Panel de Puntuaciones final (Bloque 5)
        if (MainGameManager.Instance != null)
        {
            MainGameManager.Instance.FinalizarExperienciaCompleta();
        }
    }

    public void Debug_ForzarFinal()
    {
        if (IsServer && juegoActivo)
        {
            tiempoRestanteNet.Value = 0;
            FinalizarPartida();
        }
    }
}
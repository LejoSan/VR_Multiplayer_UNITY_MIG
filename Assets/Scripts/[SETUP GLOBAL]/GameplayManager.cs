using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;

public class GameplayManager : NetworkBehaviour
{
    public static GameplayManager Instance;

    [Header("--- Configuración de la Partida ---")]
    public float tiempoDeJuego = 30f;

    // ¡NUEVO! El tiempo sincronizado para todos
    public NetworkVariable<float> tiempoRestanteNet = new NetworkVariable<float>(30f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    private bool juegoActivo = false;

    [Header("--- Entorno ---")]
    public GameObject entornoJuego;

    [Header("--- Spawners (Host-Only) ---")]
    public GameObject[] listaDeObjetivos;
    public Transform contenedorSpawners;
    public float intervaloSpawn = 1.5f;
    private List<Transform> puntosDeSpawn = new List<Transform>();

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

    // --- INICIO DEL JUEGO (Llamado por el MainGameManager) ---
    public void IniciarPartida()
    {
        if (!IsServer) return;

        tiempoRestanteNet.Value = tiempoDeJuego;
        juegoActivo = true;

        if (entornoJuego) entornoJuego.SetActive(true);

        Debug.Log("[SERVER] Arrancando reloj y spawn de asteroides...");
        StartCoroutine(RutinaSpawn());
    }

    void Update()
    {
        // El reloj lo controla únicamente el Servidor
        if (IsServer && juegoActivo)
        {
            tiempoRestanteNet.Value -= Time.deltaTime;

            if (tiempoRestanteNet.Value <= 0)
            {
                tiempoRestanteNet.Value = 0;
                FinalizarPartida();
            }
        }

        // Todos actualizan las pantallas de sus armas leyendo la variable de red
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
            // 1. Instanciamos el objeto en el servidor
            GameObject nuevoAsteroide = Instantiate(asteroideElegido, puntoAleatorio.position, puntoAleatorio.rotation);

            // 2. ¡LA MAGIA! Le decimos a Netcode que lo haga aparecer en todos los visores
            NetworkObject netObj = nuevoAsteroide.GetComponent<NetworkObject>();
            if (netObj != null)
            {
                netObj.Spawn();
            }
        }
    }

    void FinalizarPartida()
    {
        if (!IsServer) return;

        juegoActivo = false;
        StopAllCoroutines();

        // Eliminar todos los asteroides de la red
        var objetivos = FindObjectsByType<AsteroidTarget>(FindObjectsSortMode.None);
        foreach (var obj in objetivos)
        {
            if (obj != null && obj.GetComponent<NetworkObject>().IsSpawned)
            {
                obj.GetComponent<NetworkObject>().Despawn();
            }
        }

        // Avisar al MainGameManager para que muestre la UI de Victoria/Podio
        if (MainGameManager.Instance != null)
        {
            MainGameManager.Instance.FinalizarExperienciaCompleta();
        }
    }
    // --- ZONA DE DESARROLLADOR (DEBUG) ---
    public void Debug_ForzarFinal()
    {
        // En multijugador, solo el servidor tiene autoridad para forzar el final
        if (IsServer && juegoActivo)
        {
            tiempoRestanteNet.Value = 0;
            FinalizarPartida();
        }
        else
        {
            Debug.LogWarning("Solo el HOST puede forzar el final de la partida con F2.");
        }
    }
}
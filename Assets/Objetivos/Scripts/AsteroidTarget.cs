using UnityEngine;
using Unity.Netcode;

// Ahora hereda de NetworkBehaviour
public class AsteroidTarget : NetworkBehaviour
{
    [Header("--- Escala y Crecimiento ---")]
    public float escalaInicial = 0.1f;
    public float escalaFinal = 1.5f;
    public float distanciaDeCrecimiento = 60f;

    [Header("--- Lógica de Juego ---")]
    public int puntosQueDa = 10;
    public GameObject efectoExplosion;

    [Header("--- Movimiento (Solo Servidor) ---")]
    public float velocidadMin = 5f;
    public float velocidadMax = 15f;
    [Range(0, 1)] public float probabilidadDeImpacto = 0.7f;
    public float margenDeError = 5f;

    private float velocidadFinal;
    private Vector3 direccionViaje;
    private Vector3 escalaObjetivoVector;

    // El centro del mapa (0,0,0) donde estarán los 4 jugadores
    private Vector3 centroDelMapa = Vector3.zero;

    void Start()
    {
        transform.localScale = Vector3.one * escalaInicial;
        escalaObjetivoVector = Vector3.one * escalaFinal;

        // Solo el Servidor calcula las matemáticas de movimiento
        if (IsServer)
        {
            velocidadFinal = Random.Range(velocidadMin, velocidadMax);
            CalcularTrayectoria();
        }
    }

    void CalcularTrayectoria()
    {
        Vector3 puntoDestino = centroDelMapa;

        // Un poco de variación para que no vayan todos exactamente al mismo pixel
        if (Random.value > probabilidadDeImpacto)
        {
            puntoDestino += Random.insideUnitSphere * margenDeError;
            // Evitamos que varíen en altura (Y) para que no vayan al suelo
            puntoDestino.y = Mathf.Clamp(puntoDestino.y, 1f, 3f);
        }

        direccionViaje = (puntoDestino - transform.position).normalized;
        transform.forward = direccionViaje;
    }

    void Update()
    {
        // Solo el Servidor mueve el asteroide. El NetworkTransform se encarga de que los clientes lo vean moverse.
        if (IsServer)
        {
            transform.position += direccionViaje * velocidadFinal * Time.deltaTime;
        }

        // El giro estético y el crecimiento lo pueden calcular todos localmente para que se vea súper fluido
        transform.Rotate(Vector3.up * 50f * Time.deltaTime, Space.Self);

        float distanciaActual = Vector3.Distance(transform.position, centroDelMapa);
        float t = Mathf.InverseLerp(distanciaDeCrecimiento, 0f, distanciaActual);
        transform.localScale = Vector3.Lerp(Vector3.one * escalaInicial, escalaObjetivoVector, t);

        // Si el asteroide llega al centro (0,0,0) y nadie le disparó, el Servidor lo destruye
        if (IsServer && distanciaActual < 1.0f)
        {
            GetComponent<NetworkObject>().Despawn();
        }
    }

    // --- NUEVA FUNCIÓN DE IMPACTO EN RED ---
    public void RecibirDisparoEnRed(ulong idTirador)
    {
        if (!IsServer) return; // Por si acaso

        Debug.Log($"¡El jugador {idTirador} ha destruido un asteroide!");

        // 1. Buscar al jugador que disparó y darle los puntos
        foreach (var cliente in NetworkManager.Singleton.ConnectedClientsList)
        {
            if (cliente.ClientId == idTirador && cliente.PlayerObject != null)
            {
                PlayerNetworkState estadoJugador = cliente.PlayerObject.GetComponent<PlayerNetworkState>();
                if (estadoJugador != null)
                {
                    estadoJugador.ModificarPuntuacionServer(puntosQueDa);
                }
                break; // Jugador encontrado y puntuado, salimos del bucle
            }
        }

        // 2. Crear la explosión visual en las gafas de todos (ClientRPC)
        CrearExplosionClientRpc(transform.position);

        // 3. Destruir el asteroide en la red
        if (GetComponent<NetworkObject>().IsSpawned)
        {
            GetComponent<NetworkObject>().Despawn();
        }
    }

    [ClientRpc]
    private void CrearExplosionClientRpc(Vector3 posicionExplosion)
    {
        if (efectoExplosion != null)
        {
            GameObject explosion = Instantiate(efectoExplosion, posicionExplosion, Quaternion.identity);
            AudioSource fuenteAudio = explosion.GetComponent<AudioSource>();
            if (fuenteAudio != null)
            {
                fuenteAudio.pitch = Random.Range(0.85f, 1.15f);
                fuenteAudio.Play();
            }
            Destroy(explosion, 2f); // Destrucción puramente visual y local
        }
    }
}
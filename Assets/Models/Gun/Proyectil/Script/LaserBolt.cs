using UnityEngine;
using Unity.Netcode;

public class LaserBolt : NetworkBehaviour
{
    [Header("Configuración")]
    public float velocidad = 60f;
    public float tiempoDeVida = 3f;
    public GameObject efectoImpacto;

    [Header("Efectos Visuales (Color)")]
    public Renderer renderLaser; // La malla 3D de tu láser

    // ¡NUEVO! Seguro anti-doble impacto
    private bool yaDestruido = false;


    // Variable de red que guarda quién disparó este láser
    public NetworkVariable<ulong> idDueño = new NetworkVariable<ulong>(999, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    void Start()
    {
        if (IsServer) Invoke("DestruirLaser", tiempoDeVida);
    }

    public override void OnNetworkSpawn()
    {
        // 1. Intentamos pintar el láser nada más nacer
        AplicarColorDelTirador(idDueño.Value);

        // 2. Nos suscribimos por si el dato del servidor llega con unos milisegundos de retraso (prevención de lag)
        idDueño.OnValueChanged += (viejoID, nuevoID) => AplicarColorDelTirador(nuevoID);
    }

    private void AplicarColorDelTirador(ulong idTirador)
    {
        // Si el LobbyManager existe, le pedimos el color asociado a esa ID
        if (LobbyManager.Instance != null && idTirador != 999)
        {
            Color colorTirador = LobbyManager.Instance.ObtenerColorPorID(idTirador);

            // Pintamos la malla del láser
            if (renderLaser != null)
            {
                renderLaser.material.color = colorTirador;
                // Si usas materiales con emisión (brillo/HDR), esta línea hace que el color brille:
                renderLaser.material.SetColor("_EmissionColor", colorTirador * 2.5f);
            }

        }
    }

    void Update()
    {
        transform.Translate(Vector3.up * velocidad * Time.deltaTime);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!IsServer) return;

        if (other.CompareTag("Player") || other.CompareTag("Arma") || other.CompareTag("Proyectil") || other.gameObject.layer == gameObject.layer)
        {
            return;
        }

        if (other.CompareTag("Objetivo"))
        {
            AsteroidTarget asteroide = other.GetComponent<AsteroidTarget>();
            if (asteroide != null)
            {
                asteroide.RecibirDisparoEnRed(idDueño.Value);
            }
        }

        ImpactarEnRed();
    }

    void ImpactarEnRed()
    {
        DestruirLaser();
    }

    void DestruirLaser()
    {
        // Si ya chocamos hace un milisegundo, abortamos
        if (yaDestruido) return;

        // Buscamos el componente de red de forma segura
        NetworkObject miNetObj = GetComponent<NetworkObject>();

        // Si somos el servidor, el objeto tiene componente de red, y sigue vivo en la red
        if (IsServer && miNetObj != null && miNetObj.IsSpawned)
        {
            yaDestruido = true; // Activamos el seguro
            miNetObj.Despawn(); // Lo destruimos para todos
        }
        else if (miNetObj == null)
        {
            // Solo por si acaso olvidaste ponerle el componente en el inspector
            Debug.LogWarning("¡Aviso! Al láser le falta el componente NetworkObject.");
            Destroy(gameObject);
        }
    }
}
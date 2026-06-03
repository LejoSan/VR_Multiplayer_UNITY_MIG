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

    private bool yaDestruido = false;

    // Variable de red que guarda quién disparó este láser
    public NetworkVariable<ulong> idDueño = new NetworkVariable<ulong>(999, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    public override void OnNetworkSpawn()
    {
        // ¡SITIO SEGURO! Si somos el servidor, programamos su destrucción por tiempo
        if (IsServer)
        {
            Invoke("DestruirLaser", tiempoDeVida);
        }

        // 1. Intentamos pintar el láser nada más nacer
        AplicarColorDelTirador(idDueño.Value);

        // 2. Nos suscribimos por si el dato del servidor llega con unos milisegundos de retraso
        idDueño.OnValueChanged += (viejoID, nuevoID) => AplicarColorDelTirador(nuevoID);
    }

    private void AplicarColorDelTirador(ulong idTirador)
    {
        if (LobbyManager.Instance != null && idTirador != 999)
        {
            Color colorTirador = LobbyManager.Instance.ObtenerColorPorID(idTirador);

            // Pintamos la malla del láser
            if (renderLaser != null)
            {
                renderLaser.material.color = colorTirador;
                // Efecto de emisión/brillo HDR
                renderLaser.material.SetColor("_EmissionColor", colorTirador * 2.5f);
            }
        }
    }

    void Update()
    {
        // Movimiento hacia adelante del proyectil
        transform.Translate(Vector3.up * velocidad * Time.deltaTime);
    }

    private void OnTriggerEnter(Collider other)
    {
        // Solo el servidor gestiona colisiones de juego y lógicas de red
        if (!IsServer) return;

        // Filtro de seguridad para no explotar en tus manos, tu cuerpo o tus propias armas
        if (other.CompareTag("Player") || other.CompareTag("Arma") || other.CompareTag("Proyectil") || other.gameObject.layer == gameObject.layer)
        {
            return;
        }

        if (other.CompareTag("Objetivo"))
        {
            AsteroidTarget asteroideBase = other.GetComponent<AsteroidTarget>() ?? other.GetComponentInParent<AsteroidTarget>();
            if (asteroideBase != null)
            {
                // 🌟 LA MAGIA DE NETCODE: Extraemos el OwnerClientId nativo de esta bala.
                // Este ID representa de forma infalible al jugador que invocó el disparo.
                ulong idTiradorReal = OwnerClientId;

                // Le pasamos el ID real al asteroide para que procese el premio
                asteroideBase.RecibirDisparoEnRed(idTiradorReal);
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
        if (yaDestruido) return;

        // Buscamos de forma segura el NetworkObject en cualquier nivel del objeto
        NetworkObject miNetObj = GetComponent<NetworkObject>() ?? GetComponentInParent<NetworkObject>();

        if (IsServer && miNetObj != null && miNetObj.IsSpawned)
        {
            yaDestruido = true;
            miNetObj.Despawn(); // Se destruye de forma sincronizada en todas las gafas
        }
        else if (miNetObj == null)
        {
            Debug.LogWarning("¡Aviso! Al prefab del láser le falta el componente NetworkObject.");
            Destroy(gameObject);
        }
    }
}
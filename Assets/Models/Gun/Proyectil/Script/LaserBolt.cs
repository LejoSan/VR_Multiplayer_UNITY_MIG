using UnityEngine;
using Unity.Netcode;

public class LaserBolt : NetworkBehaviour
{
    [Header("Configuración")]
    public float velocidad = 60f;
    public float tiempoDeVida = 3f;
    public GameObject efectoImpacto;

    [Header("Efectos Visuales (Color)")]
    public Renderer renderLaser;

    private bool yaDestruido = false;

    // 🌟 LA REGLA DE ORO INSTANTÁNEA: Variable normal de C# que burla el lag de las físicas
    [HideInInspector] public ulong idDueñoServidor = 999;

    // Variable de red (Solo para que los clientes remotos pinten el color en sus visores)
    public NetworkVariable<ulong> idDueño = new NetworkVariable<ulong>(999, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            Invoke("DestruirLaser", tiempoDeVida);
        }

        AplicarColorDelTirador(idDueño.Value);
        idDueño.OnValueChanged += (viejoID, nuevoID) => AplicarColorDelTirador(nuevoID);
    }

    private void AplicarColorDelTirador(ulong idTirador)
    {
        if (LobbyManager.Instance != null && idTirador != 999)
        {
            Color colorTirador = LobbyManager.Instance.ObtenerColorPorID(idTirador);
            if (renderLaser != null && renderLaser.material != null)
            {
                if (renderLaser.material.HasProperty("_BaseColor")) renderLaser.material.SetColor("_BaseColor", colorTirador);
                else renderLaser.material.color = colorTirador;

                renderLaser.material.SetColor("_EmissionColor", colorTirador * 2.5f);
            }
        }
    }

    void Update()
    {
        // El proyectil avanza de forma constante hacia adelante
        transform.Translate(Vector3.up * velocidad * Time.deltaTime);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!IsServer) return;

        // 🔬 LOG DE RASTREO 3: Ver con qué datos llega la bala al asteroide al momento exacto de chocar
        if (other.CompareTag("Objetivo"))
        {
            Debug.Log($"<color=orange><b>[DEBUG BALA]</b></color> ¡Choque detectado contra Asteroide! Mi variable 'idDueñoServidor' vale actualmente: {idDueñoServidor}");
        }

        if (other.CompareTag("Player") || other.CompareTag("Arma") || other.CompareTag("Proyectil") || other.gameObject.layer == gameObject.layer)
        {
            return;
        }

        if (other.CompareTag("Objetivo"))
        {
            AsteroidTarget asteroideBase = other.GetComponent<AsteroidTarget>() ?? other.GetComponentInParent<AsteroidTarget>();
            if (asteroideBase != null)
            {
                // 🌟 PROTECCIÓN ANTI-999: Pasamos la variable C# pura que ya tiene el ID guardado al instante
                ulong idTiradorReal = idDueñoServidor;
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

        NetworkObject miNetObj = GetComponent<NetworkObject>() ?? GetComponentInParent<NetworkObject>();
        if (IsServer && miNetObj != null && miNetObj.IsSpawned)
        {
            yaDestruido = true;
            miNetObj.Despawn();
        }
        else if (miNetObj == null)
        {
            Destroy(gameObject);
        }
    }
}
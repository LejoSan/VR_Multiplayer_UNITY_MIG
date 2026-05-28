using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using Unity.Netcode;

// ¡Atención! Ahora hereda de NetworkBehaviour
public class Arma : NetworkBehaviour
{
    [Header("Configuración de Disparo")]
    public GameObject prefabLaser;
    public Transform puntaDelArma;

    [Header("Sensación")]
    public float dispersion = 1.0f;
    public AudioSource audioDisparo;
    public AudioClip sonidoClip;

    private XRGrabInteractable grabInteractable;

    void Awake()
    {
        grabInteractable = GetComponent<XRGrabInteractable>();
    }

    void OnEnable()
    {
        grabInteractable.activated.AddListener(DispararConGatillo);
    }

    void OnDisable()
    {
        grabInteractable.activated.RemoveListener(DispararConGatillo);
    }

    private void DispararConGatillo(ActivateEventArgs arg)
    {
        // Solo el jugador que tiene el arma en la mano puede ejecutar el disparo
        Shoot();
    }

    public void Shoot()
    {
        // 1. Sonido y efectos locales (para que se sienta instantáneo sin lag)
        if (audioDisparo && sonidoClip) audioDisparo.PlayOneShot(sonidoClip);
        else if (audioDisparo) audioDisparo.Play();

        // 2. Calcular rotación con dispersión
        Quaternion rotacionConRuido = puntaDelArma.rotation;
        rotacionConRuido *= Quaternion.Euler(Random.Range(-dispersion, dispersion), Random.Range(-dispersion, dispersion), 0);
        Quaternion rotacionCorregida = rotacionConRuido * Quaternion.Euler(90, 0, 0);

        // 3. Avisar al servidor para que cree el láser oficial en la red
        ulong miID = NetworkManager.Singleton.LocalClientId;
        DispararServerRpc(miID, puntaDelArma.position, rotacionCorregida);
    }
    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    private void DispararServerRpc(ulong idTirador, Vector3 posicion, Quaternion rotacion)
    {
        // 1. El servidor crea el objeto físico localmente
        GameObject nuevoLaser = Instantiate(prefabLaser, posicion, rotacion);

        // 2. Buscamos el NetworkObject en la raíz o en los hijos
        NetworkObject netObj = nuevoLaser.GetComponentInParent<NetworkObject>() ?? nuevoLaser.GetComponentInChildren<NetworkObject>();

        if (netObj != null)
        {
            // 🔥 ¡PRIMERO SPAWNEAMOS! Ahora el objeto ya vive oficialmente en la red
            netObj.Spawn();
        }

        // 3. ¡AHORA SÍ! Modificamos la NetworkVariable de forma segura sin advertencias
        LaserBolt scriptLaser = nuevoLaser.GetComponentInChildren<LaserBolt>();
        if (scriptLaser != null)
        {
            scriptLaser.idDueño.Value = idTirador;
        }
    }
}

//    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
//    private void DispararServerRpc(ulong idTirador, Vector3 posicion, Quaternion rotacion)
//    {
//        // El servidor crea el láser...
//        GameObject nuevoLaser = Instantiate(prefabLaser, posicion, rotacion);

//        // Buscamos el script de forma segura en la raíz o en los hijos
//        LaserBolt scriptLaser = nuevoLaser.GetComponentInChildren<LaserBolt>();
//        if (scriptLaser != null)
//        {
//            scriptLaser.idDueño.Value = idTirador;
//        }

//        // Buscamos el NetworkObject en la raíz o en los hijos y lo spawneamos
//        NetworkObject netObj = nuevoLaser.GetComponentInParent<NetworkObject>() ?? nuevoLaser.GetComponentInChildren<NetworkObject>();
//        if (netObj != null)
//        {
//            netObj.Spawn();
//        }
//    }
//}'
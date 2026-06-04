using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using Unity.Netcode;

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
        Shoot();
    }

    public void Shoot()
    {
        if (audioDisparo && sonidoClip) audioDisparo.PlayOneShot(sonidoClip);
        else if (audioDisparo) audioDisparo.Play();

        Quaternion rotacionConRuido = puntaDelArma.rotation;
        rotacionConRuido *= Quaternion.Euler(Random.Range(-dispersion, dispersion), Random.Range(-dispersion, dispersion), 0);
        Quaternion rotacionCorregida = rotacionConRuido * Quaternion.Euler(90, 0, 0);

        ulong miID = NetworkManager.Singleton.LocalClientId;
        DispararServerRpc(miID, puntaDelArma.position, rotacionCorregida);
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    private void DispararServerRpc(ulong idTirador, Vector3 posicion, Quaternion rotacion)
    {
        // 1. Instanciamos el prefab de la bala (pa maestra)
        GameObject nuevoLaser = Instantiate(prefabLaser, posicion, rotacion);

        Debug.Log($"<color=magenta><b>[DEBUG ARMA]</b></color> Servidor ejecuta disparo. ID del tirador enviado: {idTirador}");

        // 2. 🌟 EL BARREDO TOTAL: Buscamos TODOS los scripts LaserBolt en la raíz y en los hijos (activos o no)
        LaserBolt[] todosLosScriptsLaser = nuevoLaser.GetComponentsInChildren<LaserBolt>(true);

        Debug.Log($"<color=yellow><b>[DEBUG ARMA]</b></color> Se detectaron {todosLosScriptsLaser.Length} instancias del script LaserBolt en este objeto.");

        foreach (LaserBolt scriptLaser in todosLosScriptsLaser)
        {
            if (scriptLaser != null)
            {
                scriptLaser.idDueñoServidor = idTirador; // Se lo inyectamos a todos por seguridad
            }
        }

        // 3. Spawneamos el objeto en la red de forma legal
        NetworkObject netObj = nuevoLaser.GetComponent<NetworkObject>() ?? nuevoLaser.GetComponentInChildren<NetworkObject>();
        if (netObj != null)
        {
            netObj.Spawn();
        }

        // 4. Sincronizamos la NetworkVariable de red en todas las copias para los clientes
        foreach (LaserBolt scriptLaser in todosLosScriptsLaser)
        {
            if (scriptLaser != null)
            {
                scriptLaser.idDueño.Value = idTirador;
            }
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
using UnityEngine;
using UnityEngine.XR;
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

    [Header("Alineación con la Mano VR")]
    [Tooltip("Arrastra aquí el objeto hijo 'PuntoDeAgarre' de tu pistola")]
    public Transform puntoDeAgarre;
    [Tooltip("Nombre exacto de la mano en XR_Origin_LOCAL")]
    public string nombreManoDerecha = "Right Controller";
    public XRNode manoController = XRNode.RightHand;

    [Tooltip("Tiempo mínimo entre disparos en segundos")]
    public float cadenciaDisparo = 0.15f;

    private Transform manoTransform;
    private bool estabaPresionadoAnteriormente = false;
    private float ultimoTiempoDisparo = 0f;

    void Awake()
    {
        // 🛑 DESHABILITAR EL BOTÓN DE AGARRAR (GRIP) POR COMPLETO
        // Desactivamos el XRGrabInteractable para que el botón Grip no mueva ni suelte el arma
        if (TryGetComponent<XRGrabInteractable>(out var grabInteractable))
        {
            grabInteractable.enabled = false;
            Destroy(grabInteractable); // Lo eliminamos para evitar cualquier conflicto de física
        }

        // Asegurar que la física no afecte al arma pegada a la mano
        if (TryGetComponent<Rigidbody>(out var rb))
        {
            rb.isKinematic = true;
            rb.useGravity = false;
        }
    }

    public override void OnNetworkSpawn()
    {
        if (EsDuenioLocal())
        {
            BuscarManoLocal();
        }
    }

    private bool EsDuenioLocal()
    {
        // Funciona tanto en partida multijugador (IsOwner) como probando en el Editor (sin Netcode activo)
        return !IsSpawned || IsOwner;
    }

    private void BuscarManoLocal()
    {
        GameObject origin = GameObject.Find("XR_Origin_LOCAL");
        if (origin != null)
        {
            Transform[] todosLosHijos = origin.GetComponentsInChildren<Transform>(true);
            foreach (Transform t in todosLosHijos)
            {
                if (t.name.Equals(nombreManoDerecha, System.StringComparison.OrdinalIgnoreCase))
                {
                    manoTransform = t;
                    break;
                }
            }
        }
    }

    void Update()
    {
        if (EsDuenioLocal())
        {
            DetectarGatilloVR();
        }
    }

    void LateUpdate()
    {
        if (EsDuenioLocal())
        {
            if (manoTransform == null)
            {
                BuscarManoLocal();
            }

            if (manoTransform != null)
            {
                AlinearConMano();
            }
        }
    }

    private void AlinearConMano()
    {
        if (puntoDeAgarre != null)
        {
            // Alineación exacta usando 'puntoDeAgarre'
            Quaternion rotacionRelativa = puntoDeAgarre.localRotation;
            Vector3 posicionRelativa = puntoDeAgarre.localPosition;

            transform.rotation = manoTransform.rotation * Quaternion.Inverse(rotacionRelativa);
            transform.position = manoTransform.position - (transform.rotation * posicionRelativa);
        }
        else
        {
            transform.position = manoTransform.position;
            transform.rotation = manoTransform.rotation;
        }
    }

    private void DetectarGatilloVR()
    {
        bool triggerPresionado = false;

        // 1. Detección por Hardware VR (Visor / Mandos)
        InputDevice device = InputDevices.GetDeviceAtXRNode(manoController);
        if (device.isValid)
        {
            // Lectura como botón booleano
            if (device.TryGetFeatureValue(CommonUsages.triggerButton, out bool btnState))
            {
                triggerPresionado |= btnState;
            }
            // Lectura como eje analógico (presionado más de la mitad)
            if (device.TryGetFeatureValue(CommonUsages.trigger, out float triggerValue))
            {
                triggerPresionado |= (triggerValue > 0.5f);
            }
        }

        // 2. Detección de respaldo para pruebas en Editor (Clic de ratón o tecla Espacio)
        if (Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.Space))
        {
            triggerPresionado = true;
        }

        // Lógica de disparo
        if (triggerPresionado && !estabaPresionadoAnteriormente)
        {
            if (Time.time >= ultimoTiempoDisparo + cadenciaDisparo)
            {
                Shoot();
                ultimoTiempoDisparo = Time.time;
            }
        }

        estabaPresionadoAnteriormente = triggerPresionado;
    }

    public void Shoot()
    {
        ReproducirSonidoDisparo();

        if (puntaDelArma == null || prefabLaser == null)
        {
            Debug.LogWarning("[ARMA] Falta asignar PuntaDelArma o PrefabLaser en el Inspector.");
            return;
        }

        Quaternion rotacionConRuido = puntaDelArma.rotation;
        rotacionConRuido *= Quaternion.Euler(Random.Range(-dispersion, dispersion), Random.Range(-dispersion, dispersion), 0);
        Quaternion rotacionCorregida = rotacionConRuido * Quaternion.Euler(90, 0, 0);

        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
        {
            ulong miID = NetworkManager.Singleton.LocalClientId;
            DispararServerRpc(miID, puntaDelArma.position, rotacionCorregida);
        }
        else
        {
            // Disparo local si estás probando sin red
            Instantiate(prefabLaser, puntaDelArma.position, rotacionCorregida);
        }
    }

    private void ReproducirSonidoDisparo()
    {
        if (audioDisparo != null)
        {
            if (sonidoClip != null) audioDisparo.PlayOneShot(sonidoClip);
            else audioDisparo.Play();
        }
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    private void DispararServerRpc(ulong idTirador, Vector3 posicion, Quaternion rotacion)
    {
        GameObject nuevoLaser = Instantiate(prefabLaser, posicion, rotacion);

        LaserBolt[] todosLosScriptsLaser = nuevoLaser.GetComponentsInChildren<LaserBolt>(true);
        foreach (LaserBolt scriptLaser in todosLosScriptsLaser)
        {
            if (scriptLaser != null) scriptLaser.idDueñoServidor = idTirador;
        }

        NetworkObject netObj = nuevoLaser.GetComponent<NetworkObject>() ?? nuevoLaser.GetComponentInChildren<NetworkObject>();
        if (netObj != null) netObj.Spawn();

        foreach (LaserBolt scriptLaser in todosLosScriptsLaser)
        {
            if (scriptLaser != null) scriptLaser.idDueño.Value = idTirador;
        }

        ReproducirSonidoClientRpc();
    }

    [Rpc(SendTo.ClientsAndHost)]
    private void ReproducirSonidoClientRpc()
    {
        if (!IsOwner)
        {
            ReproducirSonidoDisparo();
        }
    }
}
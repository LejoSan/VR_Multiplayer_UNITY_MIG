using Unity.Netcode;
using UnityEngine;

public class PlayerAvatarSync : NetworkBehaviour
{
    [Header("--- Datos Sincronizados de Red ---")]
    public NetworkVariable<int> puntuacion = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public NetworkVariable<Vector4> colorJugadorNet = new NetworkVariable<Vector4>(Vector4.one, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    [Header("--- Sincronización de Movimiento en Red ---")]
    public NetworkVariable<Vector3> posCabeza = new NetworkVariable<Vector3>(Vector3.zero, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    public NetworkVariable<Quaternion> rotCabeza = new NetworkVariable<Quaternion>(Quaternion.identity, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

    public NetworkVariable<Vector3> posManoIzq = new NetworkVariable<Vector3>(Vector3.zero, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    public NetworkVariable<Quaternion> rotManoIzq = new NetworkVariable<Quaternion>(Quaternion.identity, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

    public NetworkVariable<Vector3> posManoDer = new NetworkVariable<Vector3>(Vector3.zero, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    public NetworkVariable<Quaternion> rotManoDer = new NetworkVariable<Quaternion>(Quaternion.identity, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

    [Header("Componentes Visuales Reales")]
    public Renderer mallaCabezaCompleta;
    public Renderer mallaTorsoCuerpo;

    [Header("Referencias Locales para Movimiento")]
    public Transform avatarHeadParent;
    public Transform avatarTorsoParent; // 🌟 NUEVO: Esto obligará al cuerpo a seguirte
    public Transform avatarLeftHand;
    public Transform avatarRightHand;

    private Transform localHead;
    private Transform localLeftHand;
    private Transform localRightHand;

    public override void OnNetworkSpawn()
    {
        if (IsOwner)
        {
            EstablecerColorInicialServerRpc(NetworkManager.Singleton.LocalClientId);
            VincularComponentesXRActivosLocal();
        }

        colorJugadorNet.OnValueChanged += (oldVal, newVal) => ActivarVisibilidadEnPartida();
        ActivarVisibilidadEnPartida();
    }

    public void VincularComponentesXRActivosLocal()
    {
        if (!IsOwner) return;

        // 1. La cabeza es estrictamente la cámara principal del visor
        if (Camera.main != null) localHead = Camera.main.transform;

        GameObject leftController = GameObject.Find("Left Controller") ?? GameObject.FindGameObjectWithTag("LeftHand");
        if (leftController != null) localLeftHand = leftController.transform;

        GameObject rightController = GameObject.Find("Right Controller") ?? GameObject.FindGameObjectWithTag("RightHand");
        if (rightController != null) localRightHand = rightController.transform;
    }

    [ServerRpc]
    private void EstablecerColorInicialServerRpc(ulong idCliente)
    {
        if (LobbyManager.Instance != null)
        {
            Color colorElegido = LobbyManager.Instance.ObtenerColorPorID(idCliente);
            colorJugadorNet.Value = new Vector4(colorElegido.r, colorElegido.g, colorElegido.b, colorElegido.a);
        }
    }

    public void ActivarVisibilidadEnPartida()
    {
        Color colorAsignado = new Color(colorJugadorNet.Value.x, colorJugadorNet.Value.y, colorJugadorNet.Value.z, colorJugadorNet.Value.w);

        // Usamos una función especial que fuerza el pintado en materiales normales o URP
        AplicarColorMaterial(mallaCabezaCompleta, colorAsignado);
        AplicarColorMaterial(mallaTorsoCuerpo, colorAsignado);

        if (IsOwner)
        {
            // Apagamos tu propia cabeza localmente para que la cámara no se bloquee por dentro del modelo
            if (mallaCabezaCompleta != null) mallaCabezaCompleta.enabled = false;
        }
    }

    private void AplicarColorMaterial(Renderer render, Color color)
    {
        if (render != null && render.material != null)
        {
            // Soporte universal de color para proyectos VR
            if (render.material.HasProperty("_BaseColor")) render.material.SetColor("_BaseColor", color);
            else render.material.color = color;
        }
    }
    
    public void ModificarPuntuacionServer(int cantidadBase)
    {
        if (!IsServer) return;
        puntuacion.Value += cantidadBase;
    }
    void Update()
    {
        if (IsOwner)
        {
            // Auto-reconexión de la cámara si la escena cambia
            if (localHead == null || localLeftHand == null || localRightHand == null)
            {
                VincularComponentesXRActivosLocal();
            }

            if (localHead && avatarHeadParent)
            {
                // Sincronizamos la cabeza con el visor real
                avatarHeadParent.position = localHead.position;
                avatarHeadParent.rotation = localHead.rotation;

                posCabeza.Value = localHead.position;
                rotCabeza.Value = localHead.rotation;

                // 🌟 SINCRONIZACIÓN DEL TORSO: Sigue a la cabeza, 40cm por debajo, sin rotar hacia arriba/abajo
                if (avatarTorsoParent)
                {
                    Vector3 posTorso = localHead.position - new Vector3(0, 0.4f, 0);
                    avatarTorsoParent.position = posTorso;
                    avatarTorsoParent.rotation = Quaternion.Euler(0, localHead.eulerAngles.y, 0);
                }
            }
            if (localLeftHand && avatarLeftHand)
            {
                avatarLeftHand.position = localLeftHand.position;
                avatarLeftHand.rotation = localLeftHand.rotation;

                posManoIzq.Value = localLeftHand.position;
                rotManoIzq.Value = localLeftHand.rotation;
            }
            if (localRightHand && avatarRightHand)
            {
                avatarRightHand.position = localRightHand.position;
                avatarRightHand.rotation = localRightHand.rotation;

                posManoDer.Value = localRightHand.position;
                rotManoDer.Value = localRightHand.rotation;
            }
        }
        else
        {
            // 🌟 LÓGICA DE MULTIJUGADOR: Qué haces cuando ves al otro jugador
            if (avatarHeadParent)
            {
                avatarHeadParent.position = posCabeza.Value;
                avatarHeadParent.rotation = rotCabeza.Value;

                if (avatarTorsoParent)
                {
                    Vector3 posTorso = posCabeza.Value - new Vector3(0, 0.4f, 0);
                    avatarTorsoParent.position = posTorso;
                    avatarTorsoParent.rotation = Quaternion.Euler(0, rotCabeza.Value.eulerAngles.y, 0);
                }
            }
            if (avatarLeftHand)
            {
                avatarLeftHand.position = posManoIzq.Value;
                avatarLeftHand.rotation = rotManoIzq.Value;
            }
            if (avatarRightHand)
            {
                avatarRightHand.position = posManoDer.Value;
                avatarRightHand.rotation = rotManoDer.Value;
            }
        }
    }
}
using Unity.Netcode;
using UnityEngine;

public class PlayerAvatarSync : NetworkBehaviour
{
    [Header("Componentes Visuales Reales (Mallas)")]
    public Renderer mallaCabezaCompleta;
    public Renderer mallaTorsoCuerpo;

    [Header("Referencias Locales para Movimiento")]
    public Transform avatarHeadParent; // Nodo HeadCalibration
    public Transform avatarTorsoParent; // Nodo TorsoCalibration
    public Transform avatarLeftHand;
    public Transform avatarRightHand;

    private Transform localHead;
    private Transform localLeftHand;
    private Transform localRightHand;
    private Renderer[] mallasDelAvatar;
    private PlayerNetworkState networkState;

    void Awake()
    {
        mallasDelAvatar = GetComponentsInChildren<Renderer>();
        CambiarVisibilidadAvatar(false);
    }

    public override void OnNetworkSpawn()
    {
        networkState = GetComponent<PlayerNetworkState>();

        if (IsOwner)
        {
            if (Camera.main != null) localHead = Camera.main.transform;

            GameObject leftController = GameObject.Find("Left Controller");
            if (leftController) localLeftHand = leftController.transform;

            GameObject rightController = GameObject.Find("Right Controller");
            if (rightController) localRightHand = rightController.transform;
        }

        if (networkState != null)
        {
            networkState.colorJugadorNet.OnValueChanged += (oldVal, newVal) => ActivarVisibilidadEnPartida();
        }
        ActivarVisibilidadEnPartida();
    }

    public void ActivarVisibilidadEnPartida()
    {
        CambiarVisibilidadAvatar(true);

        if (LobbyManager.Instance != null)
        {
            Color colorAsignado = LobbyManager.Instance.ObtenerColorPorID(OwnerClientId);

            if (mallaCabezaCompleta != null && mallaCabezaCompleta.material != null)
            {
                if (mallaCabezaCompleta.material.HasProperty("_BaseColor")) mallaCabezaCompleta.material.SetColor("_BaseColor", colorAsignado);
                else mallaCabezaCompleta.material.color = colorAsignado;
            }

            if (mallaTorsoCuerpo != null && mallaTorsoCuerpo.material != null)
            {
                if (mallaTorsoCuerpo.material.HasProperty("_BaseColor")) mallaTorsoCuerpo.material.SetColor("_BaseColor", colorAsignado);
                else mallaTorsoCuerpo.material.color = colorAsignado;
            }
        }
    }

    private void CambiarVisibilidadAvatar(bool visible)
    {
        if (mallasDelAvatar == null) return;
        foreach (var render in mallasDelAvatar)
        {
            if (render != null) render.enabled = visible;
        }
    }

    void Update()
    {
        if (networkState == null) return;

        if (IsOwner)
        {
            if (localHead)
            {
                transform.position = localHead.position;
                transform.rotation = localHead.rotation;

                networkState.posCabeza.Value = localHead.position;
                networkState.rotCabeza.Value = localHead.rotation;
            }

            if (avatarTorsoParent && localHead)
            {
                Vector3 posTorso = localHead.position - new Vector3(0, 0.4f, 0);
                avatarTorsoParent.position = posTorso;
                avatarTorsoParent.rotation = Quaternion.Euler(0, localHead.eulerAngles.y, 0);
            }

            if (localLeftHand && avatarLeftHand)
            {
                avatarLeftHand.position = localLeftHand.position;
                avatarLeftHand.rotation = localLeftHand.rotation;
                networkState.posManoIzq.Value = localLeftHand.position;
                networkState.rotManoIzq.Value = localLeftHand.rotation;
            }

            if (localRightHand && avatarRightHand)
            {
                avatarRightHand.position = localRightHand.position;
                avatarRightHand.rotation = localRightHand.rotation;
                networkState.posManoDer.Value = localRightHand.position;
                networkState.rotManoDer.Value = localRightHand.rotation;
            }
        }
        else
        {
            // Sincronización en pantallas de otros jugadores
            transform.position = networkState.posCabeza.Value;
            transform.rotation = networkState.rotCabeza.Value;

            if (avatarTorsoParent)
            {
                Vector3 posTorso = networkState.posCabeza.Value - new Vector3(0, 0.4f, 0);
                avatarTorsoParent.position = posTorso;
                avatarTorsoParent.rotation = Quaternion.Euler(0, networkState.rotCabeza.Value.eulerAngles.y, 0);
            }

            if (avatarLeftHand)
            {
                avatarLeftHand.position = networkState.posManoIzq.Value;
                avatarLeftHand.rotation = networkState.rotManoIzq.Value;
            }

            if (avatarRightHand)
            {
                avatarRightHand.position = networkState.posManoDer.Value;
                avatarRightHand.rotation = networkState.rotManoDer.Value;
            }
        }
    }
}
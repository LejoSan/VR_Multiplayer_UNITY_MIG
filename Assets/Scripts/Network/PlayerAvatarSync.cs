using Unity.Netcode;
using UnityEngine;

public class PlayerAvatarSync : NetworkBehaviour
{
    [Header("Partes del Avatar de Red (Arrastra aquí)")]
    public Transform avatarHead;
    public Transform avatarLeftHand;
    public Transform avatarRightHand;

    [Header("Referencias Locales (Se buscan solas)")]
    private Transform localHead;
    private Transform localLeftHand;
    private Transform localRightHand;

    private Renderer[] mallasDelAvatar;

    // 🌟 NUEVO: Variable de red para sincronizar el color mediante la ID del dueño
    private NetworkVariable<ulong> idDueñoAvatar = new NetworkVariable<ulong>(999, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    void Awake()
    {
        mallasDelAvatar = GetComponentsInChildren<Renderer>();
        CambiarVisibilidadAvatar(false);
    }

    public override void OnNetworkSpawn()
    {
        //  NUEVO: Si soy el servidor, guardo mi ID oficial en la red
        if (IsServer)
        {
            idDueñoAvatar.Value = OwnerClientId;
        }

        //  NUEVO: Nos suscribimos al cambio para pintar el color en todas las pantallas
        idDueñoAvatar.OnValueChanged += (viejoID, nuevoID) => AplicarColorDeRed(nuevoID);

        if (IsOwner)
        {
            if (Camera.main != null) localHead = Camera.main.transform;

            GameObject leftController = GameObject.Find("Left Controller");
            if (leftController) localLeftHand = leftController.transform;

            GameObject rightController = GameObject.Find("Right Controller");
            if (rightController) localRightHand = rightController.transform;
        }
        else
        {
            // Si el dato ya llegó del servidor antes de spawnear, pintamos de una vez
            AplicarColorDeRed(idDueñoAvatar.Value);
        }
    }

    public void ActivarVisibilidadEnPartida()
    {
        CambiarVisibilidadAvatar(true);

        // Forzamos el pintado al activarse la partida usando el valor de red
        AplicarColorDeRed(idDueñoAvatar.Value);

        if (IsOwner && avatarHead != null)
        {
            Renderer headRender = avatarHead.GetComponent<Renderer>();
            if (headRender != null) headRender.enabled = false;
        }
    }

    // 🌟 ACTUALIZADO: Ahora recibe la ID directamente de la NetworkVariable
    private void AplicarColorDeRed(ulong idDelDueño)
    {
        if (LobbyManager.Instance != null && idDelDueño != 999)
        {
            Color colorAsignado = LobbyManager.Instance.ObtenerColorPorID(idDelDueño);

            if (avatarHead != null && avatarHead.GetComponent<Renderer>() != null)
            {
                avatarHead.GetComponent<Renderer>().material.color = colorAsignado;
            }

            if (avatarLeftHand != null && avatarLeftHand.GetComponent<Renderer>() != null)
                avatarLeftHand.GetComponent<Renderer>().material.color = colorAsignado;

            if (avatarRightHand != null && avatarRightHand.GetComponent<Renderer>() != null)
                avatarRightHand.GetComponent<Renderer>().material.color = colorAsignado;
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
        if (IsOwner)
        {
            if (localHead)
            {
                avatarHead.position = localHead.position;
                avatarHead.rotation = localHead.rotation;
            }
            if (localLeftHand)
            {
                avatarLeftHand.position = localLeftHand.position;
                avatarLeftHand.rotation = localLeftHand.rotation;
            }
            if (localRightHand)
            {
                avatarRightHand.position = localRightHand.position;
                avatarRightHand.rotation = localRightHand.rotation;
            }
        }
    }
}
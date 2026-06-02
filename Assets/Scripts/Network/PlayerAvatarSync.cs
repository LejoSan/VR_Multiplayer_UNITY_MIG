using Unity.Netcode;
using UnityEngine;

public class PlayerAvatarSync : NetworkBehaviour
{
    [Header("Componentes Visuales Reales (Mallas)")]
    public Renderer mallaCabezaCompleta;
    // 🌟 NUEVO: Si tienes una malla para el Torso/Cuerpo, la arrastras aquí para pintarla del mismo color
    public Renderer mallaTorsoCuerpo;

    [Header("Referencias Locales para Movimiento")]
    public Transform avatarHeadParent; // El contenedor/padre de la cabeza
    public Transform avatarLeftHand;
    public Transform avatarRightHand;
    // 🌟 NUEVO: Arrastra el objeto Padre del Torso aquí si quieres que flote justo debajo de la cabeza
    public Transform avatarTorsoParent;

    private Transform localHead;
    private Transform localLeftHand;
    private Transform localRightHand;
    private Renderer[] mallasDelAvatar;

    void Awake()
    {
        mallasDelAvatar = GetComponentsInChildren<Renderer>();
        CambiarVisibilidadAvatar(false);
    }

    public override void OnNetworkSpawn()
    {
        if (IsOwner)
        {
            if (Camera.main != null) localHead = Camera.main.transform;

            GameObject leftController = GameObject.Find("Left Controller");
            if (leftController) localLeftHand = leftController.transform;

            GameObject rightController = GameObject.Find("Right Controller");
            if (rightController) localRightHand = rightController.transform;
        }
    }

    public void ActivarVisibilidadEnPartida()
    {
        CambiarVisibilidadAvatar(true);

        // 🌟 PINTADO MULTIJUGADOR SEGURO (Mantiene tu lógica de color de equipo):
        if (LobbyManager.Instance != null)
        {
            Color colorAsignado = LobbyManager.Instance.ObtenerColorPorID(OwnerClientId);

            // Pintamos la Cabeza
            if (mallaCabezaCompleta != null)
            {
                mallaCabezaCompleta.material.color = colorAsignado;
                Debug.Log($"[AVATAR] Malla de la cara del jugador {OwnerClientId} pintada.");
            }

            // Pintamos el Torso/Cuerpo con el mismo color asignado
            if (mallaTorsoCuerpo != null)
            {
                mallaTorsoCuerpo.material.color = colorAsignado;
            }
        }

        // 🚨 EL APAGADO DEL DUEÑO (Para que no veas tu propio cuerpo flotando en tus ojos)
        // Quita los comentarios (//) si quieres que en tus gafas sea invisible, pero tus amigos sí te vean.
        /*
        if (IsOwner)
        {
            if (mallaCabezaCompleta != null) mallaCabezaCompleta.enabled = false;
            if (mallaTorsoCuerpo != null) mallaTorsoCuerpo.enabled = false;
        }
        */
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
            // 🌟 EL ENFOQUE DE VALEM TUTORIALS:
            // El objeto raíz copia exactamente la posición y rotación global de tu visor VR.
            // Toda la inclinación, giro y traslación se hereda limpiamente hacia abajo.
            if (localHead)
            {
                transform.position = localHead.position;
                transform.rotation = localHead.rotation;
            }

            // Las manos copian de forma local e independiente sus controladores físicos
            if (localLeftHand && avatarLeftHand)
            {
                avatarLeftHand.position = localLeftHand.position;
                avatarLeftHand.rotation = localLeftHand.rotation;
            }
            if (localRightHand && avatarRightHand)
            {
                avatarRightHand.position = localRightHand.position;
                avatarRightHand.rotation = localRightHand.rotation;
            }
        }
    }
}
using Unity.Netcode;
using UnityEngine;

public class PlayerAvatarSync : NetworkBehaviour
{
    [Header("Componentes Visuales Reales (Cambió a Renderer)")]
    // 🌟 CAMBIO CLAVE: Ahora pedimos directamente el Renderer (la malla) en vez del Transform
    public Renderer mallaCabezaCompleta;

    [Header("Referencias Locales para Movimiento")]
    // Estos se quedan como Transform porque se usan para copiar la posición de los mandos
    public Transform avatarHeadParent;
    public Transform avatarLeftHand;
    public Transform avatarRightHand;

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

        // 🌟 PINTADO DIRECTO Y SEGURO:
        if (LobbyManager.Instance != null)
        {
            Color colorAsignado = LobbyManager.Instance.ObtenerColorPorID(OwnerClientId);

            // Al ser 'mallaCabezaCompleta' un Renderer nativo, le cambiamos el color al instante sin rodeos
            if (mallaCabezaCompleta != null)
            {
                mallaCabezaCompleta.material.color = colorAsignado;
                Debug.Log($"[AVATAR] Malla de la cara del jugador {OwnerClientId} pintada con éxito.");
            }
        }

        // Si soy el dueño, oculto la malla para que no me tape la vista en VR
        // (Recuerda comentar estas líneas si quieres ver tu propio color al probar tú solo como Host)
        //if (IsOwner && mallaCabezaCompleta != null)
        //{
        //    mallaCabezaCompleta.enabled = false;
        //}
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
            // El objeto 'avatarHeadParent' (el objeto vacío 'Cabeza') sigue sirviendo para mover el conjunto
            if (localHead && avatarHeadParent)
            {
                avatarHeadParent.position = localHead.position;
                avatarHeadParent.rotation = localHead.rotation;
            }
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
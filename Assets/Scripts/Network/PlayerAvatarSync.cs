using Unity.Netcode;
using UnityEngine;

public class PlayerAvatarSync : NetworkBehaviour
{
    [Header("Componentes Visuales Reales (Mallas)")]
    public Renderer mallaCabezaCompleta;
    public Renderer mallaTorsoCuerpo;

    [Header("Referencias Locales para Movimiento")]
    public Transform avatarHeadParent;  // Nodo HeadCalibration
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
        // Al despertar, aseguramos que nazcan completamente invisibles
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

        // 🌟 REGLA DE LOBBY: Cuando cambie el color en el lobby, solo actualizamos el material, NO encendemos la malla
        if (networkState != null)
        {
            networkState.colorJugadorNet.OnValueChanged += (oldVal, newVal) => ActualizarColorMateriales();
        }

        // Forzamos el apagado estricto en el Lobby
        CambiarVisibilidadAvatar(false);

        // Plan de emergencia: Si el jugador entra tarde a la partida y ya están jugando el Bloque 4, lo activamos inmediatamente
        if (MainGameManager.Instance != null && MainGameManager.Instance.estadoActual.Value == MainGameManager.EstadoJuego.FaseGameplay)
        {
            ActivarVisibilidadEnPartida();
        }
    }

    // 🌟 NUEVO MÉTODO: Cambia el color de los materiales en la memoria sin hacer visible al robot todavía
    private void ActualizarColorMateriales()
    {
        if (networkState == null) return;

        Vector4 vectorColor = networkState.colorJugadorNet.Value;
        Color colorAsignado = new Color(vectorColor.x, vectorColor.y, vectorColor.z, vectorColor.w);

        if (colorAsignado == Color.white && LobbyManager.Instance != null)
        {
            colorAsignado = LobbyManager.Instance.ObtenerColorPorID(OwnerClientId);
        }

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

    // Este método solo será invocado de forma oficial por el GameplayManager al iniciar el Bloque 4
    //public void ActivarVisibilidadEnPartida()
    //{
    //    // 1. Aseguramos que los materiales tengan el color correcto
    //    ActualizarColorMateriales();

    //    // 2. ¡AHORA SÍ! Hacemos aparecer al robot oficialmente en el mapa
    //    CambiarVisibilidadAvatar(true);

    //    // 3. Tu propia restricción de vista VR en primera persona
    //    //if (IsOwner)
    //    //{
    //    //    if (mallaCabezaCompleta != null) mallaCabezaCompleta.enabled = false;
    //    //    if (mallaTorsoCuerpo != null) mallaTorsoCuerpo.enabled = false;
    //    //}
    //}

    // Este método solo será invocado de forma oficial por el GameplayManager al iniciar el Bloque 4
    public void ActivarVisibilidadEnPartida()
    {
        // 1. Aseguramos que los materiales tengan el color correcto para todos
        ActualizarColorMateriales();

        // 2. ¡AHORA SÍ! Hacemos aparecer a todos los robots en el mapa
        CambiarVisibilidadAvatar(false);

        // 3. REGLA VR UNIVERSAL: Cada jugador se vuelve invisible para SÍ MISMO
        // Esto hace que nadie vea su propia cabeza por dentro ni su torso, pero vea a todos los demás.
        if (IsOwner)
        {
            if (mallaCabezaCompleta != null) mallaCabezaCompleta.enabled = false;
            if (mallaTorsoCuerpo != null) mallaTorsoCuerpo.enabled = false;

            Debug.Log("[VR] Ocultando mallas locales para el dueño del avatar. ¡Vista despejada para disparar!");
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

                if (avatarHeadParent)
                {
                    avatarHeadParent.position = localHead.position;
                    avatarHeadParent.rotation = localHead.rotation;
                }

                if (avatarTorsoParent)
                {
                    Vector3 posTorso = localHead.position - new Vector3(0, 0.4f, 0);
                    avatarTorsoParent.position = posTorso;
                    avatarTorsoParent.rotation = Quaternion.Euler(0, localHead.eulerAngles.y, 0);
                }

                networkState.posCabeza.Value = localHead.position;
                networkState.rotCabeza.Value = localHead.rotation;
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
            if (avatarHeadParent)
            {
                avatarHeadParent.position = networkState.posCabeza.Value;
                avatarHeadParent.rotation = networkState.rotCabeza.Value;
            }

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
using UnityEngine;
using Unity.Netcode;

public class AutoEquipWeapon : NetworkBehaviour
{
    [Header("Configuración de Seguimiento")]
    [Tooltip("Nombre exacto del objeto de la mano dentro de XR_Origin_LOCAL")]
    public string nombreObjetoMano = "Right Controller";

    [Header("Ajuste Fino (Offset)")]
    [Tooltip("Ajusta si el arma queda desencajada de la mano")]
    public Vector3 offsetPosicion = Vector3.zero;
    public Vector3 offsetRotacion = Vector3.zero;

    private Transform manoTransform;
    private Rigidbody rb;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = true;
            rb.useGravity = false;
        }
    }

    public override void OnNetworkSpawn()
    {
        if (IsOwner || !IsServer)
        {
            BuscarMano();
        }
    }

    void Start()
    {
        if (manoTransform == null)
        {
            BuscarMano();
        }
    }

    private void BuscarMano()
    {
        GameObject origin = GameObject.Find("XR_Origin_LOCAL");
        if (origin != null)
        {
            Transform[] todosLosHijos = origin.GetComponentsInChildren<Transform>(true);
            foreach (Transform t in todosLosHijos)
            {
                if (t.name.Equals(nombreObjetoMano, System.StringComparison.OrdinalIgnoreCase))
                {
                    manoTransform = t;
                    break;
                }
            }
        }

        if (manoTransform == null)
        {
            Debug.LogWarning($"[AutoEquip] No se encontró '{nombreObjetoMano}' dentro de XR_Origin_LOCAL.");
        }
    }

    void LateUpdate()
    {
        // Seguir la mano frame a frame sin romper la estructura de red
        if (manoTransform != null)
        {
            transform.position = manoTransform.TransformPoint(offsetPosicion);
            transform.rotation = manoTransform.rotation * Quaternion.Euler(offsetRotacion);
        }
    }
}
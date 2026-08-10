using UnityEngine;

public class MobileSpectatorCamera : MonoBehaviour
{
    [Header("--- Sensibilidad de Controles Táctiles ---")]
    public float sensRotacion = 0.15f;
    public float sensDesplazamiento = 0.008f;
    public float sensPinchZoom = 0.015f;

    [Header("--- Limites de Rotación ---")]
    public float pitchMinimo = -80f;
    public float pitchMaximo = 80f;

    private float pitch = 0f;
    private float yaw = 0f;

    void OnEnable()
    {
        Vector3 rotActual = transform.eulerAngles;
        pitch = rotActual.x;
        yaw = rotActual.y;
    }

    void Update()
    {
        // 1 DEDO: Rotar la cámara (Mirar alrededor)
        if (Input.touchCount == 1)
        {
            Touch touch = Input.GetTouch(0);
            if (touch.phase == TouchPhase.Moved)
            {
                yaw += touch.deltaPosition.x * sensRotacion;
                pitch -= touch.deltaPosition.y * sensRotacion;
                pitch = Mathf.Clamp(pitch, pitchMinimo, pitchMaximo);

                transform.rotation = Quaternion.Euler(pitch, yaw, 0f);
            }
        }
        // 2 DEDOS: Desplazar posición de la cámara (Volar por el escenario)
        else if (Input.touchCount == 2)
        {
            Touch t0 = Input.GetTouch(0);
            Touch t1 = Input.GetTouch(1);

            if (t0.phase == TouchPhase.Moved || t1.phase == TouchPhase.Moved)
            {
                Vector2 t0Prev = t0.position - t0.deltaPosition;
                Vector2 t1Prev = t1.position - t1.deltaPosition;

                // 1. Desplazamiento lateral y vertical (Pan)
                Vector2 centroActual = (t0.position + t1.position) * 0.5f;
                Vector2 centroPrevio = (t0Prev + t1Prev) * 0.5f;
                Vector2 deltaCentro = centroActual - centroPrevio;

                Vector3 movimientoHorizontal = -transform.right * (deltaCentro.x * sensDesplazamiento);
                Vector3 movimientoVertical = -transform.up * (deltaCentro.y * sensDesplazamiento);
                transform.position += (movimientoHorizontal + movimientoVertical);

                // 2. Acercar / Alejar (Pinch to Zoom / Avance 3D)
                float distPrevia = (t0Prev - t1Prev).magnitude;
                float distActual = (t0.position - t1.position).magnitude;
                float deltaDistancia = distActual - distPrevia;

                transform.position += transform.forward * (deltaDistancia * sensPinchZoom);
            }
        }
    }
}
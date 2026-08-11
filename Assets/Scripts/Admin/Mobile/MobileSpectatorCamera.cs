using UnityEngine;

public class MobileSpectatorCamera : MonoBehaviour
{
    [Header("--- Sensibilidad de Movimiento ---")]
    public float sensRotacion = 0.12f;         // 1 Dedo: Girar mirada
    public float sensDesplazamiento = 0.006f;   // 2 Dedos arrastrando: Mover cámara por el mapa
    public float sensPinchZoom = 0.01f;        // 2 Dedos pellizcando: Avanzar / Retroceder
    public float suavizadoInercia = 8f;        // Inercia de recorrido virtual

    [Header("--- Limites de Inclinación Vertical ---")]
    public float pitchMinimo = -80f;
    public float pitchMaximo = 80f;

    private float targetPitch = 0f;
    private float targetYaw = 0f;
    private float currentPitch = 0f;
    private float currentYaw = 0f;

    private Vector3 targetPosicion;

    void OnEnable()
    {
        Vector3 rotActual = transform.eulerAngles;
        currentPitch = targetPitch = rotActual.x;
        currentYaw = targetYaw = rotActual.y;
        targetPosicion = transform.position;
    }

    void Update()
    {
        // 👆 1 DEDO: Rotar la mirada (Giro 360° alrededor)
        if (Input.touchCount == 1)
        {
            Touch touch = Input.GetTouch(0);
            if (touch.phase == TouchPhase.Moved)
            {
                targetYaw += touch.deltaPosition.x * sensRotacion;
                targetPitch -= touch.deltaPosition.y * sensRotacion;
                targetPitch = Mathf.Clamp(targetPitch, pitchMinimo, pitchMaximo);
            }
        }
        // ✌️ 2 DEDOS: Movimiento (Pan) y Zoom (Pinch) simultáneos
        else if (Input.touchCount == 2)
        {
            Touch t0 = Input.GetTouch(0);
            Touch t1 = Input.GetTouch(1);

            if (t0.phase == TouchPhase.Moved || t1.phase == TouchPhase.Moved)
            {
                Vector2 t0Prev = t0.position - t0.deltaPosition;
                Vector2 t1Prev = t1.position - t1.deltaPosition;

                // 1. TRASLACIÓN (PAN): Arrastrar los 2 dedos juntos mueve la cámara por el espacio
                Vector2 centroActual = (t0.position + t1.position) * 0.5f;
                Vector2 centroPrevio = (t0Prev + t1Prev) * 0.5f;
                Vector2 deltaCentro = centroActual - centroPrevio;

                Vector3 movLateral = -transform.right * (deltaCentro.x * sensDesplazamiento);
                Vector3 movVertical = -transform.up * (deltaCentro.y * sensDesplazamiento);

                targetPosicion += (movLateral + movVertical);

                // 2. ZOOM (PINCH): Separar o juntar los 2 dedos avanza/retrocede en la dirección donde miras
                float distPrevia = (t0Prev - t1Prev).magnitude;
                float distActual = (t0.position - t1.position).magnitude;
                float deltaDistancia = distActual - distPrevia;

                targetPosicion += transform.forward * (deltaDistancia * sensPinchZoom);
            }
        }

        // 🌟 APLICAR SUAVIZADO CINEMÁTICO
        currentPitch = Mathf.Lerp(currentPitch, targetPitch, Time.deltaTime * suavizadoInercia);
        currentYaw = Mathf.Lerp(currentYaw, targetYaw, Time.deltaTime * suavizadoInercia);

        transform.rotation = Quaternion.Euler(currentPitch, currentYaw, 0f);
        transform.position = Vector3.Lerp(transform.position, targetPosicion, Time.deltaTime * suavizadoInercia);
    }

    // Método para devolver la cámara al origen al reiniciar
    public void ResetearCamara(Vector3 posInicial, Quaternion rotInicial)
    {
        transform.position = posInicial;
        transform.rotation = rotInicial;
        targetPosicion = posInicial;

        Vector3 euler = rotInicial.eulerAngles;
        currentPitch = targetPitch = euler.x;
        currentYaw = targetYaw = euler.y;
    }
}
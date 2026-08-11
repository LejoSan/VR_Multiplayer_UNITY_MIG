using UnityEngine;

public class MobileSpectatorCamera : MonoBehaviour
{
    [Header("--- Referencias Joysticks Visuales ---")]
    public VirtualJoystick joystickIzquierdo; // Para Moverse (Adelante/Atrás/Lados)
    public VirtualJoystick joystickDerecho;   // Para Rotar la vista (Mirar alrededor)

    [Header("--- Velocidad de Control ---")]
    public float velocidadMovimiento = 6f;
    public float velocidadRotacion = 80f;

    [Header("--- Limites de Rotación Vertical ---")]
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
        // 1. ROTACIÓN CON JOYSTICK DERECHO (Mirar)
        if (joystickDerecho != null)
        {
            Vector2 rotInput = joystickDerecho.InputVector;
            yaw += rotInput.x * velocidadRotacion * Time.deltaTime;
            pitch -= rotInput.y * velocidadRotacion * Time.deltaTime;
            pitch = Mathf.Clamp(pitch, pitchMinimo, pitchMaximo);

            transform.rotation = Quaternion.Euler(pitch, yaw, 0f);
        }

        // 2. MOVIMIENTO CON JOYSTICK IZQUIERDO (Volar por el mapa)
        if (joystickIzquierdo != null)
        {
            Vector2 moveInput = joystickIzquierdo.InputVector;

            // Avance 3D en la dirección hacia donde mira la cámara
            Vector3 direccion = (transform.forward * moveInput.y + transform.right * moveInput.x);
            transform.position += direccion * velocidadMovimiento * Time.deltaTime;
        }
    }
}
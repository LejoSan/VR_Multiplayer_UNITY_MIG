using UnityEngine;
using TMPro;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP; // Requerido para modificar el UnityTransport

public class NetcodeConfigUI : MonoBehaviour
{
    [Header("--- Componentes de la Interfaz (UI) ---")]
    public GameObject panelIP;                 // El contenedor de la UI de configuración
    public TMP_InputField inputFieldIP;         // Casilla para escribir la IP
    public TMP_InputField inputFieldPuerto;     // Casilla para escribir el Puerto

    private UnityTransport transport;

    void Start()
    {
        // 1. Localizamos el componente de transporte de red de forma segura
        if (NetworkManager.Singleton != null)
        {
            transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
        }

        // 2. Cargamos los valores actuales por defecto en los cuadros de texto
        if (transport != null)
        {
            if (inputFieldIP != null) inputFieldIP.text = transport.ConnectionData.Address;
            if (inputFieldPuerto != null) inputFieldPuerto.text = transport.ConnectionData.Port.ToString();
        }

        // Aseguramos que el panel inicie apagado para no estorbar en el Lobby
        if (panelIP != null) panelIP.SetActive(false);
    }

    // --- FUNCIÓN 1: El botón "IP" principal que enciende y apaga el panel ---
    public void BTN_TogglePanelIP()
    {
        if (panelIP != null)
        {
            bool estadoActual = panelIP.activeSelf;
            panelIP.SetActive(!estadoActual); // Si está encendido lo apaga, y viceversa

            // Al encenderlo, refrescamos el texto con lo que tenga el transport actualmente
            if (!estadoActual && transport != null)
            {
                if (inputFieldIP != null) inputFieldIP.text = transport.ConnectionData.Address;
                if (inputFieldPuerto != null) inputFieldPuerto.text = transport.ConnectionData.Port.ToString();
            }
        }
    }

    // --- FUNCIÓN 2: El botón de "Guardar / Aceptar" dentro del panel ---
    public void BTN_GuardarConfiguracion()
    {
        if (transport == null)
        {
            Debug.LogError("[NETWORK UI] No se encontró el componente UnityTransport en el NetworkManager.");
            return;
        }

        // 1. Extraemos y validamos la IP
        string nuevaIP = inputFieldIP != null ? inputFieldIP.text.Trim() : "0.0.0.0";

        // 2. Extraemos y validamos el Puerto (convertimos el texto a número seguro ushort)
        ushort nuevoPuerto = 7778; // Puerto por defecto de Netcode
        if (inputFieldPuerto != null && ushort.TryParse(inputFieldPuerto.text.Trim(), out ushort puertoParseado))
        {
            nuevoPuerto = puertoParseado;
        }

        // 3. Inyectamos los datos en caliente al motor de Netcode
        transport.ConnectionData.Address = nuevaIP;
        transport.ConnectionData.Port = nuevoPuerto;

        Debug.Log($"<color=green><b>[CONFIGURACIÓN RED GUARDADA]</b></color> Nueva dirección establecida -> IP: {nuevaIP} | Puerto: {nuevoPuerto}");

        // 4. Apagamos el panel automáticamente al aceptar para dar feedback de éxito
        if (panelIP != null) panelIP.SetActive(false);
    }
}
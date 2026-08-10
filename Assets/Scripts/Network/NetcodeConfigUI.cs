using UnityEngine;
using TMPro;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;

public class NetcodeConfigUI : MonoBehaviour
{
    [Header("--- Componentes de la Interfaz (UI) ---")]
    public GameObject panelIP;                  // El contenedor de la UI de configuración
    public TMP_InputField inputFieldIP;         // Casilla para escribir la IP
    public TMP_InputField inputFieldPuerto;     // Casilla para escribir el Puerto

    [Header("--- Valores por Defecto ---")]
    public string ipPorDefecto = "192.168.20.152";
    public ushort puertoPorDefecto = 7778;

    private UnityTransport transport;

    void Start()
    {
        // 1. Obtener referencia al transporte de Netcode de forma segura
        if (NetworkManager.Singleton != null)
        {
            transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
        }

        // 2. Leer la IP y Puerto guardados previamente en memoria local
        string ipGuardada = PlayerPrefs.GetString("SAVED_HOST_IP", ipPorDefecto);
        ushort puertoGuardado = (ushort)PlayerPrefs.GetInt("SAVED_HOST_PORT", puertoPorDefecto);

        // 3. Aplicar al backend de red (UnityTransport) al arrancar
        if (transport != null)
        {
            transport.ConnectionData.Address = ipGuardada;
            transport.ConnectionData.Port = puertoGuardado;
        }

        // 4. Rellenar los cuadros de texto con los valores activos
        if (inputFieldIP != null) inputFieldIP.text = ipGuardada;
        if (inputFieldPuerto != null) inputFieldPuerto.text = puertoGuardado.ToString();

        // 5. Iniciar el panel apagado por defecto
        if (panelIP != null) panelIP.SetActive(false);
    }

    // --- BTN: Abrir / Cerrar Teclado IP ---
    public void BTN_TogglePanelIP()
    {
        if (panelIP != null)
        {
            bool estadoActual = panelIP.activeSelf;
            panelIP.SetActive(!estadoActual);

            // Refrescar inputs al abrir con los datos reales que tenga el transporte
            if (!estadoActual)
            {
                if (transport == null && NetworkManager.Singleton != null)
                {
                    transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
                }

                if (transport != null)
                {
                    if (inputFieldIP != null) inputFieldIP.text = transport.ConnectionData.Address;
                    if (inputFieldPuerto != null) inputFieldPuerto.text = transport.ConnectionData.Port.ToString();
                }
            }
        }
    }

    // --- BTN: Guardar / Aceptar Configuración ---
    public void BTN_GuardarConfiguracion()
    {
        // Seguro por si el transporte no se asignó en Start
        if (transport == null && NetworkManager.Singleton != null)
        {
            transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
        }

        // 1. Validar la nueva IP
        string nuevaIP = (inputFieldIP != null && !string.IsNullOrEmpty(inputFieldIP.text))
            ? inputFieldIP.text.Trim()
            : ipPorDefecto;

        // 2. Validar el nuevo Puerto (convertir texto a ushort)
        ushort nuevoPuerto = puertoPorDefecto;
        if (inputFieldPuerto != null && ushort.TryParse(inputFieldPuerto.text.Trim(), out ushort puertoParseado))
        {
            nuevoPuerto = puertoParseado;
        }

        // 3. 🔬 INYECCIÓN EN EL BACKEND (Motor de Netcode)
        if (transport != null)
        {
            transport.ConnectionData.Address = nuevaIP;
            transport.ConnectionData.Port = nuevoPuerto;
        }

        // 4. 💾 GUARDADO PERMANENTE EN MEMORIA DEL VISOR
        PlayerPrefs.SetString("SAVED_HOST_IP", nuevaIP);
        PlayerPrefs.SetInt("SAVED_HOST_PORT", nuevoPuerto);
        PlayerPrefs.Save();

        // 5. 🌟 ACTUALIZACIÓN VISUAL EN TIEMPO REAL EN PANTALLA
        if (LobbyManager.Instance != null)
        {
            LobbyManager.Instance.ActualizarTextoIPEnPantalla(nuevaIP);
        }

        // 6. 🔍 LOG DE AUDITORÍA EN CONSOLA
        Debug.Log($"<color=green><b>[BACKEND RED ACTUALIZADO]</b></color> Conexión fijada a -> IP: <b>{nuevaIP}</b> | Puerto: <b>{nuevoPuerto}</b>");

        // 7. Apagar el panel de teclado
        if (panelIP != null) panelIP.SetActive(false);
    }
}
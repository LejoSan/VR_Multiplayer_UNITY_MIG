using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactors; // Soporte para interactores XRI

public class VRHandControllerSwitcher : MonoBehaviour
{
    public static VRHandControllerSwitcher Instance;

    [Header("--- Modelos de Mandos / Joysticks ---")]
    public GameObject mandoIzquierdo;
    public GameObject mandoDerecho;

    [Header("--- Modelos de Manos (Asset) ---")]
    public GameObject manoIzquierda;
    public GameObject manoDerecha;

    [Header("--- Interactores a Desactivar en Gameplay ---")]
    public GameObject nearFarInteractorIzquierdo;
    public GameObject nearFarInteractorDerecho;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        ActivarModoMandos();
    }

    // 🎮 Activa mandos e interactores
    public void ActivarModoMandos()
    {
        if (mandoIzquierdo != null) mandoIzquierdo.SetActive(true);
        if (mandoDerecho != null) mandoDerecho.SetActive(true);

        if (manoIzquierda != null) manoIzquierda.SetActive(false);
        if (manoDerecha != null) manoDerecha.SetActive(false);

        // Activar GameObjects y componentes de interacción
        SetInteractorEstado(nearFarInteractorIzquierdo, true);
        SetInteractorEstado(nearFarInteractorDerecho, true);

        Debug.Log("<color=yellow>[VR SWITCHER]</color> Modo Mandos + Near/Far Interactors Activados.");
    }

    // 🖐️ Oculta mandos e interactores en Gameplay
    public void ActivarModoManos()
    {
        if (mandoIzquierdo != null) mandoIzquierdo.SetActive(false);
        if (mandoDerecho != null) mandoDerecho.SetActive(false);

        if (manoIzquierda != null) manoIzquierda.SetActive(true);
        if (manoDerecha != null) manoDerecha.SetActive(true);

        // Desactivar GameObjects y componentes de interacción
        SetInteractorEstado(nearFarInteractorIzquierdo, false);
        SetInteractorEstado(nearFarInteractorDerecho, false);

        Debug.Log("<color=green>[VR SWITCHER]</color> Modo Manos Activado - Near/Far Interactors Bloqueados.");
    }

    // 🚫 Oculta absolutamente todo (Manos, Mandos e Interactores) para la pantalla final
    public void OcultarTodo()
    {
        if (mandoIzquierdo != null) mandoIzquierdo.SetActive(false);
        if (mandoDerecho != null) mandoDerecho.SetActive(false);

        if (manoIzquierda != null) manoIzquierda.SetActive(false);
        if (manoDerecha != null) manoDerecha.SetActive(false);

        SetInteractorEstado(nearFarInteractorIzquierdo, false);
        SetInteractorEstado(nearFarInteractorDerecho, false);

        Debug.Log("<color=red>[VR SWITCHER]</color> Manos y Mandos Ocultados para la Pantalla Final.");
    }

    // 🔧 Método auxiliar para apagar tanto el GameObject como sus componentes de rayo
    private void SetInteractorEstado(GameObject interactorObj, bool estado)
    {
        if (interactorObj == null) return;

        // 1. Apagar/Encender el GameObject
        interactorObj.SetActive(estado);

        // 2. Apagar/Encender el componente script de interactor si existe (evita reactivación por XR Interaction Group)
        var interactorComp = interactorObj.GetComponent<MonoBehaviour>();
        if (interactorComp != null)
        {
            interactorComp.enabled = estado;
        }

        // 3. Ocultar/Mostrar la línea visual (rayo láser) si la tiene
        var lineVisual = interactorObj.GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactors.Visuals.XRInteractorLineVisual>();
        if (lineVisual != null)
        {
            lineVisual.enabled = estado;
        }
    }
}
using UnityEngine;
using UnityEngine.InputSystem;

public class VRHandAnimatorController : MonoBehaviour
{
    public Animator animator;
    public InputActionReference triggerAction; // Asigna XRI RightHand/Pinch o Activate

    void Awake()
    {
        if (animator == null) animator = GetComponent<Animator>();
    }

    void Update()
    {
        if (animator == null) return;

        // Lee la presión del gatillo (0.0 a 1.0) y anima el dedo índice
        if (triggerAction != null && triggerAction.action != null)
        {
            float triggerVal = triggerAction.action.ReadValue<float>();
            animator.SetFloat("Trigger", triggerVal);
        }
    }
}
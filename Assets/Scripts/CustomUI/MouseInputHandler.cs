using UnityEngine;
using UnityEngine.InputSystem;

public class MouseInputHandler : MonoBehaviour
{
    [SerializeField] private UIVars uiVars;
    private UIControls controls;

    private void Awake()
    {
        controls = new UIControls();
    }

    private void OnEnable()
    {
        controls.UI.Click.performed += OnPointerDown;
        controls.UI.Click.canceled += OnPointerUp;
        controls.Enable();
    }

    private void OnDisable()
    {
        controls.UI.Click.performed -= OnPointerDown;
        controls.UI.Click.canceled -= OnPointerUp;
        controls.Disable();
    }

    private void OnPointerDown(InputAction.CallbackContext context)
    {
        uiVars.IsPointerDown = true;
        //Debug.Log("POINTER DOWN: " + Time.frameCount);
    }

    private void OnPointerUp(InputAction.CallbackContext context)
    {
        uiVars.IsPointerDown = false;
        //Debug.Log("POINTER UP: " + Time.frameCount);
    }
}

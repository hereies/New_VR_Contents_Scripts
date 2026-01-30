using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class InteractorDebug : MonoBehaviour
{
    public XRBaseInteractor interactor;

    void Reset()
    {
        interactor = GetComponent<XRBaseInteractor>();
    }

    void OnEnable()
    {
        if (interactor == null) interactor = GetComponent<XRBaseInteractor>();
        if (interactor == null) return;

        interactor.hoverEntered.AddListener(OnHoverEntered);
        interactor.hoverExited.AddListener(OnHoverExited);
        interactor.selectEntered.AddListener(OnSelectEntered);
        interactor.selectExited.AddListener(OnSelectExited);
    }

    void OnDisable()
    {
        if (interactor == null) return;

        interactor.hoverEntered.RemoveListener(OnHoverEntered);
        interactor.hoverExited.RemoveListener(OnHoverExited);
        interactor.selectEntered.RemoveListener(OnSelectEntered);
        interactor.selectExited.RemoveListener(OnSelectExited);
    }

    void OnHoverEntered(HoverEnterEventArgs a) => Debug.Log($"[Interactor] Hover ENTER -> {a.interactableObject.transform.name}", this);
    void OnHoverExited(HoverExitEventArgs a) => Debug.Log($"[Interactor] Hover EXIT  -> {a.interactableObject.transform.name}", this);
    void OnSelectEntered(SelectEnterEventArgs a) => Debug.Log($"[Interactor] Select ENTER -> {a.interactableObject.transform.name}", this);
    void OnSelectExited(SelectExitEventArgs a) => Debug.Log($"[Interactor] Select EXIT  -> {a.interactableObject.transform.name}", this);
}

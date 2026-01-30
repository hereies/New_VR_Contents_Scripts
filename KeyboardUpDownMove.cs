using UnityEngine;

public class KeyboardUpDownMove : MonoBehaviour
{
    public CharacterController characterController;
    public Transform moveTransform;

    public KeyCode upKey = KeyCode.X;
    public KeyCode downKey = KeyCode.Y;

    public float speed = 2.0f;

    void Reset()
    {
        characterController = GetComponent<CharacterController>();
        moveTransform = transform;
    }

    void Update()
    {
        float v = 0f;
        if (Input.GetKey(upKey)) v += 1f;
        if (Input.GetKey(downKey)) v -= 1f;
        if (Mathf.Abs(v) < 0.01f) return;

        Vector3 delta = Vector3.up * (v * speed * Time.deltaTime);

        if (characterController != null && characterController.enabled)
            characterController.Move(delta);
        else
            moveTransform.position += delta;
    }
}

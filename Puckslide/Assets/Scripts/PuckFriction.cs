using UnityEngine;

public class PuckFriction : MonoBehaviour
{
    [SerializeField] private float m_Friction = 0.98f;
    private Rigidbody2D m_Rigidbody;

    private void Awake()
    {
        m_Rigidbody = GetComponent<Rigidbody2D>();
    }

    private void FixedUpdate()
    {
        m_Rigidbody.velocity *= m_Friction;
        
        if (m_Rigidbody.velocity.magnitude < 0.01f)
        {
            m_Rigidbody.velocity = Vector2.zero;
        }
    }

    public float Friction => m_Friction;
}

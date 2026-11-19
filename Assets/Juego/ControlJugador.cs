using UnityEngine;

namespace UDIT.IA3.Guardia
{
    [RequireComponent(typeof(CharacterController))]
    public class ControlJugador : MonoBehaviour
    {
        [SerializeField, Min(0f)] private float velocidad = 4f;

        private CharacterController _controlador;
        public bool Agachado = false;

        private void Awake()
        {
            _controlador = GetComponent<CharacterController>();
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.C))
                Agachado = !Agachado;

            float horizontal = Input.GetAxisRaw("Horizontal");
            float vertical = Input.GetAxisRaw("Vertical");
            Vector3 direccion = Vector3.ClampMagnitude(new Vector3(horizontal, 0f, vertical), 1f);

            _controlador.SimpleMove(direccion * velocidad);
        }
    }
}

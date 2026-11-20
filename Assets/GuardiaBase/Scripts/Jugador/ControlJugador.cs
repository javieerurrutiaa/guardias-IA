using UnityEngine;

namespace UDIT.IA3.Guardia
{
    [RequireComponent(typeof(CharacterController))]
    public class ControlJugador : MonoBehaviour
    {
        [SerializeField, Min(0f)] private float velocidad;
        [SerializeField, Min(0f)] private float velocidadAndar = 4f;
        [SerializeField, Min(0f)] private float velocidadCorrer = 10f;


        private CharacterController _controlador;
        public bool Agachado = false;
        private bool Corriendo;

        private void Awake()
        {
            _controlador = GetComponent<CharacterController>();
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.C))
                Agachado = !Agachado;
            
            if (Input.GetKeyDown(KeyCode.LeftShift))
                Corriendo = !Corriendo;
            
            if (Corriendo)
            {
                velocidad = velocidadCorrer;
                HacerRuido(0.8f);
            }
            else
            {
                velocidad = velocidadAndar;
                HacerRuido(0.3f);
            }

            float horizontal = Input.GetAxisRaw("Horizontal");
            float vertical = Input.GetAxisRaw("Vertical");
            Vector3 direccion = Vector3.ClampMagnitude(new Vector3(horizontal, 0f, vertical), 1f);

            _controlador.SimpleMove(direccion * velocidad);
        }
        public void HacerRuido(float intensidad)
        {
            SensorSonido[] sensores = FindObjectsByType<SensorSonido>(
                FindObjectsSortMode.None);
            foreach (SensorSonido sensor in sensores)
            {
                sensor.RegistrarRuido(transform.position, intensidad);
            }
        }
    }
}

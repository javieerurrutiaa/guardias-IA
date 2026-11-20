using UnityEngine;
using UnityEngine.Rendering;

namespace UDIT.IA3.Guardia
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(SensorVision))]
    public sealed class VistaGuardia : MonoBehaviour
    {
        private const int Segmentos = 48;
        private static readonly int Tinte = Shader.PropertyToID("_Color");

        [SerializeField] private Shader shader;
        [SerializeField] private Color tranquilo = new Color(0.03f, 0.59f, 0.51f);
        [SerializeField] private Color sospecha = new Color(0.88f, 0.49f, 0.07f);
        [SerializeField] private Color alerta = new Color(0.88f, 0.16f, 0.26f);

        [Tooltip("Altura Y del área. El suelo de esta escena está en Y = 0.")]
        [SerializeField] private float alturaPlano = 0.04f;

        private readonly Vector3[] _vertices = new Vector3[Segmentos + 2];
        private readonly Vector3[] _perimetro = new Vector3[Segmentos + 2];

        private SensorVision _sensor;
        private GameObject _visual;
        private Mesh _malla;
        private Material _material;
        private MaterialPropertyBlock _propiedades;
        private MeshRenderer _area;
        private LineRenderer _contorno;
        private LineRenderer _fondoMedidor;
        private LineRenderer _rellenoMedidor;

        private void Awake()
        {
            if (shader == null)
            {
                Debug.LogWarning("Asigna el shader VistaGuardia en el Inspector.", this);
                enabled = false;
                return;
            }

            _sensor = GetComponent<SensorVision>();
            _material = new Material(shader);
            _propiedades = new MaterialPropertyBlock();

            _visual = new GameObject("Visión del guardia");
            _visual.transform.SetParent(transform, false);
            _malla = CrearMalla();
            _visual.AddComponent<MeshFilter>().sharedMesh = _malla;
            _area = _visual.AddComponent<MeshRenderer>();
            ConfigurarRenderer(_area, 0);

            _contorno = CrearLinea("Contorno", 0.12f, 1);
            _contorno.positionCount = _perimetro.Length;
            _contorno.loop = true;

            _fondoMedidor = CrearLinea("Fondo del medidor", 0.24f, 2);
            _fondoMedidor.positionCount = 2;
            PonerColor(_fondoMedidor, new Color(0.10f, 0.15f, 0.21f, 0.9f));

            _rellenoMedidor = CrearLinea("Sospecha", 0.14f, 3);
            _rellenoMedidor.positionCount = 2;
        }

        private void OnEnable()
        {
            if (_visual != null) _visual.SetActive(true);
        }

        private void OnDisable()
        {
            if (_visual != null) _visual.SetActive(false);
        }

        private void OnDestroy()
        {
            Destroy(_visual);
            Destroy(_malla);
            Destroy(_material);
        }

        private void LateUpdate()
        {
            float valor = Mathf.Clamp01(_sensor.SospechaActual);
            Color color = ColorDelNivel(_sensor.NivelActual);
            DibujarCampo(color, valor);
            DibujarMedidor(color, valor);
        }

        private void DibujarCampo(Color color, float valor)
        {
            Transform ojos = _sensor.Ojos != null ? _sensor.Ojos : transform;
            Vector3 origen = ojos.position;
            Vector3 frente = Vector3.ProjectOnPlane(ojos.forward, Vector3.up).normalized;
            float alcance = _sensor.Alcance;
            float angulo = _sensor.AnguloVision;
            bool mostrarCampo = alcance > 0f && angulo > 0f && frente != Vector3.zero;

            _area.enabled = mostrarCampo;
            _contorno.enabled = mostrarCampo;
            if (!mostrarCampo) return;

            var centro = new Vector3(origen.x, alturaPlano, origen.z);
            Matrix4x4 mundoALocal = _visual.transform.worldToLocalMatrix;
            _vertices[0] = mundoALocal.MultiplyPoint3x4(centro);
            _perimetro[0] = centro;

            for (int i = 0; i <= Segmentos; i++)
            {
                float giro = angulo * ((float)i / Segmentos - 0.5f);
                Vector3 rumbo = Quaternion.AngleAxis(giro, Vector3.up) * frente;
                float distancia = alcance;

                // Se proyecta al suelo lo que se ve desde la altura de los ojos.
                if (Physics.Raycast(origen, rumbo, out RaycastHit muro, alcance,
                        _sensor.CapasQueOcluyen, QueryTriggerInteraction.Ignore))
                {
                    distancia = muro.distance;
                }

                Vector3 extremo = centro + rumbo * distancia;
                _vertices[i + 1] = mundoALocal.MultiplyPoint3x4(extremo);
                _perimetro[i + 1] = extremo;
            }

            _malla.vertices = _vertices;
            _malla.RecalculateBounds();
            _contorno.SetPositions(_perimetro);

            color.a = 0.84f;
            PonerColor(_contorno, color);

            color.a = Mathf.Lerp(0.23f, 0.36f, valor);
            _propiedades.SetColor(Tinte, color);
            _area.SetPropertyBlock(_propiedades);
        }

        private void DibujarMedidor(Color color, float valor)
        {
            Vector3 centro = transform.position + Vector3.forward * 1.05f + Vector3.up * 2.25f;
            Vector3 inicio = centro + Vector3.left * 0.88f;
            Vector3 fin = centro + Vector3.right * 0.88f;
            _fondoMedidor.SetPosition(0, inicio);
            _fondoMedidor.SetPosition(1, fin);

            _rellenoMedidor.enabled = valor > 0.001f;
            _rellenoMedidor.SetPosition(0, inicio);
            _rellenoMedidor.SetPosition(1, Vector3.Lerp(inicio, fin, valor));
            PonerColor(_rellenoMedidor, color);
        }

        private Mesh CrearMalla()
        {
            var indices = new int[Segmentos * 3];
            for (int i = 0; i < Segmentos; i++)
            {
                indices[i * 3] = 0;
                indices[i * 3 + 1] = i + 1;
                indices[i * 3 + 2] = i + 2;
            }

            var colores = new Color32[_vertices.Length];
            colores[0] = new Color32(255, 255, 255, 255);
            for (int i = 1; i < colores.Length; i++)
                colores[i] = new Color32(255, 255, 255, 187);

            var malla = new Mesh { name = "Campo de visión" };
            malla.MarkDynamic();
            malla.vertices = _vertices;
            malla.colors32 = colores;
            malla.triangles = indices;
            return malla;
        }

        private LineRenderer CrearLinea(string nombre, float grosor, int orden)
        {
            var linea = new GameObject(nombre).AddComponent<LineRenderer>();
            linea.transform.SetParent(_visual.transform, false);
            linea.useWorldSpace = true;
            linea.startWidth = grosor;
            linea.endWidth = grosor;
            linea.numCapVertices = 2;
            linea.numCornerVertices = 2;
            ConfigurarRenderer(linea, orden);
            return linea;
        }

        private void ConfigurarRenderer(Renderer renderer, int orden)
        {
            renderer.sharedMaterial = _material;
            renderer.sortingOrder = orden;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
        }

        private static void PonerColor(LineRenderer linea, Color color)
        {
            linea.startColor = color;
            linea.endColor = color;
        }

        private Color ColorDelNivel(NivelSospecha nivel)
        {
            if (nivel == NivelSospecha.Alerta) return alerta;
            if (nivel == NivelSospecha.Sospecha) return sospecha;
            return tranquilo;
        }
    }
}

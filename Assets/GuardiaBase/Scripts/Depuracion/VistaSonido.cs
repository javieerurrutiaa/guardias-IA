
using UnityEngine;
using UnityEngine.Rendering;

namespace UDIT.IA3.Guardia
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(SensorSonido))]
    public class VistaSonido : MonoBehaviour
    {
        private const int Segmentos = 48;

        private static readonly int Tinte =
            Shader.PropertyToID("_Color");

        [Header("Colores")]
        [SerializeField] private Color colorSonido =
            new Color(1f, 0.55f, 0.05f, 0.3f);

        [SerializeField] private Color colorOrigen =
            new Color(1f, 0.25f, 0.05f, 1f);

        [Header("Visualización")]
        [SerializeField] private Shader shader;

        [SerializeField] private float alturaPlano = 0.06f;

        [SerializeField] private float alturaOrigen = 0.2f;

        private SensorSonido _sensor;

        private GameObject _visual;
        private Mesh _malla;
        private Material _material;

        private MeshRenderer _area;
        private LineRenderer _contorno;
        private LineRenderer _lineaOrigen;
        private LineRenderer _marcadorOrigen;

        private readonly Vector3[] _vertices =
            new Vector3[Segmentos + 2];

        private readonly Vector3[] _perimetro =
            new Vector3[Segmentos + 2];

        private MaterialPropertyBlock _propiedades;

        private void Awake()
        {
            _sensor = GetComponent<SensorSonido>();

            if (shader == null)
            {
                Debug.LogWarning(
                    "Asigna el shader de VistaGuardia.",
                    this
                );

                enabled = false;
                return;
            }

            _material = new Material(shader);
            _propiedades = new MaterialPropertyBlock();

            _visual = new GameObject("Visualización del sonido");

            _malla = CrearMalla();

            GameObject superficie = new GameObject("Area de sonido");
            superficie.transform.SetParent(_visual.transform, false);

            superficie.AddComponent<MeshFilter>().sharedMesh = _malla;

            _area = superficie.AddComponent<MeshRenderer>();
            ConfigurarRenderer(_area, 0);

            _contorno = CrearLinea("Contorno sonido", 0.10f);
            _contorno.positionCount = Segmentos + 1;
            _contorno.loop = true;

            _lineaOrigen = CrearLinea("Direccion del ruido", 0.07f);
            _lineaOrigen.positionCount = 2;

            _marcadorOrigen = CrearLinea("Marcador origen", 0.10f);
            _marcadorOrigen.positionCount = 5;
        }

        private void LateUpdate()
        {
            bool activo = _sensor.enabled &&
                GetComponent<GuardiaAgente>()
                    .Pizarra.Get(ClavesBB.HayRuido, false);

            _area.enabled = activo;
            _contorno.enabled = activo;
            _lineaOrigen.enabled = activo;
            _marcadorOrigen.enabled = activo;

            if (!activo)
                return;

            BlackboardActualizar();
        }

        private void BlackboardActualizar()
        {
            GuardiaAgente agente = GetComponent<GuardiaAgente>();

            Vector3 origen = agente.Pizarra.Get(
                ClavesBB.PosicionRuido,
                Vector3.zero
            );

            float intensidad = agente.Pizarra.Get(
                ClavesBB.IntensidadRuido,
                0f
            );

            float tiempo = agente.Pizarra.Get(
                ClavesBB.TiempoRuido,
                0f
            );

            float radio = Mathf.Lerp(
                0.5f,
                _sensor.AlcanceAudicion,
                intensidad
            );

            Vector3 centro = new Vector3(
                origen.x,
                alturaPlano,
                origen.z
            );

            DibujarArea(centro, radio, intensidad, tiempo);
            DibujarOrigen(origen);
        }

        private void DibujarArea(
            Vector3 centro,
            float radio,
            float intensidad,
            float tiempo)
        {
            Matrix4x4 mundoALocal =
                _visual.transform.worldToLocalMatrix;

            _vertices[0] =
                mundoALocal.MultiplyPoint3x4(centro);

            _perimetro[0] = centro;

            for (int i = 0; i <= Segmentos; i++)
            {
                float angulo = i * Mathf.PI * 2f / Segmentos;

                Vector3 punto = centro + new Vector3(
                    Mathf.Cos(angulo) * radio,
                    0f,
                    Mathf.Sin(angulo) * radio
                );

                _vertices[i + 1] =
                    mundoALocal.MultiplyPoint3x4(punto);

                _perimetro[i] = punto;
            }

            _malla.vertices = _vertices;
            _malla.RecalculateBounds();

            _contorno.SetPositions(_perimetro);

            Color color = colorSonido;
            color.a = Mathf.Lerp(0.08f, 0.35f, intensidad);

            _propiedades.SetColor(Tinte, color);
            _area.SetPropertyBlock(_propiedades);

            _contorno.startColor = colorSonido;
            _contorno.endColor = colorSonido;
        }

        private void DibujarOrigen(Vector3 origen)
        {
            Vector3 centro = origen + Vector3.up * alturaOrigen;

            _lineaOrigen.SetPosition(
                0,
                transform.position + Vector3.up
            );

            _lineaOrigen.SetPosition(1, centro);

            float radio = 0.35f;

            for (int i = 0; i < 5; i++)
            {
                float angulo = i * Mathf.PI * 2f / 4f;

                _marcadorOrigen.SetPosition(
                    i,
                    centro + new Vector3(
                        Mathf.Cos(angulo) * radio,
                        0f,
                        Mathf.Sin(angulo) * radio
                    )
                );
            }

            _lineaOrigen.startColor = colorOrigen;
            _lineaOrigen.endColor = colorOrigen;

            _marcadorOrigen.startColor = colorOrigen;
            _marcadorOrigen.endColor = colorOrigen;
        }

        private Mesh CrearMalla()
        {
            int[] triangulos = new int[Segmentos * 3];

            for (int i = 0; i < Segmentos; i++)
            {
                triangulos[i * 3] = 0;
                triangulos[i * 3 + 1] = i + 1;
                triangulos[i * 3 + 2] = i + 2;
            }

            Mesh malla = new Mesh();
            malla.name = "Area de propagacion sonora";
            malla.MarkDynamic();
            malla.vertices = _vertices;
            malla.triangles = triangulos;

            return malla;
        }

        private LineRenderer CrearLinea(string nombre, float grosor)
        {
            GameObject objeto = new GameObject(nombre);
            objeto.transform.SetParent(_visual.transform, false);

            LineRenderer linea = objeto.AddComponent<LineRenderer>();

            linea.useWorldSpace = true;
            linea.startWidth = grosor;
            linea.endWidth = grosor;
            linea.loop = false;
            linea.numCapVertices = 4;

            ConfigurarRenderer(linea, 1);

            return linea;
        }

        private void ConfigurarRenderer(Renderer renderer, int orden)
        {
            renderer.sharedMaterial = _material;
            renderer.sortingOrder = orden;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
        }

        private void OnDestroy()
        {
            if (_visual != null)
                Destroy(_visual);

            if (_malla != null)
                Destroy(_malla);

            if (_material != null)
                Destroy(_material);
        }
    }
}

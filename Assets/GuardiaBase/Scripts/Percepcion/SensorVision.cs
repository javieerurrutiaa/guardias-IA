// ═══════════════════════════════════════════════════════════════════════════
//  SensorVision.cs — Percepción visual y memoria de contacto
//
//  Inteligencia Artificial (IA3) · GDDVV 3º · UDIT · Curso 2026/2027
//  Dr. Luis Peña Sánchez
//
// ═══════════════════════════════════════════════════════════════════════════

using UnityEngine;
using UDIT.IA3.BT;

namespace UDIT.IA3.Guardia
{
    public class SensorVision : MonoBehaviour, ISensor
    {
        // El pivote de la cápsula del jugador está en el centro.
        private const float AlturaCabeza = 0.8f;
        private const float AlturaPies = -0.9f;

        [Header("Objetivo")]
        [Tooltip("El jugador. Si lo dejáis vacío se busca por tag al arrancar.")]
        public Transform Objetivo;
        public string TagObjetivo = "Player";

        [Header("Cono de visión")]
        [Min(0f)] public float Alcance = 15f;
        [Range(0f, 180f)] public float AnguloVision = 60f;

        [Tooltip("Desde dónde se mira. Si está vacío se usa el propio transform. " +
                 "Poned aquí la cabeza: mirar desde los pies da resultados raros.")]
        public Transform Ojos;

        [Tooltip("Qué capas TAPAN la visión. Marcad aquí las paredes. " +
                 "Si dejáis esto en Nothing, no habrá oclusión y se notará.")]
        public LayerMask CapasQueOcluyen;

        [Header("Medidor de sospecha — APARTADO A DEL ENUNCIADO")]
        [Tooltip("Ritmo base de subida. Se multiplica por la evidencia y la curva de distancia.")]
        [Min(0f)] public float VelocidadSubida = 0.8f;

        [Tooltip("Multiplica la subida. Distancia 0 = junto al guardia, 1 = al límite del alcance.")]
        [SerializeField] private AnimationCurve curvaSubidaPorDistancia = new AnimationCurve(
            new Keyframe(0f, 9f),
            new Keyframe(0.13f, 6.5f),
            new Keyframe(0.40f, 1.5f),
            new Keyframe(0.70f, 0.8f),
            new Keyframe(1f, 0.65f));

        [Tooltip("Cuánto baja por segundo cuando el guardia pierde de vista al jugador.")]
        [Min(0f)] public float VelocidadBajada = 0.08f;

        [Tooltip("Umbral tranquilo → sospecha (al subir).")]
        [Range(0f, 1f)] public float UmbralSospecha = 0.35f;

        [Tooltip("Umbral sospecha → alerta (al subir).")]
        [Range(0f, 1f)] public float UmbralAlerta = 0.8f;

        [Tooltip("Cuánto hay que bajar por debajo del umbral para volver atrás. " +
                 "Sin esto el guardia parpadea entre estados. Es la HISTÉRESIS.")]
        [Range(0f, 0.5f)] public float Histeresis = 0.1f;

        // Estado interno del sensor. Lo publica en la pizarra al final de Percibir.
        private float _sospecha;
        private NivelSospecha _nivel = NivelSospecha.Tranquilo;
        private bool _visiblePrevio;
        private Vector3 _ultimaPosVista;

        /// <summary>Resultado de la última comprobación de visión.</summary>
        public bool VisibleAhora { get; private set; }
        /// <summary>Punto alcanzado por la visión cuando VisibleAhora es true.</summary>
        public Vector3 PuntoVisible { get; private set; }
        public float SospechaActual { get { return _sospecha; } }
        public NivelSospecha NivelActual { get { return _nivel; } }

        private void Awake()
        {
            if (Ojos == null) Ojos = transform;

            if (Objetivo == null && !string.IsNullOrEmpty(TagObjetivo))
            {
                GameObject go = GameObject.FindGameObjectWithTag(TagObjetivo);
                if (go != null) Objetivo = go.transform;
            }
        }

        // ───────────────────────────────────────────────────────────────────
        //  EL CICLO DE PERCEPCIÓN
        // ───────────────────────────────────────────────────────────────────

        public void Percibir(Blackboard bb)
        {
            bool visible = ComprobarVision(out float subidaPorSegundo);
            VisibleAhora = visible;

            ActualizarSospecha(visible ? subidaPorSegundo : -VelocidadBajada, Time.deltaTime);

            // ── Publicar en la pizarra ──────────────────────────────────────
            bb.Set(ClavesBB.Visible, visible);
            bb.Set(ClavesBB.Sospecha, _sospecha);
            bb.Set(ClavesBB.Nivel, _nivel);

            // Al perderlo, se publica la última posición que sí se pudo observar.
            if (visible)
            {
                _ultimaPosVista = Objetivo.position;
                bb.Set(ClavesBB.PosicionVisible, _ultimaPosVista);
                if (!_visiblePrevio) bb.Set(ClavesBB.TieneUltimaPos, false);
            }
            else if (_visiblePrevio)
            {
                bb.Set(ClavesBB.UltimaPos, _ultimaPosVista);
                bb.Set(ClavesBB.TieneUltimaPos, true);
                bb.Set(ClavesBB.VecesPerdido, bb.Get(ClavesBB.VecesPerdido, 0) + 1);
            }

            _visiblePrevio = visible;
        }

        // ───────────────────────────────────────────────────────────────────
        //  LAS TRES ETAPAS
        // ───────────────────────────────────────────────────────────────────

        private bool ComprobarVision(out float subidaPorSegundo)
        {
            subidaPorSegundo = 0f;
            float semiangulo = AnguloVision * 0.5f;
            if (Objetivo == null || !Objetivo.gameObject.activeInHierarchy || Alcance <= 0f || semiangulo <= 0f)
                return false;

            Transform ojos = Ojos != null ? Ojos : transform;
            Vector3 origen = ojos.position;
            Vector3 torso = Objetivo.position;
            Vector3 haciaObjetivo = torso - origen;

            // 1. Alcance. Primero se descartan los objetivos lejanos.
            float distanciaCuadrada = haciaObjetivo.sqrMagnitude;
            if (distanciaCuadrada > Alcance * Alcance) return false;

            // 2. Ángulo. El cono se mide en horizontal para esta escena plana.
            Vector3 frente = Vector3.ProjectOnPlane(ojos.forward, Vector3.up);
            Vector3 rumbo = Vector3.ProjectOnPlane(haciaObjetivo, Vector3.up);
            float angulo = Vector3.Angle(frente, rumbo);
            if (angulo > semiangulo) return false;

            // 3. Oclusión. Basta con que uno de los tres puntos esté a la vista.
            Vector3 cabeza = torso + Vector3.up * AlturaCabeza;
            Vector3 pies = torso + Vector3.up * AlturaPies;
            bool cabezaVisible = TieneLineaDeVision(origen, cabeza);
            bool torsoVisible = TieneLineaDeVision(origen, torso);
            bool piesVisibles = TieneLineaDeVision(origen, pies);
            if (!cabezaVisible && !torsoVisible && !piesVisibles) return false;

            PuntoVisible = torso;
            if (!torsoVisible) PuntoVisible = cabezaVisible ? cabeza : pies;

            float distanciaNormalizada = Mathf.Clamp01(Mathf.Sqrt(distanciaCuadrada) / Alcance);
            float factorDistancia = 1f - distanciaNormalizada;
            float factorAngulo = Mathf.Clamp01(1f - angulo / semiangulo);
            // Media provisional hasta que se elija el tercer factor.
            float evidencia = (factorDistancia + factorAngulo) * 0.5f;
            float multiplicadorDistancia = curvaSubidaPorDistancia != null && curvaSubidaPorDistancia.length > 0
                ? Mathf.Max(0f, curvaSubidaPorDistancia.Evaluate(distanciaNormalizada))
                : 1f;
            subidaPorSegundo = VelocidadSubida * evidencia * multiplicadorDistancia;
            return true;
        }

        private bool TieneLineaDeVision(Vector3 origen, Vector3 punto)
        {
            Vector3 haciaPunto = punto - origen;
            float distancia = haciaPunto.magnitude;
            return distancia == 0f ||
                   !Physics.Raycast(origen, haciaPunto / distancia, distancia, CapasQueOcluyen, QueryTriggerInteraction.Ignore);
        }

        // ───────────────────────────────────────────────────────────────────
        //  EL MEDIDOR DE SOSPECHA
        // ───────────────────────────────────────────────────────────────────

        private void ActualizarSospecha(float cambioPorSegundo, float dt)
        {
            _sospecha = Mathf.Clamp01(_sospecha + cambioPorSegundo * dt);
            _nivel = ActualizarNivel(_sospecha, _nivel);
        }

        /// <summary>
        /// Para bajar de nivel, la sospecha debe cruzar el umbral de subida
        /// menos el margen de histéresis.
        /// </summary>
        private NivelSospecha ActualizarNivel(float s, NivelSospecha actual)
        {
            if (s <= 0f) return NivelSospecha.Tranquilo;

            switch (actual)
            {
                case NivelSospecha.Tranquilo:
                    if (s >= UmbralSospecha)
                        return s >= UmbralAlerta ? NivelSospecha.Alerta : NivelSospecha.Sospecha;
                    break;

                case NivelSospecha.Sospecha:
                    if (s >= UmbralAlerta) return NivelSospecha.Alerta;
                    if (s < UmbralSospecha - Histeresis) return NivelSospecha.Tranquilo;
                    break;

                case NivelSospecha.Alerta:
                    if (s < UmbralAlerta - Histeresis) return NivelSospecha.Sospecha;
                    break;
            }
            return actual;
        }
    }
}

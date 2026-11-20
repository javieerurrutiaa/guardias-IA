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
        // Referencia al controlador del jugador para comprobar postura
        private ControlJugador controlJugador;

        private GestorSospecha gestorSospecha;
        
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
            new Keyframe(0f, 2f),
            new Keyframe(0.13f, 1.8f),
            new Keyframe(0.40f, 1.3f),
            new Keyframe(0.70f, 0.8f),
            new Keyframe(1f, 0.5f));

  
       
        private bool _visiblePrevio;
        private Vector3 _ultimaPosVista;

        /// <summary>Resultado de la última comprobación de visión.</summary>
        public bool VisibleAhora { get; private set; }
        /// <summary>Punto alcanzado por la visión cuando VisibleAhora es true.</summary>
        public Vector3 PuntoVisible { get; private set; }
        public float SospechaActual
        {
            get
            {
                if (gestorSospecha != null)
                {
                    return gestorSospecha.SospechaActual;
                }

                return 0f;
            }
        }

        public NivelSospecha NivelActual
        {
            get
            {
                if (gestorSospecha != null)
                {
                    return gestorSospecha.NivelActual;
                }

                return NivelSospecha.Tranquilo;
            }
        }
    

        private void Awake()
        {
            if (Ojos == null) Ojos = transform;
            gestorSospecha = GetComponent<GestorSospecha>();

            if (Objetivo == null && !string.IsNullOrEmpty(TagObjetivo))
            {
                GameObject go = GameObject.FindGameObjectWithTag(TagObjetivo);
                if (go != null) Objetivo = go.transform;
            }
            if (Objetivo != null)
                controlJugador = Objetivo.GetComponent<ControlJugador>();
        }

        // ───────────────────────────────────────────────────────────────────
        //  EL CICLO DE PERCEPCIÓN
        // ───────────────────────────────────────────────────────────────────

       
            public void Percibir(Blackboard bb)
            {
                bool visible = ComprobarVision(out float subidaPorSegundo);

                VisibleAhora = visible;

                // Publicar evidencia visual.
                bb.Set(
                    ClavesBB.EvidenciaVisual,
                    visible ? subidaPorSegundo : 0f
                );

                bb.Set(ClavesBB.Visible, visible);

                // Guardar la última posición visible.
                if (visible)
                {
                    _ultimaPosVista = Objetivo.position;

                    bb.Set(ClavesBB.PosicionVisible, _ultimaPosVista);

                    if (!_visiblePrevio)
                        bb.Set(ClavesBB.TieneUltimaPos, false);
                }
                else if (_visiblePrevio)
                {
                    bb.Set(ClavesBB.UltimaPos, _ultimaPosVista);
                    bb.Set(ClavesBB.TieneUltimaPos, true);

                    bb.Set(
                        ClavesBB.VecesPerdido,
                        bb.Get(ClavesBB.VecesPerdido, 0) + 1
                    );
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

            // Factor distancia
            float distanciaNormalizada = Mathf.Clamp01(Mathf.Sqrt(distanciaCuadrada) / Alcance);
            float factorDistancia = 1f - distanciaNormalizada;
            
            // Factor angulo
            float factorAngulo = Mathf.Clamp01(1f - angulo / semiangulo);
            
            // Factor postura
            float factorPostura = 1f;
            if (controlJugador != null)
            {
                if (controlJugador.Agachado)
                    factorPostura = 0.4f;
                else
                    factorPostura = 1f;
            }            
            // Combinacion de 3 factores
            float evidencia = (factorDistancia + factorAngulo + factorPostura) / 3f;
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
    }
}

// ═══════════════════════════════════════════════════════════════════════════
//  GuardiaGizmos.cs — Hacer visible lo que el guardia piensa
//
//  Inteligencia Artificial (IA3) · GDDVV 3º · UDIT · Curso 2026/2027
//  Dr. Luis Peña Sánchez
//
//  MATERIAL DE PARTIDA — terminado, pero AMPLIADLO.
//
//  El requisito A.6 del enunciado dice que el estado interno tiene que ser
//  legible desde fuera. No es un capricho de corrección: es que sin esto no
//  podéis depurar. Un medidor de sospecha que no se ve es un número del que
//  no sabéis si sube demasiado rápido.
//
//  Y es también diseño de juego: si el jugador no percibe que el guardia
//  empieza a sospechar, para él la detección es instantánea e injusta.
// ═══════════════════════════════════════════════════════════════════════════

using UnityEngine;
using UDIT.IA3.BT;

namespace UDIT.IA3.Guardia
{
    [RequireComponent(typeof(GuardiaAgente))]
    public class GuardiaGizmos : MonoBehaviour
    {
        public bool DibujarCono = true;
        public bool DibujarSospecha = true;
        public bool DibujarUltimaPos = true;

        public bool DibujarAudicion = true;
        public bool DibujarOrigenRuido = true;

        public Color ColorAudicion = Color.yellow;
        public Color ColorOrigenRuido = new Color(1f, 0.5f, 0f);

        private GuardiaAgente _agente;
        private SensorVision _vision;
        private SensorSonido _sonido;

        private void Awake()
        {
            _agente = GetComponent<GuardiaAgente>();
            _vision = GetComponent<SensorVision>();
            _sonido = GetComponent<SensorSonido>();
        }

        private static Color ColorDe(NivelSospecha n)
        {
            switch (n)
            {
                case NivelSospecha.Alerta:   return new Color(1f, 0.2f, 0.2f);
                case NivelSospecha.Sospecha: return new Color(1f, 0.85f, 0.2f);
                default:                     return new Color(0.4f, 0.9f, 0.4f);
            }
        }

        private void OnDrawGizmos()
        {
            if (_vision == null) _vision = GetComponent<SensorVision>();
            if (_vision == null) return;
            if (_sonido == null)
                _sonido = GetComponent<SensorSonido>();

            Transform ojos = _vision.Ojos != null ? _vision.Ojos : transform;
            NivelSospecha nivel = _vision.NivelActual;
            Color c = ColorDe(nivel);

            // ── El cono de visión ───────────────────────────────────────────
            if (DibujarCono)
            {
                Gizmos.color = new Color(c.r, c.g, c.b, 0.85f);

                float medio = _vision.AnguloVision * 0.5f;
                Vector3 frente = Vector3.ProjectOnPlane(ojos.forward, Vector3.up).normalized;
                Vector3 izq = Quaternion.Euler(0f, -medio, 0f) * frente;
                Vector3 der = Quaternion.Euler(0f,  medio, 0f) * frente;

                Gizmos.DrawLine(ojos.position, ojos.position + izq * _vision.Alcance);
                Gizmos.DrawLine(ojos.position, ojos.position + der * _vision.Alcance);

                // El arco, a trozos
                const int pasos = 16;
                Vector3 previo = ojos.position + izq * _vision.Alcance;
                for (int i = 1; i <= pasos; i++)
                {
                    float t = -medio + (_vision.AnguloVision * i / pasos);
                    Vector3 p = ojos.position +
                                (Quaternion.Euler(0f, t, 0f) * frente) * _vision.Alcance;
                    Gizmos.DrawLine(previo, p);
                    previo = p;
                }

                // Dibuja una de las líneas de visión despejadas.
                if (Application.isPlaying && _vision.VisibleAhora)
                {
                    Gizmos.color = c;
                    Gizmos.DrawLine(ojos.position, _vision.PuntoVisible);
                }
            }

            // ── La barra de sospecha, sobre la cabeza ───────────────────────
            if (DibujarSospecha && Application.isPlaying)
            {
                Vector3 baseBarra = transform.position + Vector3.up * 2.3f - Vector3.right * 0.5f;
                float s = Mathf.Clamp01(_vision.SospechaActual);

                Gizmos.color = new Color(0f, 0f, 0f, 0.5f);
                Gizmos.DrawLine(baseBarra, baseBarra + Vector3.right * 1f);

                Gizmos.color = c;
                if (s > 0.001f)
                    Gizmos.DrawLine(baseBarra, baseBarra + Vector3.right * s);
            }


            // ── Visualización auditiva ──────────────────────────

            if (_sonido != null && Application.isPlaying)
            {
                // Alcance de audición.
                if (DibujarAudicion)
                {
                    Gizmos.color = ColorAudicion;
                    Gizmos.DrawWireSphere(
                        transform.position,
                        _sonido.AlcanceAudicion
                    );
                }

                // Origen del último ruido.
                if (DibujarOrigenRuido &&
                    _agente != null &&
                    _agente.Pizarra != null &&
                    _agente.Pizarra.Get(ClavesBB.HayRuido, false))
                {
                    Vector3 origen = _agente.Pizarra.Get(
                        ClavesBB.PosicionRuido,
                        Vector3.zero
                    );

                    float intensidad = _agente.Pizarra.Get(
                        ClavesBB.IntensidadRuido,
                        0f
                    );

                    float tamano = 0.2f + intensidad * 0.5f;

                    // Esfera en el origen del ruido.
                    Gizmos.color = ColorOrigenRuido;
                    Gizmos.DrawSphere(
                        origen + Vector3.up * 0.2f,
                        tamano
                    );

                    // Círculo alrededor del origen.
                    Gizmos.DrawWireSphere(
                        origen + Vector3.up * 0.2f,
                        tamano * 1.8f
                    );

                    // Línea entre guardia y origen.
                   Gizmos.color = Color.yellow;
                   Gizmos.DrawLine(
                       transform.position + Vector3.up,
                     origen + Vector3.up * 0.2f
                 );
                }
            }
            // ── La última posición conocida ─────────────────────────────────
            if (DibujarUltimaPos && Application.isPlaying && _agente != null &&
                _agente.Pizarra != null && _agente.Pizarra.Get(ClavesBB.TieneUltimaPos, false))
            {
                Vector3 lkp = _agente.Pizarra.Get(ClavesBB.UltimaPos, Vector3.zero);

                Gizmos.color = new Color(1f, 0.5f, 0f, 0.9f);
                Gizmos.DrawWireCube(lkp + Vector3.up * 0.1f, new Vector3(0.6f, 0.2f, 0.6f));
                Gizmos.DrawLine(transform.position + Vector3.up, lkp + Vector3.up * 0.2f);
            }
        }
        
        private void OnGUI()
        {
            if (!Application.isPlaying || _agente == null)
                return;

            if (!_agente.MostrarTraza)
                return;
                
            float x = 10f;
            float y = 10f;

            string nombreGuardia = "";

            if (gameObject.name == "GuardiaEste")
            {
                nombreGuardia = "GUARDIA ESTE";
                y = 10f;
                GUI.color = Color.green;
            }
            else if (gameObject.name == "GuardiaOeste")
            {
                nombreGuardia = "GUARDIA OESTE";
                y = 40f;
                GUI.color = Color.cyan;
            }

            if (nombreGuardia != "")
            {
                GUI.Label(
                new Rect(x, y, 1200, 35),
                nombreGuardia + ": " + _agente.UltimaTraza
                );
            }

            GUI.color = Color.white;  
        }
    }
}

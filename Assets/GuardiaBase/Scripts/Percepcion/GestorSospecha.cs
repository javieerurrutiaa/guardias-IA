
using UnityEngine;
using UDIT.IA3.BT;

namespace UDIT.IA3.Guardia
{
    public class GestorSospecha : MonoBehaviour
    {
        [Header("Velocidad del medidor")]
        [Min(0f)] public float VelocidadBajada = 0.08f;

        [Header("Umbrales")]
        [Range(0f, 1f)] public float UmbralSospecha = 0.35f;
        [Range(0f, 1f)] public float UmbralAlerta = 0.8f;
        [Range(0f, 0.5f)] public float Histeresis = 0.1f;

        [Header("Depuración")]
        public float SospechaActual { get; private set; }

        public NivelSospecha NivelActual { get; private set; }
            = NivelSospecha.Tranquilo;

        public void Actualizar(Blackboard bb)
        {
            float visual = bb.Get(ClavesBB.EvidenciaVisual, 0f);
            float auditiva = bb.Get(ClavesBB.EvidenciaAuditiva, 0f);

            // Combinamos ambas fuentes de percepción.
            float evidenciaTotal = visual + auditiva;

            // Si hay estímulos, la sospecha aumenta.
            // Si no hay estímulos, disminuye lentamente.
            float cambio = evidenciaTotal > 0f
                ? evidenciaTotal
                : -VelocidadBajada;

            SospechaActual = Mathf.Clamp01(
                SospechaActual + cambio * Time.deltaTime
            );

            NivelActual = ActualizarNivel(
                SospechaActual,
                NivelActual
            );

            bb.Set(ClavesBB.Sospecha, SospechaActual);
            bb.Set(ClavesBB.Nivel, NivelActual);
        }

        private NivelSospecha ActualizarNivel(
            float s,
            NivelSospecha actual)
        {
            if (s <= 0f)
                return NivelSospecha.Tranquilo;

            switch (actual)
            {
                case NivelSospecha.Tranquilo:
                    if (s >= UmbralSospecha)
                        return s >= UmbralAlerta
                            ? NivelSospecha.Alerta
                            : NivelSospecha.Sospecha;
                    break;

                case NivelSospecha.Sospecha:
                    if (s >= UmbralAlerta)
                        return NivelSospecha.Alerta;

                    if (s < UmbralSospecha - Histeresis)
                        return NivelSospecha.Tranquilo;
                    break;

                case NivelSospecha.Alerta:
                    if (s < UmbralAlerta - Histeresis)
                        return NivelSospecha.Sospecha;
                    break;
            }

            return actual;
        }
    }
}

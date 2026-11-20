
using UnityEngine;
using UDIT.IA3.BT;

namespace UDIT.IA3.Guardia
{
    public class SensorSonido : MonoBehaviour, ISensor
    {
        [Header("Audición")]
        [Min(0f)] public float AlcanceAudicion = 12f;

        [Min(0f)] public float MultiplicadorSonido = 0.8f;

        [Header("Memoria del sonido")]
        [Min(0f)] public float DuracionRuido = 2f;

        private Vector3 posicionRuido;
        private float intensidadRuido;
        private float tiempoRestante;
        private bool hayRuido;

        public void RegistrarRuido(
            Vector3 origen,
            float intensidad)
        {
            float distancia = Vector3.Distance(
                transform.position,
                origen
            );

            if (distancia > AlcanceAudicion ||
                AlcanceAudicion <= 0f)
                return;

            float factorDistancia = 1f -
                Mathf.Clamp01(distancia / AlcanceAudicion);

            float intensidadFinal = Mathf.Clamp01(
                intensidad * factorDistancia
            );

            // Guardamos el ruido más intenso.
            if (!hayRuido || intensidadFinal > intensidadRuido)
            {
                posicionRuido = origen;
                intensidadRuido = intensidadFinal;
                tiempoRestante = DuracionRuido;
                hayRuido = true;
            }
        }

        public void Percibir(Blackboard bb)
        {
            if (hayRuido)
            {
                tiempoRestante -= Time.deltaTime;

                if (tiempoRestante <= 0f)
                {
                    hayRuido = false;
                    intensidadRuido = 0f;
                }
            }

            float evidencia = hayRuido
                ? intensidadRuido * MultiplicadorSonido
                : 0f;

            bb.Set(ClavesBB.EvidenciaAuditiva, evidencia);
            bb.Set(ClavesBB.HayRuido, hayRuido);
            bb.Set(ClavesBB.PosicionRuido, posicionRuido);
            bb.Set(ClavesBB.IntensidadRuido, intensidadRuido);
            bb.Set(ClavesBB.TiempoRuido, tiempoRestante);
        }
    }
}

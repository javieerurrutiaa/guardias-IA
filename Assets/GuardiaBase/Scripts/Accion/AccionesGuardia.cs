using UDIT.IA3.BT;
using UnityEngine;
using UnityEngine.AI;

namespace UDIT.IA3.Guardia
{
    internal sealed class AccionesGuardia
    {
        private const float DistanciaInvestigacion = 2f;
        private const float RadioProyeccion = 2f;
        private const float TiempoMaximoBusqueda = 8f;

        private static readonly Vector3[] DesplazamientosBusqueda =
        {
            Vector3.zero,
            Vector3.left * 2f,
            Vector3.right * 2f
        };

        private readonly Transform _guardia;
        private readonly RutaPatrulla _ruta;
        private int _perdidaEnCurso = -1;
        private int _siguientePunto;
        private float _limiteBusqueda;

        public AccionesGuardia(Transform guardia, RutaPatrulla ruta)
        {
            _guardia = guardia;
            _ruta = ruta;
        }

        public bool Patrullar(Blackboard bb)
        {
            if (_ruta == null || !_ruta.TieneRuta)
            {
                bb.Set(ClavesBB.TieneDestino, false);
                return false;
            }

            Vector3 destino = _ruta.PuntoActual;
            if (bb.Get(ClavesBB.TieneDestino, false) &&
                bb.Get(ClavesBB.HaLlegado, false) &&
                bb.Get<Vector3>(ClavesBB.Destino) == destino)
            {
                destino = _ruta.Avanzar();
            }

            PedirDestino(bb, destino);
            return true;
        }

        public void Investigar(Blackboard bb)
        {
            Vector3 jugador = bb.Get(ClavesBB.PosicionVisible, _guardia.position);
            Vector3 haciaJugador = jugador - _guardia.position;
            haciaJugador.y = 0f;

            // El guardia se acerca con cautela mientras sube la sospecha.
            if (haciaJugador.sqrMagnitude <= DistanciaInvestigacion * DistanciaInvestigacion)
            {
                bb.Set(ClavesBB.TieneDestino, false);
                return;
            }

            Vector3 destino = jugador - haciaJugador.normalized * DistanciaInvestigacion;
            if (!PedirSobreMalla(bb, destino)) bb.Set(ClavesBB.TieneDestino, false);
        }

        public void Perseguir(Blackboard bb)
        {
            Vector3 jugador = bb.Get(ClavesBB.PosicionVisible, _guardia.position);
            if (!PedirSobreMalla(bb, jugador)) bb.Set(ClavesBB.TieneDestino, false);
        }

        public bool Buscar(Blackboard bb)
        {
            int perdida = bb.Get(ClavesBB.VecesPerdido, 0);
            if (perdida != _perdidaEnCurso)
            {
                _perdidaEnCurso = perdida;
                _siguientePunto = 0;
                _limiteBusqueda = bb.Tiempo + TiempoMaximoBusqueda;
            }
            else if (bb.Tiempo < _limiteBusqueda && !bb.Get(ClavesBB.HaLlegado, false))
            {
                return true;
            }

            if (bb.Tiempo < _limiteBusqueda)
            {
                Vector3 ultimaPos = bb.Get<Vector3>(ClavesBB.UltimaPos);
                while (_siguientePunto < DesplazamientosBusqueda.Length)
                {
                    Vector3 punto = ultimaPos + DesplazamientosBusqueda[_siguientePunto];
                    _siguientePunto++;
                    if (PedirSobreMalla(bb, punto)) return true;
                }
            }

            bb.Set(ClavesBB.TieneUltimaPos, false);
            return false;
        }

        // La locomoción recibe un punto en el suelo aunque el pivote esté en el centro de la cápsula.
        private static bool PedirSobreMalla(Blackboard bb, Vector3 destino)
        {
            if (!NavMesh.SamplePosition(destino, out NavMeshHit punto, RadioProyeccion, NavMesh.AllAreas))
                return false;

            PedirDestino(bb, punto.position);
            return true;
        }

        private static void PedirDestino(Blackboard bb, Vector3 destino)
        {
            bb.Set(ClavesBB.Destino, destino);
            bb.Set(ClavesBB.TieneDestino, true);
        }
    }
}

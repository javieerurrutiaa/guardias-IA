using UDIT.IA3.BT;

namespace UDIT.IA3.Guardia
{
    internal static class CondicionesGuardia
    {
        public static bool PuedePerseguir(Blackboard bb)
        {
            return bb.Get(ClavesBB.Visible, false) &&
                   bb.Get(ClavesBB.Nivel, NivelSospecha.Tranquilo) == NivelSospecha.Alerta;
        }

        public static bool PuedeInvestigar(Blackboard bb)
        {
            return bb.Get(ClavesBB.Visible, false) &&
                   bb.Get(ClavesBB.Nivel, NivelSospecha.Tranquilo) == NivelSospecha.Sospecha;
        }

        public static bool PuedeBuscar(Blackboard bb)
        {
            return !bb.Get(ClavesBB.Visible, false) && bb.Get(ClavesBB.TieneUltimaPos, false);
        }
    }
}

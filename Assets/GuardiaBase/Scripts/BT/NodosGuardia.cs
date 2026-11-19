using UDIT.IA3.BT;

namespace UDIT.IA3.Guardia
{
    internal sealed class NodoPatrullar : ActionNode
    {
        public NodoPatrullar(AccionesGuardia acciones)
            : base("Patrullar", bb => acciones.Patrullar(bb) ? Status.Running : Status.Failure) { }
    }

    internal sealed class NodoInvestigar : ActionNode
    {
        public NodoInvestigar(AccionesGuardia acciones) : base("Investigar", bb =>
        {
            acciones.Investigar(bb);
            return Status.Running;
        }) { }
    }

    internal sealed class NodoPerseguir : ActionNode
    {
        public NodoPerseguir(AccionesGuardia acciones) : base("Perseguir", bb =>
        {
            acciones.Perseguir(bb);
            return Status.Running;
        }) { }
    }

    internal sealed class NodoBuscar : ActionNode
    {
        public NodoBuscar(AccionesGuardia acciones)
            : base("Buscar", bb => acciones.Buscar(bb) ? Status.Running : Status.Failure) { }
    }
}

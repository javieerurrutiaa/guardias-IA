
using UDIT.IA3.BT;

namespace UDIT.IA3.Guardia
{
    internal class NodoPatrullar : ActionNode
    {
        public NodoPatrullar(AccionesGuardia acciones)
            : base("Patrullar", bb =>
            {
                if (acciones.Patrullar(bb))
                {
                    return Status.Running;
                }
                else
                {
                    return Status.Failure;
                }
            })
        {
        }
    }

    internal class NodoInvestigar : ActionNode
    {
        public NodoInvestigar(AccionesGuardia acciones)
            : base("Investigar", bb =>
            {
                acciones.Investigar(bb);

                return Status.Running;
            })
        {
        }
    }

    internal class NodoPerseguir : ActionNode
    {
        public NodoPerseguir(AccionesGuardia acciones)
            : base("Perseguir", bb =>
            {
                acciones.Perseguir(bb);

                return Status.Running;
            })
        {
        }
    }

    internal class NodoBuscar : ActionNode
    {
        public NodoBuscar(AccionesGuardia acciones)
            : base("Buscar", bb =>
            {
                if (acciones.Buscar(bb))
                {
                    return Status.Running;
                }
                else
                {
                    return Status.Success;
                }
            })
        {
        }
    }
}

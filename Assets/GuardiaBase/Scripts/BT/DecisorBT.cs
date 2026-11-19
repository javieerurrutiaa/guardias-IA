// Basado en el decisor de ejemplo de la práctica de IA3 de Luis Peña Sánchez (UDIT).
using UDIT.IA3.BT;

namespace UDIT.IA3.Guardia
{
    public class DecisorBT : IDecisor
    {
        private readonly GuardiaAgente _agente;
        private BehaviorTree _arbol;

        public DecisorBT(GuardiaAgente agente) { _agente = agente; }

        public void Iniciar(Blackboard bb)
        {
            AccionesGuardia acciones = _agente.Acciones;

            // Los monitores comprueban la guarda en cada tick mientras se ejecuta la acción.
            var raiz = new ActiveSelector("Guardia",
                new Monitor("SiAlerta",
                    new Condition("Alerta con contacto", CondicionesGuardia.PuedePerseguir),
                    new NodoPerseguir(acciones)),
                new Monitor("SiSospecha",
                    new Condition("Sospecha con contacto", CondicionesGuardia.PuedeInvestigar),
                    new NodoInvestigar(acciones)),
                new Monitor("SiPerdido",
                    new Condition("Rastro vigente", CondicionesGuardia.PuedeBuscar),
                    new NodoBuscar(acciones)),
                new NodoPatrullar(acciones));

            _arbol = new BehaviorTree(raiz, bb);
        }

        public void Decidir(Blackboard bb)
        {
            if (_arbol != null) _arbol.Tick();
        }

        public string Traza { get { return "BT: " + Trazador.Ultima(); } }
    }
}

// Basado en el estado de ejemplo de la práctica de IA3 de Luis Peña Sánchez (UDIT).
namespace UDIT.IA3.Guardia
{
    public class EstadoPatrullar : State
    {
        public EstadoPatrullar(StateMachine machine) : base(machine) { }

        internal override void Enter()
        {
            Agente.Acciones.Patrullar(Pizarra);
        }

        internal override void Execute()
        {
            if (CondicionesGuardia.PuedePerseguir(Pizarra))
                StateMachine.ChangeState(new EstadoPerseguir(StateMachine));
            else if (CondicionesGuardia.PuedeInvestigar(Pizarra))
                StateMachine.ChangeState(new EstadoInvestigar(StateMachine));
            else if (CondicionesGuardia.PuedeBuscar(Pizarra))
                StateMachine.ChangeState(new EstadoBuscar(StateMachine));
            else
                Agente.Acciones.Patrullar(Pizarra);
        }
    }
}

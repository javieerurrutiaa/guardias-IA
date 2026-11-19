namespace UDIT.IA3.Guardia
{
    internal sealed class EstadoInvestigar : State
    {
        public EstadoInvestigar(StateMachine machine) : base(machine) { }

        internal override void Enter()
        {
            Agente.Acciones.Investigar(Pizarra);
        }

        internal override void Execute()
        {
            if (CondicionesGuardia.PuedePerseguir(Pizarra))
                StateMachine.ChangeState(new EstadoPerseguir(StateMachine));
            else if (CondicionesGuardia.PuedeInvestigar(Pizarra))
                Agente.Acciones.Investigar(Pizarra);
            else if (CondicionesGuardia.PuedeBuscar(Pizarra))
                StateMachine.ChangeState(new EstadoBuscar(StateMachine));
            else
                StateMachine.ChangeState(new EstadoPatrullar(StateMachine));
        }
    }
}

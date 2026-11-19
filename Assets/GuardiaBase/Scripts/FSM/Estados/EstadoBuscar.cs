namespace UDIT.IA3.Guardia
{
    internal sealed class EstadoBuscar : State
    {
        public EstadoBuscar(StateMachine machine) : base(machine) { }

        internal override void Enter()
        {
            if (!Agente.Acciones.Buscar(Pizarra))
                StateMachine.ChangeState(new EstadoPatrullar(StateMachine));
        }

        internal override void Execute()
        {
            if (CondicionesGuardia.PuedePerseguir(Pizarra))
                StateMachine.ChangeState(new EstadoPerseguir(StateMachine));
            else if (CondicionesGuardia.PuedeInvestigar(Pizarra))
                StateMachine.ChangeState(new EstadoInvestigar(StateMachine));
            else if (!CondicionesGuardia.PuedeBuscar(Pizarra) || !Agente.Acciones.Buscar(Pizarra))
                StateMachine.ChangeState(new EstadoPatrullar(StateMachine));
        }
    }
}

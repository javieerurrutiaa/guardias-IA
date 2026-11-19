namespace UDIT.IA3.Guardia
{
    internal sealed class EstadoPerseguir : State
    {
        public EstadoPerseguir(StateMachine machine) : base(machine) { }

        internal override void Enter()
        {
            Agente.Acciones.Perseguir(Pizarra);
        }

        internal override void Execute()
        {
            if (CondicionesGuardia.PuedePerseguir(Pizarra))
                Agente.Acciones.Perseguir(Pizarra);
            else if (CondicionesGuardia.PuedeInvestigar(Pizarra))
                StateMachine.ChangeState(new EstadoInvestigar(StateMachine));
            else if (CondicionesGuardia.PuedeBuscar(Pizarra))
                StateMachine.ChangeState(new EstadoBuscar(StateMachine));
            else
                StateMachine.ChangeState(new EstadoPatrullar(StateMachine));
        }
    }
}

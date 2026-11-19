// ═══════════════════════════════════════════════════════════════════════════
//  DecisorFSM.cs — Cerebro versión 1: máquina de estados
//
//  Inteligencia Artificial (IA3) · GDDVV 3º · UDIT · Curso 2026/2027
//  Dr. Luis Peña Sánchez
//
//  APARTADO B DEL ENUNCIADO.
//
//  El comportamiento vive en las clases de estado, en Scripts/FSM/Estados/.
// ═══════════════════════════════════════════════════════════════════════════

using UDIT.IA3.BT;

namespace UDIT.IA3.Guardia
{
    public class DecisorFSM : IDecisor
    {
        private readonly GuardiaAgente _agente;
        private StateMachine _maquina;
        private bool _iniciada;

        public DecisorFSM(GuardiaAgente agente) { _agente = agente; }

        public void Iniciar(Blackboard bb)
        {
            _maquina = new StateMachine(_agente, bb);
            _iniciada = false;
        }

        public void Decidir(Blackboard bb)
        {
            if (_iniciada)
            {
                _maquina.Execute();
                return;
            }

            // El primer tick usa la situación actual de la pizarra, también al cambiar de cerebro.
            _iniciada = true;
            if (CondicionesGuardia.PuedePerseguir(bb))
                _maquina.Start(new EstadoPerseguir(_maquina));
            else if (CondicionesGuardia.PuedeInvestigar(bb))
                _maquina.Start(new EstadoInvestigar(_maquina));
            else if (CondicionesGuardia.PuedeBuscar(bb))
                _maquina.Start(new EstadoBuscar(_maquina));
            else
                _maquina.Start(new EstadoPatrullar(_maquina));
        }

        public string Traza
        {
            get { return "FSM: " + (_maquina == null ? "-" : _maquina.CurrentName); }
        }
    }
}

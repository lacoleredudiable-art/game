namespace Dovus.Core.RuleEngineV4
{
    /// <summary>Komut planındaki tek adım; yürütme katmanı (Unity) PR2+.</summary>
    public abstract class PhysicsCommand
    {
        public abstract PhysicsCommandKind Kind { get; }
    }
}

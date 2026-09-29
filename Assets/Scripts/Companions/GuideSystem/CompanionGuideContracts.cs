namespace RPG.Companions
{
    /// <summary>
    /// Точка расширения CompanionController. Пока IsOverriding == true, контроллер в состоянии
    /// Following вместо TickFollow() вызывает Tick() аддона. Бой контроллер по-прежнему обрабатывает сам.
    /// </summary>
    public interface ICompanionFollowOverride
    {
        bool IsOverriding { get; }
        void Tick();

        /// <summary>Контроллер ушёл в бой — аддон должен отпустить агента и анимацию.</summary>
        void OnInterrupted();
    }

    /// <summary>
    /// Адаптер к квестовой системе: отдаёт id GuideTarget'а текущей цели активного задания.
    /// Реализуется на любом MonoBehaviour и назначается в CompanionGuide.
    /// </summary>
    public interface IGuideTargetSource
    {
        bool TryGetCurrentTargetId(out string targetId);
    }
}

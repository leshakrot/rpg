namespace RPG.Companions
{
    /// <summary>
    /// Хранит id GuideTarget, выбранной игроком в динамическом списке активных квестов
    /// (CompanionQuestGuideChoiceProvider), до срабатывания CompanionGuide.GuideToPendingSelection()
    /// на ноде-продолжении диалога.
    /// </summary>
    public static class QuestGuideSelection
    {
        public static string PendingTargetId { get; private set; }

        public static void Set(string targetId) => PendingTargetId = targetId;
        public static void Clear() => PendingTargetId = null;
    }
}

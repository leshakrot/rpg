
namespace YG
{
    [System.Serializable]
    public class SavesYG
    {
        // "Технические сохранения" для работы плагина (Не удалять)
        public int idSave;
        public bool isFirstSession = true;
        public string language = "ru";
        public bool promptDone;

        // Тестовые сохранения для демо сцены
        // Можно удалить этот код, но тогда удалите и демо (папка Example)
        public int money = 1;                       // Можно задать полям значения по умолчанию
        public string newPlayerName = "Hello!";
        public bool[] openLevels = new bool[3];

        // Ваши сохранения

        // Основные игровые сохранения (интеграция с системой сохранений)
        public string currentSaveFile = "";
        public string gameDataJson = "";
        public int lastSceneBuildIndex = 0;
        
        // Дополнительные сохранения для нескольких слотов (если нужно)
        public string saveSlot1Name = "";
        public string saveSlot1Data = "";
        public string saveSlot2Name = "";  
        public string saveSlot2Data = "";
        public string saveSlot3Name = "";
        public string saveSlot3Data = "";

        // Поля (сохранения) можно удалять и создавать новые. При обновлении игры сохранения ломаться не должны


        // Вы можете выполнить какие то действия при загрузке сохранений
        public SavesYG()
        {
            // Допустим, задать значения по умолчанию для отдельных элементов массива

            openLevels[1] = true;
        }
    }
}

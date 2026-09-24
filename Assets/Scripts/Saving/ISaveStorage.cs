using System.Collections.Generic;

namespace GameDevTV.Saving
{
    /// <summary>
    /// Хранилище сохранений. Есть две реализации:
    ///   LocalSaveStorage  - гости (файлы на десктопе / PlayerPrefs в WebGL)
    ///   YandexSaveStorage - авторизованные игроки Яндекс.Игр (облако)
    ///
    /// Формат данных одинаковый: Dictionary&lt;string, object&gt;
    /// (ключ = уникальный ID SaveableEntity, плюс "lastSceneBuildIndex").
    /// </summary>
    public interface ISaveStorage
    {
        string Name { get; }

        bool Exists(string saveFile);
        bool TryLoad(string saveFile, out Dictionary<string, object> state);
        bool TrySave(string saveFile, Dictionary<string, object> state);
        bool Delete(string saveFile);
        List<string> ListSaves();

        /// <summary>Имя "текущего" сейва (для кнопки "Продолжить"). У каждого хранилища своё.</summary>
        string GetCurrentSaveName();
        void SetCurrentSaveName(string saveFile);
    }
}

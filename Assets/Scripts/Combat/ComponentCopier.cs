using UnityEngine;

namespace RPG.Combat
{
    /// <summary>
    /// Копирует сериализованные поля с компонента-шаблона на целевой компонент
    /// через JsonUtility (работает в рантайме без Editor-зависимостей).
    ///
    /// Поддерживаются все поля, помеченные [SerializeField] или public,
    /// которые Unity умеет сериализовать: примитивы, UnityEngine-объекты,
    /// вложенные [Serializable]-классы, массивы, List<T> и т.д.
    ///
    /// Ограничения JsonUtility (те же, что у Unity):
    ///   - Словари (Dictionary) не сериализуются.
    ///   - Абстрактные/полиморфные ссылки через [SerializeReference]
    ///     НЕ копируются этим методом — используй EditorJsonUtility в Editor-коде.
    ///   - Ссылки на другие GameObject/компоненты сцены копируются корректно
    ///     (это именно то, что нужно для _dropLibrary и т.п.).
    /// </summary>
    public static class ComponentCopier
    {
        /// <summary>
        /// Копирует все сериализованные данные с <paramref name="source"/> на
        /// <paramref name="target"/>. Оба компонента должны быть одного типа.
        /// </summary>
        public static void CopySerializedFields(MonoBehaviour source, MonoBehaviour target)
        {
            if (source == null || target == null)
            {
                Debug.LogWarning("[ComponentCopier] source или target равен null, пропускаем.");
                return;
            }

            if (source.GetType() != target.GetType())
            {
                Debug.LogWarning(
                    $"[ComponentCopier] Типы не совпадают: {source.GetType().Name} → {target.GetType().Name}. " +
                    "Копирование может быть некорректным.");
            }

            // JsonUtility.ToJson сериализует только поля, которые Unity умеет
            // сохранять (аналог того, что попадает в .asset/.prefab).
            // JsonUtility.FromJsonOverwrite накладывает их на уже существующий объект,
            // не затрагивая поля, которых нет в JSON (ссылки на компоненты Unity и т.д.).
            string json = JsonUtility.ToJson(source);
            JsonUtility.FromJsonOverwrite(json, target);
        }

        /// <summary>
        /// Добавляет компонент того же типа, что и <paramref name="template"/>,
        /// на <paramref name="targetObject"/>, копирует сериализованные поля
        /// и вызывает <see cref="IInitializableComponent.OnAfterDynamicAdd"/> если реализован.
        /// </summary>
        /// <returns>Добавленный компонент или null при ошибке.</returns>
        public static MonoBehaviour AddAndCopy(
            MonoBehaviour template,
            GameObject targetObject)
        {
            if (template == null)
            {
                Debug.LogWarning("[ComponentCopier] template равен null, пропускаем.");
                return null;
            }

            var type = template.GetType();
            var added = targetObject.AddComponent(type) as MonoBehaviour;

            if (added == null)
            {
                Debug.LogError(
                    $"[ComponentCopier] Не удалось добавить компонент {type.Name} на {targetObject.name}.");
                return null;
            }

            CopySerializedFields(template, added);
            return added;
        }
    }
}

namespace GameDevTV.Saving
{
    /// <summary>
    /// Implement on components whose runtime state must be persisted.
    /// </summary>
    public interface ISaveable
    {
        object CaptureState();
        void RestoreState(object state);
    }
}

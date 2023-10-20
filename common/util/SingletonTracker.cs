internal class SingletonTracker<T> {
    private T? _singleton;
    public T Ref() {
        if (_singleton != null) {
            return _singleton;
        }
        else {
            throw new System.Exception($"{typeof(T).Name} singleton does not exist");
        }
    }

    public void Ready(T refToSingleton) {
        if (_singleton != null) {
            throw new System.Exception($"{typeof(T).Name} singleton already exists");
        }
        _singleton = refToSingleton;
    }
}

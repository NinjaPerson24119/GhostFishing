public class SingletonTracker<T> {
    private T _singleton;
    private bool _singletonExists = false;
    public T Ref() {
        if (_singletonExists) {
            return _singleton;
        }
        else {
            throw new System.Exception($"{typeof(T).Name} singleton does not exist");
        }
    }

    public void Ready(T refToSingleton) {
        if (_singletonExists) {
            throw new System.Exception($"{typeof(T).Name} singleton already exists");
        }
        _singleton = refToSingleton;
        _singletonExists = true;
    }
}

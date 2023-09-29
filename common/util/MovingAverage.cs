public class MovingAverage {
    private double[] _history;
    private int _windowSize;
    private int _currentIndex = 0;
    private double _currentAverage = 0;

    public MovingAverage(int windowSize) {
        _windowSize = windowSize;
        _history = new double[windowSize];
    }

    public double GetValue() {
        return _currentAverage;
    }

    public void AddValue(float value) {
        _history[_currentIndex] = value;
        _currentIndex = (_currentIndex + 1) % _windowSize;

        _currentAverage = CalculateMovingAverage();
    }

    private double CalculateMovingAverage() {
        double sum = 0;
        for (int i = 0; i < _history.Length; i++) {
            sum += _history[i];
        }
        return sum / _history.Length;
    }
}

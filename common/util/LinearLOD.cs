using System.Collections.Generic;
using Godot;

public class LinearLOD {
    // the distance at which the LOD curve reaches its end value
    private float _distance;
    private int _startValue;
    private int _endValue;
    private int _step;

    private Dictionary <float, float> _distanceToLOD = new Dictionary<float, float>();
    public int ReusedDistances {get; private set;} = 0;
    private Dictionary <float, bool> _computedLODsLogCheck = new Dictionary<float, bool>();

    public LinearLOD(float distance, int startValue, int endValue, int step) {
        _distance = distance;
        _startValue = startValue;
        _endValue = endValue;
        _step = step;
    }

    public int ComputeLOD(float distance) {
        if (_distanceToLOD.ContainsKey(distance)) {
            ReusedDistances++;
            return (int)_distanceToLOD[distance];
        }

        float lodProgressionPercentage = Mathf.Clamp(distance / _distance, 0, 1);
        int result = (int)((1 - lodProgressionPercentage) * _startValue);

        // round to nearest lower multiple of step
        result = result - (result % _step);
        
        // clamp to minimum
        result = Mathf.Max(result, _endValue);

        if (!_computedLODsLogCheck.ContainsKey(result)) {
            _computedLODsLogCheck[result] = true;
            GD.Print($"Computed LOD at distance {distance} with result {result} (progression percentage {lodProgressionPercentage}%))");
        }

        _distanceToLOD[distance] = result;
        return result;
    }
}

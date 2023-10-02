public class ConnectedArray {
    // checks if an array defining a space is connected
    public static bool IsArrayConnected(int width, int height, bool[] array) {
        for (int x = 0; x < width; x++) {
            for (int y = 0; y < width; y++) {
                if (array[y * width + x]) {
                    bool left = x > 0 && array[y * width + x - 1];
                    bool right = x < width - 1 && array[y * width + x + 1];
                    bool up = y > 0 && array[(y - 1) * width + x];
                    bool down = y < height - 1 && array[(y + 1) * width + x];
                    if (!left && !right && !up && !down) {
                        return false;
                    }
                }
            }
        }
        return true;
    }
}

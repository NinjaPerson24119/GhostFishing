public static class ConnectedArray {
    // checks if an array defining a space is connected
    // TODO: this won't guard against splitting the inventory in half
    public static bool IsArrayConnected(int width, int height, bool[] array) {
        // Short circuit cause this is just broken
        for (int x = 0; x < width; x++) {
            for (int y = 0; y < width; y++) {
                if (array[y * width + x]) {
                    bool left = width == 1 || (x > 0 && array[y * width + x - 1]);
                    bool right = width == 1 || (x < width - 1 && array[y * width + x + 1]);
                    bool up = height == 1 || (y > 0 && array[(y - 1) * width + x]);
                    bool down = height == 1 || (y < height - 1 && array[(y + 1) * width + x]);
                    if (!left && !right && !up && !down) {
                        return false;
                    }
                }
            }
        }
        return true;
    }
}

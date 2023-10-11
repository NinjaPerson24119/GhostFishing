public static class ConnectedArray {
    // checks if an array defining a space is connected
    public static bool IsArrayConnected(int width, int height, bool[] array) {
        DebugTools.Assert(width * height == array.Length, "Array size does not match width and height");
        DebugTools.Assert(width * height < 100, "Array size is too large, we might get a stack overflow");

        // find the first filled tile
        int firstFilled = -1;
        for (int i = 0; i < width * height; i++) {
            if (array[i]) {
                firstFilled = i;
                break;
            }
        }

        bool[] visited = new bool[width * height];
        depthFirstSearch(width, height, array, firstFilled, visited);

        // validate that all filled tiles are connected
        for (int i = 0; i < width * height; i++) {
            if (array[i] && !visited[i]) {
                return false;
            }
        }
        return true;
    }

    private static void depthFirstSearch(int width, int height, bool[] array, int idx, bool[] visited) {
        int x = idx % width;
        int y = idx / width;

        if (x < 0 || x >= width || y < 0 || y >= height) {
            return;
        }
        if (visited[idx]) {
            return;
        }
        visited[idx] = true;

        depthFirstSearch(width, height, array, y * width + x - 1, visited);
        depthFirstSearch(width, height, array, y * width + x + 1, visited);
        depthFirstSearch(width, height, array, (y - 1) * width + x, visited);
        depthFirstSearch(width, height, array, (y + 1) * width + x, visited);
    }
}

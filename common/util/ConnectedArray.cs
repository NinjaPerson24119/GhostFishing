public static class ConnectedArray {
    // checks if an array defining a space is connected
    public static bool IsArrayConnected(int width, int height, bool[] array) {
        // find the first filled tile
        int firstFilled = -1;
        for (int i = 0; i < width * height; i++) {
            if (array[i]) {
                firstFilled = i;
                break;
            }
        }

        bool[] visited = new bool[width * height];
        visited[firstFilled] = true;
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
        visited[idx] = true;

        int x = idx % width;
        int y = idx / width;

        if (x > 0) {
            int leftIdx = y * width + x - 1;
            if (array[leftIdx] && !visited[leftIdx]) {
                depthFirstSearch(width, height, array, leftIdx, visited);
            }
        }
        if (x < width - 1) {
            int rightIdx = y * width + x + 1;
            if (array[rightIdx] && !visited[rightIdx]) {
                depthFirstSearch(width, height, array, rightIdx, visited);
            }
        }
        if (y > 0) {
            int upIdx = (y - 1) * width + x;
            if (array[upIdx] && !visited[upIdx]) {
                depthFirstSearch(width, height, array, upIdx, visited);
            }
        }
        if (y < height - 1) {
            int downIdx = (y + 1) * width + x;
            if (array[downIdx] && !visited[downIdx]) {
                depthFirstSearch(width, height, array, downIdx, visited);
            }
        }
    }
}

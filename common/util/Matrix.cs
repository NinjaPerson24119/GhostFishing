public struct Matrix<T> {
    public int Width;
    public int Height;
    public T[] Data;

    public Matrix(int width, int height, T[] data) {
        DebugTools.Assert(data.Length == width * height, "Matrix data length must match width * height");
        Width = width;
        Height = height;
        Data = data;
    }

    public void Transpose() {
        T[] newData = new T[Width * Height];
        for (int y = 0; y < Height; y++) {
            for (int x = 0; x < Width; x++) {
                newData[x * Height + y] = Data[y * Width + x];
            }
        }

        int temp = Width;
        Width = Height;
        Height = temp;
        Data = newData;
    }

    public void ReverseColumns() {
        for (int y = 0; y < Height; y++) {
            for (int x = 0; x < Width / 2; x++) {
                int leftIdx = y * Width + x;
                int rightIdx = y * Width + Width - 1 - x;
                T temp = Data[leftIdx];
                Data[leftIdx] = Data[rightIdx];
                Data[rightIdx] = temp;
            }
        }
    }

    public void RotateClockwise90() {
        Transpose();
        ReverseColumns();
    }
}

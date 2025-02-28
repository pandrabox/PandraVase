#include "UnityCG.cginc"

struct Cell
{
    float2 size; //セルのサイズ(テクスチャ全体を1としたときの正規化されたサイズ)
    int x;  //セルのx座標
    int y;  //セルのy座標
    float2 min; //セルの最小座標(左下)(テクスチャ全体を1としたときの正規化されたサイズ)
    float2 max; //セルの最大座標(右上)(テクスチャ全体を1としたときの正規化されたサイズ)
    float aspectRatio; //セルの比率
};

// Function to calculate cell information
inline Cell GetCell(int xMax, int yMax, int x, int y)
{
    Cell cell;
    cell.size = float2(1.0 / xMax, 1.0 / yMax);
    cell.x = x;
    cell.y = yMax-y-1;
    cell.min = float2(cell.x, cell.y) * cell.size;
    cell.max = cell.min + cell.size;
    cell.aspectRatio = cell.size.x / cell.size.y; // 比率を計算
    return cell;
}
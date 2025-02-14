using System;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace GameFramework
{
    [ExecuteInEditMode, AddComponentMenu("2D/TileMap")]
    public class TileMap : MonoBehaviour
    {
        public enum Layout
        {
            CartesianCoordinate,
            IsometricDiamond,
            IsometricDiamondFreestyle,
            IsometricStaggered,
            Hexagonal
        }

        public enum HexOrientation
        {
            PointySideUp,
            FlatSideUp
        }

        [SerializeField]
        private int mapWidth = 25, mapHeight = 25; // The Whole map width(horizontal grid count) and height(vertical grid count)
        private int mapLength;
        [SerializeField]
        private float gridSize = 1; // Grid size when layout is CartesianCoordinate
        [SerializeField]
        private float xRotation = 30; // x axis rotation(to unity x axis counterclockwise) when layout is IsometricDiamondFreestyle
        private float sinX, cosX;
        [SerializeField]
        private float yRotation = 60; // y axis rotation(to unity y axis counterclockwise) when layout is IsometricDiamondFreestyle
        private float sinY, cosY;
        [SerializeField]
        private float isoWidth = 3f, isoHeight = 1.5f; // Single isometric grid width and height when layout is Isometric*, but the value means length along the unity world x y asix when layout is IsometricDiamond and IsometricStaggered, means the length along the tile shape axis when layout is IsometricDiamondFreestyle
        private float isoHalfWidth, isoHalfHeight;
        private Vector2 freestylePivot;
        private float freestyleWidth, freestyleHeight;
        [SerializeField]
        private float outerRadius = 0.5f;
        [SerializeField]
        private HexOrientation orientation = HexOrientation.PointySideUp;
        private float innerRadius = 0;
        private Vector3[] hexCornersOffset = new Vector3[6];
        [SerializeField]
        private Layout mapLayout = Layout.IsometricDiamond;
        [SerializeField]
        private bool showCoord = true;
        [SerializeField]
        private bool onlyShowGridWhenSelected = false;
        [SerializeField]
        private bool is2DMode = true;

        public bool placeInGridCenter = false;

        public Action<int, int> OnUpdateTileAt = (x, y) => { };
        public Action<int, int, GameObject> OnSetGameObject;
        public Action<int, int, GameObject> OnSetGameObjectCompleted;
        public Func<int, GameObject, string> OnRenameGameObject;
        public Action<int, int> OnResize = (width, height) => { };
        public Action OnCompleteReset;

        public int MapWidth
        { get { return mapWidth; } }
        public int MapHeight
        { get { return mapHeight; } }

        public float GridSize
        { get { return gridSize; } }

        public float IsoWidth
        { get { return isoWidth; } }
        public float IsoHeight
        { get { return isoHeight; } }

        public float IsoHalfWidth
        { get { return isoHalfWidth; } }
        public float IsoHalfHeight
        { get { return isoHalfHeight; } }

        public float XRotation
        { get { return xRotation; } }
        public float YRotation
        { get { return yRotation; } }

        public float OuterRadius
        { get { return outerRadius; } }
        public HexOrientation Orientation
        { get { return orientation; } }
        public float InnerRadius
        { get { return innerRadius; } }
        public Vector3[] HexCornersOffset
        { get { return hexCornersOffset; } }

        public Layout MapLayout
        { get { return mapLayout; } }

        public Vector2 FreestylePivot
        { get { return freestylePivot; } }
        public float FreestyleWidth
        { get { return freestyleWidth; } }
        public float FreestyleHeight
        { get { return freestyleHeight; } }

        public bool ShowCoord
        { get { return showCoord; } set { showCoord = value; } }
        public bool OnlyShowGridWhenSelected
        { get { return onlyShowGridWhenSelected; } set { onlyShowGridWhenSelected = value; } }
        public bool Is2DMode
        { get { return is2DMode; } set { is2DMode = value; } }

        //使用一维数组来存储地图上的物体，性能最好
        private GameObject[] gameObjectArray;

        private void Awake()
        {
            Initialize();
        }

        public void Initialize()
        {
            UpdateIsoHalfValue();
            UpdateFreestyle();
            UpdateHexInnerRadius();
            UpdateHexCornerOffset();

            mapLength = mapWidth * mapHeight;
            if (gameObjectArray == null || mapLength != gameObjectArray.Length)
            {
                gameObjectArray = new GameObject[mapLength];
            }
        }

        public void ResizeMap(int newWidth, int newHeight)
        {
            if ((newWidth <= 0 || newHeight <= 0) || (this.mapWidth == newWidth && this.mapHeight == newHeight))
                return;

            mapWidth = newWidth;
            mapHeight = newHeight;
            UpdateIsoHalfValue();
            UpdateFreestyle();
            UpdateHexInnerRadius();
            UpdateHexCornerOffset();

            mapLength = mapWidth * mapHeight;
            if (gameObjectArray == null)
            {
                gameObjectArray = new GameObject[mapLength];
            }
            else if (mapLength > gameObjectArray.Length)
            {
                GameObject[] copy = new GameObject[mapLength];
                gameObjectArray.CopyTo(copy, 0);
                gameObjectArray = copy;
            }
            else if (mapLength < gameObjectArray.Length)
            {
                GameObject[] copy = new GameObject[mapLength];
                for (int i = 0; i < mapWidth; i++)
                {
                    for (int j = 0; j < mapHeight; j++)
                    {
                        int mapIndex = GetMapIndex(i, j);
                        copy[mapIndex] = gameObjectArray[mapIndex];
                    }
                }
                gameObjectArray = copy;
            }
        }

        public void ChangeMode(bool newMode)
        {
            is2DMode = newMode;
            UpdateHexCornerOffset();
            RefreshWholeMap();
        }

        public void ResizeGrid(float gridSize)
        {
            this.gridSize = gridSize;
            RefreshWholeMap();
        }

        public void ResizeHexGrid(float outerRadius)
        {
            this.outerRadius = outerRadius;
            UpdateHexInnerRadius();
            UpdateHexCornerOffset();
            RefreshWholeMap();
        }

        public void ResizeHexOrientation(HexOrientation orientation)
        {
            this.orientation = orientation;
            UpdateHexCornerOffset();
            RefreshWholeMap();
        }

        private void UpdateHexInnerRadius()
        {
            this.innerRadius = outerRadius * 0.866025404f;
        }

        private void UpdateHexCornerOffset()
        {
            /*
              PointyUp point position, value is offset to center
                  4
                5 ︿ 3
                |    |
                0 ﹀ 2
                   1

              Flat up point position, value is offset to center
               4 ___3
               5/   \2
                \___/
                0   1
            */
            if (is2DMode)
            {
                if (orientation == HexOrientation.PointySideUp)
                {
                    hexCornersOffset[0] = new Vector2(-innerRadius, -outerRadius / 2);
                    hexCornersOffset[1] = new Vector2(0, -outerRadius);
                    hexCornersOffset[2] = new Vector2(innerRadius, -outerRadius / 2);
                    hexCornersOffset[3] = new Vector2(innerRadius, outerRadius / 2);
                    hexCornersOffset[4] = new Vector2(0, outerRadius);
                    hexCornersOffset[5] = new Vector2(-innerRadius, outerRadius / 2);
                }
                else
                {
                    hexCornersOffset[0] = new Vector2(-outerRadius / 2, -innerRadius);
                    hexCornersOffset[1] = new Vector2(outerRadius / 2, -innerRadius);
                    hexCornersOffset[2] = new Vector2(outerRadius, 0);
                    hexCornersOffset[3] = new Vector2(outerRadius / 2, innerRadius);
                    hexCornersOffset[4] = new Vector2(-outerRadius / 2, innerRadius);
                    hexCornersOffset[5] = new Vector2(-outerRadius, 0);
                }
            }
            else
            {
                if (orientation == HexOrientation.PointySideUp)
                {
                    hexCornersOffset[0] = new Vector3(-innerRadius, 0, -outerRadius / 2);
                    hexCornersOffset[1] = new Vector3(0, 0, -outerRadius);
                    hexCornersOffset[2] = new Vector3(innerRadius, 0, -outerRadius / 2);
                    hexCornersOffset[3] = new Vector3(innerRadius, 0, outerRadius / 2);
                    hexCornersOffset[4] = new Vector3(0, 0, outerRadius);
                    hexCornersOffset[5] = new Vector3(-innerRadius, 0, outerRadius / 2);
                }
                else
                {
                    hexCornersOffset[0] = new Vector3(-outerRadius / 2, 0, -innerRadius);
                    hexCornersOffset[1] = new Vector3(outerRadius / 2, 0, -innerRadius);
                    hexCornersOffset[2] = new Vector3(outerRadius, 0, 0);
                    hexCornersOffset[3] = new Vector3(outerRadius / 2, 0, innerRadius);
                    hexCornersOffset[4] = new Vector3(-outerRadius / 2, 0, innerRadius);
                    hexCornersOffset[5] = new Vector3(-outerRadius, 0, 0);
                }
            }
        }

        public void ChangeFreestyleRotation(float xRotation, float yRotation)
        {
            this.xRotation = xRotation;
            this.yRotation = yRotation;
            UpdateFreestyle();
            RefreshWholeMap();
        }

        private void UpdateFreestyle()
        {
            /*          y
                        ↑
                        │    /
             ╲yRotation│   /
                ╲      │  /
                   ╲ ☇│ /  xRotation
                      ╲│/ ↶
             —————————————➝ x
            */
            sinX = Mathf.Sin(xRotation * Mathf.Deg2Rad);
            cosX = Mathf.Cos(xRotation * Mathf.Deg2Rad);
            sinY = Mathf.Sin(yRotation * Mathf.Deg2Rad);
            cosY = Mathf.Cos(yRotation * Mathf.Deg2Rad);

            freestyleWidth = cosX * isoWidth + sinY * isoHeight;
            freestyleHeight = sinX * isoWidth + cosY * isoHeight;
            freestylePivot = new Vector2(sinY * isoHeight / freestyleWidth, 0);
        }

        public void ResizeIso(float isoWidth, float isoHeight)
        {
            this.isoWidth = isoWidth;
            this.isoHeight = isoHeight;
            UpdateIsoHalfValue();
            UpdateFreestyle();
            RefreshWholeMap();
        }

        private void UpdateIsoHalfValue()
        {
            isoHalfWidth = isoWidth / 2;
            isoHalfHeight = isoHeight / 2;
        }

        public void ChangeLayout(Layout layout)
        {
            mapLayout = layout;
            RefreshWholeMap();
        }

        public bool IsInBounds(Point point)
        {
            return IsInBounds(point.x, point.y);
        }

        public bool IsInBounds(int x, int y)
        {
            return (0 <= x && x < mapWidth && y >= 0 && y < mapHeight);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int GetMapIndex(int x, int y)
        {
            return y * mapWidth + x;
        }

        public bool IsEdge(Point point)
        {
            return IsEdge(point.x, point.y);
        }

        public bool IsEdge(int x, int y)
        {
            bool isEdge = false;
            for (int i = 0; i < Point.NeighbourOffset8.Length; i++)
            {
                Point p = Point.NeighbourOffset8[i];
                bool exists = GetGameObjectAt(p.x + x, p.y + y) != null;
                if (!exists)
                {
                    isEdge = true;
                    break;
                }
            }
            return isEdge;
        }

        public GameObject GetGameObjectAt(Vector3 worldPosition)
        {
            return GetGameObjectAt(WorldPosition2Coordinate(worldPosition));
        }

        public GameObject GetGameObjectAt(Point point)
        {
            return GetGameObjectAt(point.x, point.y);
        }

        public GameObject GetGameObjectAt(int x, int y)
        {
            if (!IsInBounds(x, y))
                return null;

            return gameObjectArray[GetMapIndex(x, y)];
        }

        public bool SetGameObjectAt(Vector2 worldPosition, GameObject gameObject)
        {
            return SetGameObjectAt(WorldPosition2Coordinate(worldPosition), gameObject);
        }

        public bool SetGameObjectAt(Point point, GameObject gameObject)
        {
            return SetGameObjectAt(point.x, point.y, gameObject);
        }

        public bool SetGameObjectAt(int x, int y, GameObject gameObject)
        {
            if (IsInBounds(x, y))
            {
                int mapIndex = GetMapIndex(x, y);
                gameObjectArray[mapIndex] = gameObject;

                if (gameObject != null)
                {
                    if (OnRenameGameObject != null)
                    {
                        string newName = OnRenameGameObject(mapIndex, gameObject);
                        gameObject.name = newName;
                    }
                    else
                    {
                        string[] array = gameObject.name.Split('_');
                        if (array.Length >= 2)
                        {
                            gameObject.name = $"[{x},{y}]_{array[1]}";
                        }
                        else
                        {
                            gameObject.name = $"[{x},{y}]_{gameObject.name}";
                        }
                    }
                    gameObject.transform.SetParent(this.transform);
                    gameObject.transform.localPosition = Coordinate2LocalPosition(x, y, placeInGridCenter);
                    OnSetGameObject?.Invoke(x, y, gameObject);
                    OnSetGameObjectCompleted?.Invoke(x, y, gameObject);
                }
                return true;
            }
            return false;
        }

        public void RefreshWholeMap()
        {
        }

        private static GameObject[] nearTileList = new GameObject[9];

        public GameObject[] GetNeighbours(Point point)
        {
            return GetNeighbours(point.x, point.y);
        }

        public GameObject[] GetNeighbours(int x, int y)
        {
            Point position = new Point(x, y);

            nearTileList[0] = GetGameObjectAt(position.x, position.y);
            nearTileList[1] = GetGameObjectAt(position.x, position.y - 1);
            nearTileList[2] = GetGameObjectAt(position.x + 1, position.y);
            nearTileList[3] = GetGameObjectAt(position.x, position.y + 1);
            nearTileList[4] = GetGameObjectAt(position.x - 1, position.y);
            nearTileList[5] = GetGameObjectAt(position.x + 1, position.y - 1);
            nearTileList[6] = GetGameObjectAt(position.x + 1, position.y + 1);
            nearTileList[7] = GetGameObjectAt(position.x - 1, position.y + 1);
            nearTileList[8] = GetGameObjectAt(position.x - 1, position.y - 1);

            return nearTileList;
        }

        public void CompleteReset()
        {
            foreach (var instance in gameObjectArray)
            {
                Destroy(instance);
            }
        }

        #region 世界坐标，本地坐标，格子坐标之间的换算

        public Vector3 Coordinate2WorldPosition(Point point, bool isGridCenter = false)
        {
            return Coordinate2WorldPosition(point.x, point.y, isGridCenter);
        }

        public Vector3 Coordinate2WorldPosition(float x, float y, bool isGridCenter = false)
        {
            return this.transform.TransformPoint(Coordinate2LocalPosition(x, y, isGridCenter));
        }

        public Vector3 GetGridCenterOffset()
        {
            if (is2DMode)
            {
                if (mapLayout == Layout.CartesianCoordinate)
                {
                    return new Vector3(0.5f * gridSize, 0.5f * gridSize);
                }
                else if (mapLayout == Layout.IsometricDiamond)
                {
                    return new Vector3(0, isoHalfHeight);
                }
                else if (mapLayout == Layout.IsometricDiamondFreestyle)
                {
                    return new Vector3((isoWidth * cosX - isoHeight * sinY) * 0.5f, (isoWidth * sinX + isoHeight * cosY) * 0.5f);
                }
                else if (mapLayout == Layout.IsometricStaggered)
                {
                    return new Vector3(0, isoHalfHeight);
                }
                else if (mapLayout == Layout.Hexagonal)
                {
                    return Vector3.zero;
                }
            }
            else
            {
                if (mapLayout == Layout.CartesianCoordinate)
                {
                    return new Vector3(0.5f * gridSize, 0, 0.5f * gridSize);
                }
                else if (mapLayout == Layout.IsometricDiamond)
                {
                    return new Vector3(0, 0, isoHalfHeight);
                }
                else if (mapLayout == Layout.IsometricDiamondFreestyle)
                {
                    return new Vector3((isoWidth * cosX - isoHeight * sinY) * 0.5f, 0, (isoWidth * sinX + isoHeight * cosY) * 0.5f);
                }
                else if (mapLayout == Layout.IsometricStaggered)
                {
                    return new Vector3(0, 0, isoHalfHeight);
                }
                else if (mapLayout == Layout.Hexagonal)
                {
                    return Vector3.zero;
                }
            }
            return Vector3.zero;
        }

        public Vector3 Coordinate2LocalPosition(Point p, bool isGridCenter = false)
        {
            return Coordinate2LocalPosition(p.x, p.y, isGridCenter);
        }

        public Vector3 Coordinate2LocalPosition(float x, float y, bool isGridCenter = false)
        {
            Vector3 localPosition = Vector3.zero;
            if (is2DMode)
            {
                if (mapLayout == Layout.CartesianCoordinate)
                {
                    localPosition = new Vector3(x * gridSize, y * gridSize);
                }
                else if (mapLayout == Layout.IsometricDiamond)
                {
                    localPosition = new Vector3(x * isoHalfWidth - y * isoHalfWidth, y * isoHalfHeight + x * isoHalfHeight);
                }
                else if (mapLayout == Layout.IsometricDiamondFreestyle)
                {
                    localPosition = new Vector3(x * isoWidth * cosX - y * isoHeight * sinY, x * isoWidth * sinX + y * isoHeight * cosY);
                }
                else if (mapLayout == Layout.IsometricStaggered)
                {
                    localPosition = new Vector3(x * isoWidth + Mathf.Abs(y % 2) * isoHalfWidth, y * isoHalfHeight);
                }
                else if (mapLayout == Layout.Hexagonal)
                {
                    if (orientation == HexOrientation.PointySideUp)
                    {
                        localPosition = new Vector3(x * innerRadius * 2 + (y % 2) * innerRadius, y * (outerRadius + outerRadius / 2));
                    }
                    else
                    {
                        localPosition = new Vector3(x * (outerRadius + outerRadius / 2), y * innerRadius * 2 + (x % 2) * innerRadius);
                    }
                }
            }
            else
            {
                if (mapLayout == Layout.CartesianCoordinate)
                {
                    localPosition = new Vector3(x * gridSize, 0, y * gridSize);
                }
                else if (mapLayout == Layout.IsometricDiamond)
                {
                    localPosition = new Vector3(x * isoHalfWidth - y * isoHalfWidth, 0, y * isoHalfHeight + x * isoHalfHeight);
                }
                else if (mapLayout == Layout.IsometricDiamondFreestyle)
                {
                    localPosition = new Vector3(x * isoWidth * cosX - y * isoHeight * sinY, 0, x * isoWidth * sinX + y * isoHeight * cosY);
                }
                else if (mapLayout == Layout.IsometricStaggered)
                {
                    localPosition = new Vector3(x * isoWidth + Mathf.Abs(y % 2) * isoHalfWidth, 0, y * isoHalfHeight);
                }
                else if (mapLayout == Layout.Hexagonal)
                {
                    if (orientation == HexOrientation.PointySideUp)
                    {
                        localPosition = new Vector3(x * innerRadius * 2 + (y % 2) * innerRadius, 0, y * (outerRadius + outerRadius / 2));
                    }
                    else
                    {
                        localPosition = new Vector3(x * (outerRadius + outerRadius / 2), 0, y * innerRadius * 2 + (x % 2) * innerRadius);
                    }
                }
            }
            if (isGridCenter)
            {
                localPosition += GetGridCenterOffset();
            }
            return localPosition;
        }

        public Vector2 World2CoordinateFloat(Vector3 worldPos)
        {
            if (is2DMode)
            {
                Vector3 posInTileMap = this.transform.InverseTransformPoint(worldPos);
                if (mapLayout == Layout.CartesianCoordinate)
                {
                    return new Vector2(posInTileMap.x / gridSize, posInTileMap.y / gridSize);
                }
                else if (mapLayout == Layout.IsometricDiamond)
                {
                    return new Vector2(posInTileMap.x / isoWidth + posInTileMap.y / isoHeight,
                                    posInTileMap.y / isoHeight - posInTileMap.x / isoWidth);
                }
                else if (mapLayout == Layout.IsometricDiamondFreestyle)
                {
                    return new Vector2((posInTileMap.x * cosY + posInTileMap.y * sinY) / (cosX * cosY + sinX * sinY) / isoWidth,
                         (posInTileMap.y * cosX - posInTileMap.x * sinX) / (cosX * cosY + sinX * sinY) / isoHeight);
                }
                else if (mapLayout == Layout.IsometricStaggered)
                {
                    int xBasic = Mathf.FloorToInt(posInTileMap.x / isoHalfWidth);
                    int yBasic = Mathf.FloorToInt(posInTileMap.y / isoHalfHeight);
                    int absXMode = Mathf.Abs(xBasic) % 2;
                    int absYMode = Mathf.Abs(yBasic) % 2;

                    if (absXMode == 0 && absYMode == 0)
                    {
                        Vector3 newPoint = posInTileMap - this.Coordinate2LocalPosition(xBasic / 2, yBasic);
                        if (Mathf.Abs(newPoint.y / newPoint.x) >= (isoHeight / isoWidth))
                        {
                            return new Vector2(xBasic / 2, yBasic);
                        }
                        else
                        {
                            return new Vector2(xBasic / 2, yBasic - 1);
                        }
                    }
                    else if (absXMode == 0 && absYMode == 1)
                    {
                        Vector3 newPoint = posInTileMap - this.Coordinate2LocalPosition(xBasic / 2, yBasic);
                        if (Mathf.Abs(newPoint.y / newPoint.x) <= (isoHeight / isoWidth))
                        {
                            return new Vector2(xBasic / 2, yBasic - 1);
                        }
                        else
                        {
                            return new Vector2(xBasic / 2, yBasic);
                        }
                    }
                    else if (absXMode == 1 && absYMode == 0)
                    {
                        Vector3 newPoint = posInTileMap - this.Coordinate2LocalPosition((xBasic + 1) / 2, yBasic);
                        if (Mathf.Abs(newPoint.y / newPoint.x) <= (isoHeight / isoWidth))
                        {
                            return new Vector2(xBasic / 2, yBasic - 1);
                        }
                        else
                        {
                            return new Vector2((xBasic + 1) / 2, yBasic);
                        }
                    }
                    else if (absXMode == 1 && absYMode == 1)
                    {
                        Vector3 newPoint = posInTileMap - this.Coordinate2LocalPosition((xBasic - 1) / 2, yBasic);
                        if (Mathf.Abs(newPoint.y / newPoint.x) <= (isoHeight / isoWidth))
                        {
                            return new Vector2((xBasic + 1) / 2, yBasic - 1);
                        }
                        else
                        {
                            return new Vector2((xBasic - 1) / 2, yBasic);
                        }
                    }
                }
                else if (mapLayout == Layout.Hexagonal)
                {
                    if (orientation == HexOrientation.PointySideUp)
                    {
                        float x = posInTileMap.x / (innerRadius * 2f);
                        float y = -x;

                        float offset = posInTileMap.y / (outerRadius * 3f);
                        x -= offset;
                        y -= offset;

                        int iX = Mathf.RoundToInt(x);
                        int iY = Mathf.RoundToInt(y);
                        int iZ = Mathf.RoundToInt(-x - y);

                        if (iX + iY + iZ != 0)
                        {
                            float dX = Mathf.Abs(x - iX);
                            float dY = Mathf.Abs(y - iY);
                            float dZ = Mathf.Abs(-x - y - iZ);

                            if (dX > dY && dX > dZ)
                            {
                                iX = -iY - iZ;
                            }
                            else if (dZ > dY)
                            {
                                iZ = -iX - iY;
                            }
                        }
                        iX += iZ / 2;

                        return new Vector2(iX, iZ);
                    }
                    else
                    {
                        float y = posInTileMap.y / (innerRadius * 2f);
                        float x = -y;

                        float offset = posInTileMap.x / (outerRadius * 3f);
                        y -= offset;
                        x -= offset;

                        int iX = Mathf.RoundToInt(x);
                        int iY = Mathf.RoundToInt(y);
                        int iZ = Mathf.RoundToInt(-x - y);

                        if (iX + iY + iZ != 0)
                        {
                            float dX = Mathf.Abs(x - iX);
                            float dY = Mathf.Abs(y - iY);
                            float dZ = Mathf.Abs(-x - y - iZ);

                            if (dX > dY && dX > dZ)
                            {
                                iX = -iY - iZ;
                            }
                            else if (dZ > dY)
                            {
                                iZ = -iX - iY;
                            }
                            else if (dY > dZ)
                            {
                                iY = -iX - iZ;
                            }
                        }
                        iY += iZ / 2;

                        return new Vector2(iZ, iY);
                    }
                }
            }
            else
            {
                Vector3 posInTileMap = this.transform.InverseTransformPoint(worldPos);
                if (mapLayout == Layout.CartesianCoordinate)
                {
                    return new Vector2(posInTileMap.x / gridSize, posInTileMap.z / gridSize);
                }
                else if (mapLayout == Layout.IsometricDiamond)
                {
                    return new Vector2(posInTileMap.x / isoWidth + posInTileMap.z / isoHeight,
                                    posInTileMap.z / isoHeight - posInTileMap.x / isoWidth);
                }
                else if (mapLayout == Layout.IsometricDiamondFreestyle)
                {
                    return new Vector2((posInTileMap.x * cosY + posInTileMap.z * sinY) / (cosX * cosY + sinX * sinY) / isoWidth,
                         (posInTileMap.z * cosX - posInTileMap.x * sinX) / (cosX * cosY + sinX * sinY) / isoHeight);
                }
                else if (mapLayout == Layout.IsometricStaggered)
                {
                    int xBasic = Mathf.FloorToInt(posInTileMap.x / isoHalfWidth);
                    int yBasic = Mathf.FloorToInt(posInTileMap.z / isoHalfHeight);
                    int absXMode = Mathf.Abs(xBasic) % 2;
                    int absYMode = Mathf.Abs(yBasic) % 2;

                    if (absXMode == 0 && absYMode == 0)
                    {
                        Vector3 newPoint = posInTileMap - this.Coordinate2LocalPosition(xBasic / 2, yBasic);
                        if (Mathf.Abs(newPoint.z / newPoint.x) >= (isoHeight / isoWidth))
                        {
                            return new Vector2(xBasic / 2, yBasic);
                        }
                        else
                        {
                            return new Vector2(xBasic / 2, yBasic - 1);
                        }
                    }
                    else if (absXMode == 0 && absYMode == 1)
                    {
                        Vector3 newPoint = posInTileMap - this.Coordinate2LocalPosition(xBasic / 2, yBasic);
                        if (Mathf.Abs(newPoint.z / newPoint.x) <= (isoHeight / isoWidth))
                        {
                            return new Vector2(xBasic / 2, yBasic - 1);
                        }
                        else
                        {
                            return new Vector2(xBasic / 2, yBasic);
                        }
                    }
                    else if (absXMode == 1 && absYMode == 0)
                    {
                        Vector3 newPoint = posInTileMap - this.Coordinate2LocalPosition((xBasic + 1) / 2, yBasic);
                        if (Mathf.Abs(newPoint.z / newPoint.x) <= (isoHeight / isoWidth))
                        {
                            return new Vector2(xBasic / 2, yBasic - 1);
                        }
                        else
                        {
                            return new Vector2((xBasic + 1) / 2, yBasic);
                        }
                    }
                    else if (absXMode == 1 && absYMode == 1)
                    {
                        Vector3 newPoint = posInTileMap - this.Coordinate2LocalPosition((xBasic - 1) / 2, yBasic);
                        if (Mathf.Abs(newPoint.z / newPoint.x) <= (isoHeight / isoWidth))
                        {
                            return new Vector2((xBasic + 1) / 2, yBasic - 1);
                        }
                        else
                        {
                            return new Vector2((xBasic - 1) / 2, yBasic);
                        }
                    }
                }
                else if (mapLayout == Layout.Hexagonal)
                {
                    if (orientation == HexOrientation.PointySideUp)
                    {
                        float x = posInTileMap.x / (innerRadius * 2f);
                        float y = -x;

                        float offset = posInTileMap.z / (outerRadius * 3f);
                        x -= offset;
                        y -= offset;

                        int iX = Mathf.RoundToInt(x);
                        int iY = Mathf.RoundToInt(y);
                        int iZ = Mathf.RoundToInt(-x - y);

                        if (iX + iY + iZ != 0)
                        {
                            float dX = Mathf.Abs(x - iX);
                            float dY = Mathf.Abs(y - iY);
                            float dZ = Mathf.Abs(-x - y - iZ);

                            if (dX > dY && dX > dZ)
                            {
                                iX = -iY - iZ;
                            }
                            else if (dZ > dY)
                            {
                                iZ = -iX - iY;
                            }
                        }
                        iX += iZ / 2;

                        return new Vector2(iX, iZ);
                    }
                    else
                    {
                        float y = posInTileMap.y / (innerRadius * 2f);
                        float x = -y;

                        float offset = posInTileMap.z / (outerRadius * 3f);
                        y -= offset;
                        x -= offset;

                        int iX = Mathf.RoundToInt(x);
                        int iY = Mathf.RoundToInt(y);
                        int iZ = Mathf.RoundToInt(-x - y);

                        if (iX + iY + iZ != 0)
                        {
                            float dX = Mathf.Abs(x - iX);
                            float dY = Mathf.Abs(y - iY);
                            float dZ = Mathf.Abs(-x - y - iZ);

                            if (dX > dY && dX > dZ)
                            {
                                iX = -iY - iZ;
                            }
                            else if (dZ > dY)
                            {
                                iZ = -iX - iY;
                            }
                            else if (dY > dZ)
                            {
                                iY = -iX - iZ;
                            }
                        }
                        iY += iZ / 2;

                        return new Vector2(iZ, iY);
                    }
                }
            }
            return Vector2.zero;
        }

        public Point WorldPosition2Coordinate(Vector3 worldPos)
        {
            return (Point)World2CoordinateFloat(worldPos);
        }

        #endregion

        #region 在场景中绘制格子线

        private void OnDrawGizmos()
        {
            if (!onlyShowGridWhenSelected && showCoord)
            {
                DrawGrid();
            }
        }

        private void OnDrawGizmosSelected()
        {
            if (onlyShowGridWhenSelected && showCoord)
            {
                DrawGrid();
            }
        }

        private void DrawGrid()
        {
            int mapWidth = this.MapWidth;
            int mapHeight = this.MapHeight;
            TileMap.Layout mapLayout = this.MapLayout;

            Gizmos.color = Color.black;

            //绘制格子
            if (mapLayout == TileMap.Layout.CartesianCoordinate ||
                mapLayout == TileMap.Layout.IsometricDiamond ||
                mapLayout == TileMap.Layout.IsometricDiamondFreestyle)
            {
                //Draw horizontal grid lines
                for (int i = 0; i <= mapHeight; i++)
                {
                    Vector3 start = this.Coordinate2WorldPosition(0, i);
                    Vector3 end = this.Coordinate2WorldPosition(mapWidth, i);
                    Gizmos.DrawLine(start, end);
                }
                //Draw vertical grid lines
                for (int i = 0; i <= mapWidth; i++)
                {
                    Vector3 start = this.Coordinate2WorldPosition(i, 0);
                    Vector3 end = this.Coordinate2WorldPosition(i, mapHeight);
                    Gizmos.DrawLine(start, end);
                }
            }
            else if (mapLayout == TileMap.Layout.IsometricStaggered)
            {
                //Draw horizontal(/) grid lines
                int hlineCount = 2 + (mapHeight - 1) / 2 + (mapWidth - 1);
                for (int i = 0; i < hlineCount; i++)
                {
                    Vector3 start = Vector3.zero;
                    if (i < (mapHeight + 1) / 2)
                    {
                        int startX = -1;
                        int startY = mapHeight - i * 2 - 1 + mapHeight % 2;
                        start = this.Coordinate2WorldPosition(startX, startY);
                    }
                    else
                    {
                        int startX = i - (mapHeight + 1) / 2;
                        int startY = 0;
                        start = this.Coordinate2WorldPosition(startX, startY);
                    }

                    Vector3 end = Vector3.zero;
                    if (i < mapWidth)
                    {
                        int endX = i;
                        int endY = mapHeight + 1;
                        end = this.Coordinate2WorldPosition(endX, endY);
                    }
                    else if (i <= mapWidth)
                    {
                        int endX = i - mapHeight % 2;
                        int endY = mapHeight;
                        end = this.Coordinate2WorldPosition(endX, endY);
                    }
                    else
                    {
                        int endX = mapWidth;
                        int endY = (mapHeight + mapHeight % 2) - (i - mapWidth) * 2;
                        end = this.Coordinate2WorldPosition(endX, endY);
                    }
                    Gizmos.DrawLine(start, end);
                }
                //Draw vertical(\) grid lines3
                int vlineCount = 2 + mapHeight / 2 + (mapWidth - 1);
                for (int i = 0; i < vlineCount; i++)
                {
                    Vector3 start = Vector3.zero;
                    if (i < (mapHeight + 1) / 2)
                    {
                        int startX = -1;
                        int startY = i * 2 + 1;
                        start = this.Coordinate2WorldPosition(startX, startY);
                    }
                    else if (i == (mapHeight + 1) / 2)
                    {
                        int startX = 0;
                        int startY = (mapHeight + 1) / 2 * 2;
                        start = this.Coordinate2WorldPosition(startX, startY);
                    }
                    else
                    {
                        int startX = i - (mapHeight + 1) / 2 - (1 - mapHeight % 2);
                        int startY = mapHeight + 1;
                        start = this.Coordinate2WorldPosition(startX, startY);
                    }

                    Vector3 end = Vector3.zero;
                    if (i < mapWidth)
                    {
                        int endX = i;
                        int endY = 0;
                        end = this.Coordinate2WorldPosition(endX, endY);
                    }
                    else if (i == mapWidth)
                    {
                        int endX = mapWidth - 1;
                        int endY = 1;
                        end = this.Coordinate2WorldPosition(endX, endY);
                    }
                    else
                    {
                        int endX = mapWidth;
                        int endY = (i - mapWidth) * 2;
                        end = this.Coordinate2WorldPosition(endX, endY);
                    }
                    Gizmos.DrawLine(start, end);
                }
            }
            else if (mapLayout == TileMap.Layout.Hexagonal)
            {
                if (this.Orientation == TileMap.HexOrientation.PointySideUp)
                {
                    int pointCountH = 2 * mapWidth + 1;
                    //Last horizontal line
                    Vector3[] points = new Vector3[pointCountH];
                    points[0] = this.transform.TransformPoint(Coordinate2LocalPosition(0, mapHeight - 1) + this.HexCornersOffset[5]);
                    for (int j = 0; j < mapWidth; j++)
                    {
                        Vector3 baseCorner = this.Coordinate2LocalPosition(j, mapHeight - 1);
                        points[j * 2 + 1] = this.transform.TransformPoint(baseCorner + this.HexCornersOffset[4]);
                        points[j * 2 + 2] = this.transform.TransformPoint(baseCorner + this.HexCornersOffset[3]);
                    }
                    GizmosExtend.DrawPolyLine(points);
                    //Draw horizontal grid lines
                    for (int i = 0; i < mapHeight; i++)
                    {
                        points = new Vector3[pointCountH];
                        points[0] = this.transform.TransformPoint(Coordinate2LocalPosition(0, i) + this.HexCornersOffset[0]);
                        for (int j = 0; j < mapWidth; j++)
                        {
                            Vector3 baseCorner = this.Coordinate2LocalPosition(j, i);
                            points[j * 2 + 1] = this.transform.TransformPoint(baseCorner + this.HexCornersOffset[1]);
                            points[j * 2 + 2] = this.transform.TransformPoint(baseCorner + this.HexCornersOffset[2]);
                        }

                        GizmosExtend.DrawPolyLine(points);
                    }

                    int pointCountV = 2 * mapHeight + 1;
                    //Draw vertical grid lines
                    for (int i = 0; i < mapWidth; i++)
                    {
                        points = new Vector3[pointCountV];
                        for (int j = 0; j < mapHeight; j++)
                        {
                            Vector3 baseCorner = this.Coordinate2LocalPosition(i, j);
                            points[j * 2] = this.transform.TransformPoint(baseCorner + this.HexCornersOffset[0]);
                            points[j * 2 + 1] = this.transform.TransformPoint(baseCorner + this.HexCornersOffset[5]);
                        }
                        Vector3 finalCorner = this.Coordinate2LocalPosition(i, mapHeight - 1);
                        points[pointCountV - 1] = this.transform.TransformPoint(finalCorner + this.HexCornersOffset[4]);

                        GizmosExtend.DrawPolyLine(points);
                    }

                    //Last vertical line
                    points = new Vector3[pointCountV - 1];
                    for (int j = 0; j < mapHeight; j++)
                    {
                        Vector3 baseCorner = this.Coordinate2LocalPosition(mapWidth, j);
                        points[j * 2] = this.transform.TransformPoint(baseCorner + this.HexCornersOffset[0]);
                        points[j * 2 + 1] = this.transform.TransformPoint(baseCorner + this.HexCornersOffset[5]);
                    }

                    GizmosExtend.DrawPolyLine(points);
                }
                else
                {
                    int pointCountH = 2 * mapWidth + 1;
                    //first horizontal line
                    Vector3[] points = new Vector3[pointCountH - 1];
                    for (int j = 0; j < mapWidth; j++)
                    {
                        Vector3 baseCorner = this.Coordinate2LocalPosition(j, 0);
                        points[j * 2] = this.transform.TransformPoint(baseCorner + this.HexCornersOffset[0]);
                        points[j * 2 + 1] = this.transform.TransformPoint(baseCorner + this.HexCornersOffset[1]);
                    }
                    GizmosExtend.DrawPolyLine(points);
                    //Draw horizontal grid lines
                    for (int i = 0; i < mapHeight; i++)
                    {
                        points = new Vector3[pointCountH];
                        points[0] = this.transform.TransformPoint(this.Coordinate2LocalPosition(0, i) + this.HexCornersOffset[5]);
                        for (int j = 0; j < mapWidth; j++)
                        {
                            Vector3 baseCorner = this.Coordinate2LocalPosition(j, i);
                            points[j * 2 + 1] = this.transform.TransformPoint(baseCorner + this.HexCornersOffset[4]);
                            points[j * 2 + 2] = this.transform.TransformPoint(baseCorner + this.HexCornersOffset[3]);
                        }

                        GizmosExtend.DrawPolyLine(points);
                    }

                    int pointCountV = 2 * mapHeight + 1;
                    //first vertical line
                    points = new Vector3[pointCountV - 1];
                    for (int j = 0; j < mapHeight; j++)
                    {
                        Vector3 baseCorner = this.Coordinate2LocalPosition(0, j);
                        points[j * 2] = this.transform.TransformPoint(baseCorner + this.HexCornersOffset[0]);
                        points[j * 2 + 1] = this.transform.TransformPoint(baseCorner + this.HexCornersOffset[5]);
                    }
                    GizmosExtend.DrawPolyLine(points);
                    //Draw vertical grid lines
                    for (int i = 0; i < mapWidth; i++)
                    {
                        points = new Vector3[pointCountV];
                        Vector3 finalCorner = this.Coordinate2LocalPosition(i, 0);
                        points[0] = this.transform.TransformPoint(finalCorner + this.HexCornersOffset[1]);
                        for (int j = 0; j < mapHeight; j++)
                        {
                            Vector3 baseCorner = this.Coordinate2LocalPosition(i, j);
                            points[j * 2 + 1] = this.transform.TransformPoint(baseCorner + this.HexCornersOffset[2]);
                            points[j * 2 + 2] = this.transform.TransformPoint(baseCorner + this.HexCornersOffset[3]);
                        }

                        GizmosExtend.DrawPolyLine(points);
                    }
                }
            }
        }

        #endregion
    }
}

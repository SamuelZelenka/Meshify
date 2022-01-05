using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public partial class Meshify : EditorWindow
{
    private enum Direction { North, West, East, South }
    private const float DEFAULT_Y_OFFSET = 200;

    private float _xOffset;
    private int _scale = 1;
    private float _vertexScale = 1;
    private Texture2D _texture;
    private Color[,] _pixelGrid;
    private List<Vector2Int> _pixels = new List<Vector2Int>();
    private PixelArea[] _areas;

    private int _triCount;
    private int _pixelCount;

    // Add menu named "My Window" to the Window menu
    [MenuItem("Window/Meshify")]
    static void Init()
    {
        // Get existing open window or if none, make a new one:
        Meshify window = (Meshify)EditorWindow.GetWindow(typeof(Meshify));
        window.Show();
    }

    void OnGUI()
    {
        _scale = (int)EditorGUILayout.Slider("Scale", _scale, 1, 100);
        _vertexScale = (int)EditorGUILayout.Slider("Vertex scale", _vertexScale, 1, 100);

        _texture = (Texture2D)EditorGUILayout.ObjectField("Texture2D", _texture, typeof(Texture2D), allowSceneObjects: true);

        if (_texture != null)
        {
            if (GUILayout.Button("Calculate Areas"))
            {
                DrawAreas();
            }
            if (GUILayout.Button("Reset Image"))
            {
                ResetImage();
            }
            DrawImage();
        }
        EditorGUILayout.HelpBox($"{_triCount} tris\n {_pixelCount} pixel count", MessageType.Info);
    }

    public void ResetImage()
    {
        _pixelGrid = new Color[_texture.width, _texture.height];
        _triCount = 0;
        _pixelCount = 0;

        for (int x = 0; x < _texture.width; x++)
        {
            for (int y = 0; y < _texture.height; y++)
            {
                _pixelGrid[x, y] = _texture.GetPixel(x, _texture.height - 1 - y);
                if (_pixelGrid[x, y].a == 1)
                {
                    _pixelCount++;
                }
            }
        }
    }

    public void DrawImage()
    {
        if (_texture != null)
        {
            if (_pixelGrid == null)
            {
                _pixelGrid = new Color[_texture.width, _texture.height];
            }

            _xOffset = position.width / 2 - (_texture.width / 2) * _scale;

            for (int x = 0; x < _texture.width; x++)
            {
                for (int y = 0; y < _texture.height; y++)
                {
          
                    Color tempColor = new Color((float)x / (_texture.width), (float)y / (_texture.height - 1), 0, 1);
                    EditorGUI.DrawRect(new Rect(_xOffset + _scale * x, DEFAULT_Y_OFFSET + _scale * y, _scale, _scale), _pixelGrid[x, y]);
                }
            }
        }
    }

    public void DrawAreas()
    {
        _areas = GetAreas();

        foreach (PixelArea area in _areas)
        {
            Color areaColor = new Color(Random.value, Random.value, Random.value, 1);
            foreach (Vector2Int pixel in area.pixels)
            {
                _pixelGrid[pixel.x, pixel.y] = areaColor;
            }
            
            Handles.DrawLine(area.triangleVertices[1] * _scale, area.triangleVertices[2]);
        }
        _triCount = _areas.Length * 2;
    }

    private PixelArea[] GetAreas()
    {
        List<PixelArea> areas = new List<PixelArea>();

        for (int x = 0; x < _texture.width; x++)
        {
            for (int y = 0; y < _texture.height; y++)
            {
                Vector2Int checkPos = new Vector2Int(x, y);

                // If Not transparent calculate area from pixel
                if (_pixelGrid[x, y].a == 1)
                {
                    Vector2Int[] newArea = new Vector2Int[] { };
                    areas.Add(GetArea(new Vector2Int(x, y), GetRange(checkPos), ref newArea));
                }
            }
        }
        return areas.ToArray();
    }

    private PixelArea GetArea(Vector2Int startPos, int range, ref Vector2Int[] areaPixels)
    {
        List<Vector2Int> validPositions = new List<Vector2Int>();
        validPositions.AddRange(areaPixels);

        for (int y = 0; y < range; y++)
        {
            if (startPos.y + y < _pixelGrid.GetLength(1) && _pixelGrid[startPos.x, startPos.y + y].a == 1)
            {
                validPositions.Add(new Vector2Int(startPos.x, startPos.y + y));
            }
            else
            {
                return new PixelArea(areaPixels);
            }
        }
        areaPixels = validPositions.ToArray();
        foreach (Vector2Int position in areaPixels)
        {
            _pixelGrid[position.x, position.y].a = 0;
        }

        if (startPos.x + 1 < _pixelGrid.GetLength(0))
        {
            return GetArea(new Vector2Int(startPos.x + 1, startPos.y), range, ref areaPixels);
        }
        return new PixelArea(areaPixels);
    }

    private int GetRange(Vector2Int pos)
    {
        int range = 1;

        for (int x = pos.y; x < _texture.height - 1; x++)
        {
            if (x + 1 < _pixelGrid.GetLength(1) && _pixelGrid[pos.x, x + 1].a < 1)
            {
                break;
            }
            range++;
        }
        return range;
    }
}

public struct PixelArea
{
    public Vector2Int[] pixels;
    public Vector2Int topLeft;
    public Vector2Int topRight;
    public Vector2Int bottomLeft;
    public Vector2Int bottomRight;

    public Vector3[] triangleVertices;

    public PixelArea(Vector2Int[] pixels)
    {
        this.pixels = pixels;

        topLeft = new Vector2Int(pixels[0].x, pixels[0].y); //Top left
        bottomLeft = new Vector2Int(pixels[0].x, pixels[pixels.Length - 1].y); // Bottom left
        topRight = new Vector2Int(pixels[pixels.Length - 1].x, pixels[0].y); // Top right
        bottomRight = new Vector2Int(pixels[pixels.Length - 1].x, pixels[pixels.Length - 1].y); // Bottom right

        Vector3[] topTriangle = new Vector3[] { (Vector2)topLeft, (Vector2)topRight, (Vector2)bottomLeft };
        Vector3[] bottomTriangle = new Vector3[] { (Vector2)bottomLeft, (Vector2)topRight, (Vector2)bottomRight };

        triangleVertices = topTriangle.Concat(bottomTriangle).ToArray();
    }
}

public struct Triangle
{
    public Vector3[] vertices;

    public Triangle(Vector3[] vertices)
    {
        this.vertices = vertices;
    }
}
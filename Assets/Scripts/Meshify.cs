using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Random = UnityEngine.Random;

public partial class Meshify : EditorWindow
{
    private const float DEFAULT_Y_OFFSET = 200;

    private int _drawStyle;
    private bool _showWireFrame;
    private float _xOffset;
    private int _scale = 1;
    private Texture2D _texture;
    private Color[,] _imageColorGrid;
    private Color[,] _textureColorGrid;
    private Color[,] _boxColorGrid;
    private PixelArea[] _areas = Array.Empty<PixelArea>();

    private int _triCount;
    private int _pixelCount;
    
    private Texture2D _textureLastFrame;


    [MenuItem("Window/Meshify")]
    private static void Init()
    {
        Meshify window = (Meshify)GetWindow(typeof(Meshify));
        window.Show();
    }

    private void OnGUI()
    {
        EditorGUI.BeginChangeCheck();
        _scale = (int)EditorGUILayout.Slider("Scale", _scale, 1, 100);
        _showWireFrame = EditorGUILayout.Toggle("Show Wire frame", _showWireFrame);
        _drawStyle = EditorGUILayout.Popup("Draw Style", _drawStyle, Enum.GetNames(typeof(DrawStyle)));
        _texture = (Texture2D)EditorGUILayout.ObjectField("Texture2D", _texture, typeof(Texture2D), allowSceneObjects: true);
        
        if (EditorGUI.EndChangeCheck() && _texture)
        {
            ResetImage();
            _areas = GetAreas();
        }


        Draw();

        
        EditorGUILayout.HelpBox($"{_triCount} tris\n {_pixelCount} pixel count", MessageType.Info);
    }

    private void Draw()
    {
        Color[,] selectedGrid = _imageColorGrid;

        switch ((DrawStyle)_drawStyle)
        {
            case DrawStyle.None:
                selectedGrid = null;
                break;
            case DrawStyle.Texture:
                selectedGrid = _textureColorGrid;
                break;
            case DrawStyle.Solid:
                DrawAreasByColor();
                selectedGrid = _boxColorGrid;
                break;
            default:
                break;
        }

        DrawImage(selectedGrid);

        if (_showWireFrame)
        {
            DrawMeshLines();
        }
    }

    private void DrawMeshLines()
    {
        foreach (PixelArea area in _areas)
        {
            Vector2 xScaleOffset = new Vector2(_scale, 0);
            Vector2 yScaleOffset = new Vector2(0, _scale);
            Vector2 xyScaleOffset = xScaleOffset + yScaleOffset;

            (Vector3, Vector3) diagonal = (GetPixelPos(area.topLeft), GetPixelPos(area.bottomRight) + (Vector3)xyScaleOffset);

            (Vector3, Vector3) topLine = (GetPixelPos(area.topLeft), GetPixelPos(area.topRight) + (Vector3)xScaleOffset);

            (Vector3, Vector3) rightLine = (GetPixelPos(area.topRight) + (Vector3)xScaleOffset, GetPixelPos(area.bottomRight) + (Vector3)xyScaleOffset);

            (Vector3, Vector3) bottomLine = (GetPixelPos(area.bottomLeft) + (Vector3)yScaleOffset, GetPixelPos(area.bottomRight) + (Vector3)xyScaleOffset);

            (Vector3, Vector3) leftLine = (GetPixelPos(area.topLeft), GetPixelPos(area.bottomLeft) + (Vector3)yScaleOffset);

            Handles.DrawLine(diagonal.Item1, diagonal.Item2);
            Handles.DrawLine(topLine.Item1, topLine.Item2);
            Handles.DrawLine(rightLine.Item1, rightLine.Item2);
            Handles.DrawLine(bottomLine.Item1, bottomLine.Item2);
            Handles.DrawLine(leftLine.Item1, leftLine.Item2);
        }
    }

    private void ResetImage()
    {
        _imageColorGrid = new Color[_texture.width, _texture.height];
        _textureColorGrid = new Color[_texture.width, _texture.height];
        _boxColorGrid = new Color[_texture.width, _texture.height];
        _triCount = 0;
        _pixelCount = 0;
        _areas = Array.Empty<PixelArea>();

        for (int x = 0; x < _texture.width; x++)
        {
            for (int y = 0; y < _texture.height; y++)
            {
                _imageColorGrid[x, y] = _texture.GetPixel(x, _texture.height - 1 - y);

                _textureColorGrid[x, y] = _imageColorGrid[x, y];
                _boxColorGrid[x, y] = _imageColorGrid[x, y];

                if (_imageColorGrid[x, y].a != 0)
                {
                    _pixelCount++;
                }
            }
        }
    }

    private void DrawImage(Color[,] colorGrid)
    {
        if (_texture != null)
        {
            if (colorGrid == null)
            {
                colorGrid = new Color[_texture.width, _texture.height];
            }

            _xOffset = position.width / 2 - (_texture.width / 2) * _scale;

            for (int xPixel = 0; xPixel < _texture.width; xPixel++)
            {
                for (int yPixel = 0; yPixel < _texture.height; yPixel++)
                {
                    Vector3 pixelPos = GetPixelPos(xPixel, yPixel);
                    Rect pixelRect = new Rect(pixelPos.x, pixelPos.y, _scale, _scale);
                    
                    EditorGUI.DrawRect(pixelRect, colorGrid[xPixel, yPixel]);
                }
            }
        }
    }

    private Vector3 GetPixelPos(Vector2Int pixelCord) => GetPixelPos(pixelCord.x, pixelCord.y);
    private Vector3 GetPixelPos(int xPixel, int yPixel)
    {
        float xPos =  xPixel * _scale + _xOffset;
        float yPos =  yPixel * _scale + DEFAULT_Y_OFFSET;

        return new Vector3(xPos, yPos,0);
    }

    private void DrawAreasByColor()
    {
        foreach (PixelArea area in _areas)
        {
            foreach (Vector2Int pixel in area.pixels)
            {
                _boxColorGrid[pixel.x, pixel.y] = area.color;
            }
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

                if (_boxColorGrid[x, y].a != 0)
                {
                    Vector2Int[] newArea = { };
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
            bool isInRangeOfTexture = startPos.y + y < _boxColorGrid.GetLength(1);
            bool isPixel = _boxColorGrid[startPos.x, startPos.y + y].a != 0;

            if (isInRangeOfTexture && isPixel)
            {
                validPositions.Add(new Vector2Int(startPos.x, startPos.y + y));
            }
            else
            {
                return new PixelArea(areaPixels);
            }
        }
        
        areaPixels = validPositions.ToArray();
        
        ExcludeAreaFromSearch(areaPixels);

        if (startPos.x + 1 < _boxColorGrid.GetLength(0))
        {
            Vector2Int startPixel = new Vector2Int(startPos.x + 1, startPos.y);
            return GetArea(startPixel, range, ref areaPixels);
        }
        return new PixelArea(areaPixels);
    }

    private void ExcludeAreaFromSearch(Vector2Int[] areaPixels)
    {
        foreach (Vector2Int position in areaPixels)
        {
            _boxColorGrid[position.x, position.y].a = 0;
        }
    }

    private int GetRange(Vector2Int pos)
    {
        int range = 1;

        for (int x = pos.y; x < _texture.height - 1; x++)
        {
            if (x + 1 < _boxColorGrid.GetLength(1) && _boxColorGrid[pos.x, x + 1].a < 1)
            {
                break;
            }
            range++;
        }
        return range;
    }

    private enum DrawStyle
    {
        None,
        Texture,
        Solid
    }

}

public struct PixelArea
{
    public readonly Vector2Int[] pixels;

    public Vector2Int topLeft;
    public Vector2Int bottomLeft;
    public Vector2Int topRight;
    public Vector2Int bottomRight;

    public Vector3[] triangleVertices;

    public Color color;

    public PixelArea(Vector2Int[] pixels)
    {
        topLeft = new Vector2Int(pixels[0].x, pixels[0].y);
        bottomLeft = new Vector2Int(pixels[0].x, pixels[pixels.Length - 1].y);
        topRight = new Vector2Int(pixels[pixels.Length - 1].x, pixels[0].y);
        bottomRight = new Vector2Int(pixels[pixels.Length - 1].x, pixels[pixels.Length - 1].y);

        this.pixels = pixels;

        Vector3[] topTriangle = { (Vector2)topLeft, (Vector2)topRight, (Vector2)bottomLeft };
        Vector3[] bottomTriangle = { (Vector2)bottomLeft, (Vector2)topRight, (Vector2)bottomRight };

        triangleVertices = topTriangle.Concat(bottomTriangle).ToArray();

        color = new Color(Random.value, Random.value, Random.value, 1);
    }
}
using System.Collections;
using System.Collections.Generic;
using UnityEngine;



public class LevelGrid<T> : MonoBehaviour
{
    protected T[] cells;
    public int Width { get; set; }
    public int Height { get; set; }

    private Rect gridRect;

    private Vector2Int dimensions;
    public Vector2Int Dimensions
    {
        get { return dimensions; }
        set
        {
            dimensions = value;
            Width = value.x;
            Height = value.y;
            cells = new T[value.x * value.y];
            gridRect = new Rect(0, 0, value.x, value.y);
        }
    }

    public LevelGrid()
    {
        this.Width = 0;
        this.Height = 0;
        dimensions = new Vector2Int(0, 0);
    }

    public LevelGrid(int width, int height)
    {
        this.Width = width;
        this.Height = height;
        cells = new T[width * height];
        dimensions = new Vector2Int(width, height);
        gridRect = new Rect(0, 0, width, height);
    }

    public T this[int x, int y]
    {
        get { return cells[GetIndex(x, y)]; }
        set { cells[GetIndex(x, y)] = value; }
    }

    public T this[Vector2Int pos]
    {
        get { return cells[GetIndex(pos.x, pos.y)]; }
        set { cells[GetIndex(pos.x, pos.y)] = value; }
    }

    public void Fill(T val)
    {
        for (int i = 0; i < cells.Length; i++)
        {
            cells[i] = val;
        }
    }

    protected int GetIndex(int x, int y)
    {
        return (y * Width) + x;
    }

}   


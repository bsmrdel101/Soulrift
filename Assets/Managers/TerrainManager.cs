using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Tilemaps;

[System.Serializable]
public struct ResourceData
{
  public string Name;
  public GameObject Prefab;
  public float Weight;
  public float NoiseScale;
  public float Threshold;
  public float Density;
  public float MinSpacing;
  public bool RequiresGround;
}

[System.Serializable]
public struct DecorTile
{
  public string Name;
  public TileBase TileBase;
  public float Weight;
  public float NoiseScale;
  public float Threshold;
}

public enum TileType
{
  Ground,
  Barrier
}

[System.Serializable]
public struct Tile
{
  public string Name;
  public float Threshold;
  public TileType Type;
  public TileBase TileBase;
}

[System.Serializable]
public struct Biome
{
  public string Name;
  public float NoiseScale;
  public TileBase DefaultTileBase;
  public Tile[] Tiles;
  public float DecorDensity;
  public DecorTile[] DecorTiles;
  public ResourceData[] Resources;
}

public class TerrainManager : MonoBehaviour
{
  [Header("Tilemap")]
  [SerializeField] private Tilemap _groundTilemap;
  [SerializeField] private Tilemap _barrierTilemap;
  [SerializeField] private Tilemap _decorTilemap;

  [Header("Terrain Generation")]
  [SerializeField] private int _seed;
  [SerializeField] private int _width = 100;
  [SerializeField] private int _height = 100;
  [SerializeField] private float _biomeNoiseScale = 0.02f;
  [SerializeField] private Biome[] _biomes;
  private List<Vector2> _placedResources = new List<Vector2>();
  private Vector2 _noiseOffset;
  private System.Random _rng;


  private void Awake()
  {
    if (_seed == 0) _seed = Random.Range(0, 100000000);
    for (int i = 0; i < _biomes.Length; i++)
    {
      System.Array.Sort(_biomes[i].Tiles, (a, b) => a.Threshold.CompareTo(b.Threshold));
    }
  }

  private void Start()
  {
    _rng = new System.Random(_seed);
    _noiseOffset = new Vector2(
      (float)_rng.NextDouble() * 1000f,
      (float)_rng.NextDouble() * 1000f
    );
    Generate();
  }


  public void Generate()
  {
    _groundTilemap.ClearAllTiles();
    _barrierTilemap.ClearAllTiles();
    _decorTilemap.ClearAllTiles();

    for (int x = 0; x < _width; x++)
    {
      for (int y = 0; y < _height; y++)
      {
        float biomeSample = Mathf.PerlinNoise(
          x * _biomeNoiseScale + _noiseOffset.x,
          y * _biomeNoiseScale + _noiseOffset.y
        );
        Biome biome = GetBiome(biomeSample);

        float terrainSample = Mathf.PerlinNoise(
          x * biome.NoiseScale + _noiseOffset.x,
          y * biome.NoiseScale + _noiseOffset.y
        );
        Tile tile = GetTile(biome, terrainSample);

        PlaceTile(x, y, tile);
        TryPlaceDecor(x, y, biome, tile);
        TryPlaceResource(x, y, biome, tile);
      }
    }
  }

  private void PlaceTile(int x, int y, Tile tile)
  {
    Vector3Int pos = new Vector3Int(x, y, 0);
    if (tile.Type == TileType.Barrier)
      _barrierTilemap.SetTile(pos, tile.TileBase);
    else
      _groundTilemap.SetTile(pos, tile.TileBase);
  }

  private Biome GetBiome(float value)
  {
    int index = Mathf.FloorToInt(value * _biomes.Length);
    index = Mathf.Clamp(index, 0, _biomes.Length - 1);
    return _biomes[index];
  }

  private Tile GetTile(Biome biome, float value)
  {
    foreach (Tile tile in biome.Tiles)
    {
      if (value < tile.Threshold) return tile;
    }

    return new Tile
    {
      TileBase = biome.DefaultTileBase,
      Type = TileType.Ground
    };
  }

  private void TryPlaceResource(int x, int y, Biome biome, Tile tile)
  {
    foreach (ResourceData resource in biome.Resources)
    {
      if (resource.RequiresGround && tile.Type != TileType.Ground) continue;
      if ((float)_rng.NextDouble() > resource.Density) continue;

      float noise = Mathf.PerlinNoise(
        x * resource.NoiseScale + _noiseOffset.x + 700,
        y * resource.NoiseScale + _noiseOffset.y + 700
      );
      if (noise < resource.Threshold) continue;

      Vector2 pos = new Vector2(x + 0.5f, y + 0.5f);
      foreach (Vector2 placed in _placedResources)
      {
        if (Vector2.Distance(pos, placed) < resource.MinSpacing) return;
      }

      Instantiate(resource.Prefab, pos, Quaternion.identity, transform);
      _placedResources.Add(pos);
    }
  }

  private void TryPlaceDecor(int x, int y, Biome biome, Tile tile)
  {
    if (tile.Type != TileType.Ground || (float)_rng.NextDouble() > biome.DecorDensity) return;

    Vector3Int pos = new Vector3Int(x, y, 0);
    DecorTile? selectedDecor = GetWeightedDecor(biome);
    if (selectedDecor == null) return;

    DecorTile decor = selectedDecor.Value;
    float noise = Mathf.PerlinNoise(
      x * decor.NoiseScale + _noiseOffset.x + 200,
      y * decor.NoiseScale + _noiseOffset.y + 200
    );
    if (noise < decor.Threshold) return;

    _decorTilemap.SetTile(pos, decor.TileBase);
  }

  private DecorTile? GetWeightedDecor(Biome biome)
  {
    float totalWeight = biome.DecorTiles.Sum((decor) => decor.Weight);
    if (totalWeight <= 0f) return null;

    float roll = (float)_rng.NextDouble() * totalWeight;
    float cumulative = 0f;

    foreach (var decor in biome.DecorTiles)
    {
      cumulative += decor.Weight;
      if (roll <= cumulative) return decor;
    }
    return null;
  }

  public void GenerateEditor()
  {
    System.Random rng = new System.Random(_seed);
    _noiseOffset = new Vector2(
      (float)rng.NextDouble() * 1000f,
      (float)rng.NextDouble() * 1000f
    );
    _seed = Random.Range(0, 100000000);
    Generate();
  }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Match3Game
{
    public class GridSystem : MonoBehaviour
    {
        public enum GemType
        {
            NORMAL,
            EMPTY
        };

        [System.Serializable]
        public struct GemPrefab
        {
            public GemType type;
            public GameObject prefab;
        };

        [SerializeField] int xDim;
        [SerializeField] int yDim;

        [SerializeField] GemPrefab[] gemPrefabs;
        [SerializeField] GameObject[] backgroundPrefab;

        private Dictionary<GemType, GameObject> gemPrefabDic;

        private Gem[,] gems;

        private void Start()
        {
            gemPrefabDic = new Dictionary<GemType, GameObject>();
            for (int i = 0; i < gemPrefabs.Length; i++)
            {
                if (!gemPrefabDic.ContainsKey(gemPrefabs[i].type))
                {
                    gemPrefabDic.Add(gemPrefabs[i].type, gemPrefabs[i].prefab);
                }
            }


            if (backgroundPrefab.Length > 0)
            {
                GameObject backGroundParent = new GameObject("BackgroundPrefabs");
                backGroundParent.transform.SetParent(transform);

                for (int x = 0; x < xDim; x++)
                {
                    for (int y = 0; y < yDim; y++)
                    {
                        Vector3 spawnPosition = GetWorldPosition(x, y);
                        GameObject background = Instantiate(backgroundPrefab[0], spawnPosition, Quaternion.identity);
                        background.transform.parent = backGroundParent.transform;
                    }
                }
            }



            gems = new Gem[xDim, yDim];
            for (int x = 0; x < xDim; x++)
            {
                for (int y = 0; y < yDim; y++)
                {
                    SpawnNewGem(x, y, GemType.EMPTY);
                }
            }

            Fill();

        }

        public void Fill()
        {
            while (FillStep())
            {

            }
        }

        public bool FillStep()
        {
            bool movedGem = false;
            for (int y = yDim - 2; y >= 0; y--)
            {
                for (int x = 0; x < xDim; x++)
                {
                    Gem gem = gems[x, y];
                    if (gem.IsMovable())
                    {
                        Gem gemBelow = gems[x, y + 1];

                        if (gemBelow.GemType == GemType.EMPTY)
                        {
                            gem.MovableComponent.Move(x, y + 1);
                            gems[x, y + 1] = gem;
                            SpawnNewGem(x, y, GemType.EMPTY);
                            movedGem = true;
                        }
                    }
                }

            }

            for (int x = 0; x < xDim; x++)
            {
                Gem gemBelow = gems[x, 0];

                if (gemBelow.GemType == GemType.EMPTY)
                {
                    GameObject newGem = Instantiate(gemPrefabDic[GemType.NORMAL], GetWorldPosition(x, -1), Quaternion.identity);
                    newGem.transform.parent = this.transform;
                    gems[x, 0] = newGem.GetComponent<Gem>();
                    gems[x, 0].Init(x, -1, this, GemType.NORMAL);
                    gems[x, 0].MovableComponent.Move(x, 0);
                    gems[x, 0].ColorComponent.SetColor((ColorGem.ColorType)Random.Range(0, gems[x, 0].ColorComponent.NumColors));
                    movedGem = true;
                }
            }

            return movedGem;
        }
        public Vector2 GetWorldPosition(int x, int y)
        {
            return new Vector2(transform.position.x - xDim / 2.0f + x,
                transform.position.y + yDim / 2.0f - y);
        }

        public Gem SpawnNewGem(int x, int y, GemType type)
        {
            GameObject newGem = Instantiate(gemPrefabDic[type], new Vector3(x, y, 0), Quaternion.identity);
            newGem.transform.parent = transform;

            gems[x, y] = newGem.GetComponent<Gem>();
            gems[x, y].Init(x, y, this, type);

            return gems[x, y];
        }
    }
}

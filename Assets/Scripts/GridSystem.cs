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
            EMPTY,
            OBSTACLE
        };

        [System.Serializable]
        public struct GemPrefab
        {
            public GemType type;
            public GameObject prefab;
        };

        [SerializeField] int xDim;
        [SerializeField] int yDim;
        public float fillTime;

        [SerializeField] GemPrefab[] gemPrefabs;
        [SerializeField] GameObject[] backgroundPrefab;

        private Dictionary<GemType, GameObject> gemPrefabDic;

        private Gem[,] gems;

        private bool inverse = false;

        private Gem pressedGem;
        private Gem enteredGem;
        private Gem swipedGem;

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

           
            Destroy(gems[4, 4].gameObject);

            SpawnNewGem(4,4, GemType.OBSTACLE);


            Destroy(gems[3, 4].gameObject);
            SpawnNewGem(3, 4, GemType.OBSTACLE);

            Destroy(gems[5, 6].gameObject);
            SpawnNewGem(5, 6, GemType.OBSTACLE);



            StartCoroutine(Fill());
        }



        public IEnumerator Fill()
        {
            while (FillStep())
            {
                inverse = !inverse;
                yield return new WaitForSeconds(fillTime);
            }
        }

        public bool FillStep()
        {
            bool movedGem = false;
            // Move gems down from top to bottom
            for (int y = yDim - 2; y >= 0; y--)
            {
                for (int loopX = 0; loopX < xDim; loopX++)
                {   
                    int x= loopX;

                    if (inverse)
                    {
                        x = xDim - 1 - loopX;
                    }

                    Gem gem = gems[x, y]; // Get the gem at the current position

                    if (gem.IsMovable()) // Check if the gem can move
                    {
                        Gem gemBelow = gems[x, y + 1]; // Get the gem directly below the current one

                        if (gemBelow.GemType == GemType.EMPTY) // If the spot below is empty
                        {
                            Destroy(gemBelow.gameObject);
                            gem.MovableComponent.Move(x, y + 1, fillTime); // Move the gem down
                            gems[x, y + 1] = gem; // Update the grid to reflect the new position
                            SpawnNewGem(x, y, GemType.EMPTY); // Replace the old spot with an empty gem
                            movedGem = true;
                        }
                        else
                        {
                            for(int diag = -1; diag <= 1; diag++)
                            {
                                if (diag != 0)
                                {
                                    int diagX = x + diag;

                                    if (inverse)
                                    {
                                        diagX = x - diag;
                                    }

                                    if(diagX>= 0 && diagX < xDim)
                                    {
                                        Gem diagonalGem = gems[diagX, y + 1];

                                        if(diagonalGem.GemType == GemType.EMPTY)
                                        {
                                            bool hasGemAbove = true;

                                            for( int aboveY = y; aboveY>=0; aboveY--)
                                            {
                                                Gem gemAbove = gems[diagX,aboveY];

                                                if (gemAbove.IsMovable())
                                                {
                                                    break;
                                                }
                                                else if(!gemAbove.IsMovable() && gemAbove.GemType != GemType.EMPTY)
                                                {
                                                    hasGemAbove = false;
                                                    break;
                                                }
                                            }

                                            if (!hasGemAbove)
                                            {
                                                Destroy(diagonalGem.gameObject);
                                                gem.MovableComponent.Move(diagX,y+1,fillTime);
                                                gems[diagX,y+1] = gem;
                                                SpawnNewGem(x,y,GemType.EMPTY);
                                                movedGem = true;
                                                break;
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }

            }

            for (int x = 0; x < xDim; x++) // Fill bottom row with new gems where empty
            {
                Gem gemBelow = gems[x, 0];  // Get the gem at the bottom row

                if (gemBelow.GemType == GemType.EMPTY) // If the spot is empty
                {
                    // Create a new gem above the grid (at y = -1 for animation purposes)
                    GameObject newGem = Instantiate(gemPrefabDic[GemType.NORMAL], GetWorldPosition(x, -1), Quaternion.identity);
                    newGem.transform.parent = this.transform;

                    // Initialize the new gem and assign it to the grid
                    gems[x, 0] = newGem.GetComponent<Gem>();
                    gems[x, 0].Init(x, -1, this, GemType.NORMAL);
                    gems[x, 0].MovableComponent.Move(x, 0, fillTime); // Move it to the correct position on the grid
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
            Vector3 spawnPosition = GetWorldPosition(x, y); // Calculate world position
            GameObject newGem = Instantiate(gemPrefabDic[type], spawnPosition, Quaternion.identity);
            newGem.transform.parent = transform;

            gems[x, y] = newGem.GetComponent<Gem>();
            gems[x, y].Init(x, y, this, type);

            return gems[x, y];
        }

        public bool IsAdjacent(Gem gem1, Gem gem2)
        {
            return (gem1.X == gem2.X && (int)Mathf.Abs(gem1.Y - gem2.Y) == 1) || (gem1.Y == gem2.Y && (int)Mathf.Abs(gem1.X - gem2.X) == 1);
        }

        public void SwapGems(Gem gem1, Gem gem2)
        {
            if (gem1.IsMovable() && gem2.IsMovable()) 
            {
                gems[gem1.X, gem1.Y] = gem2;
                gems[gem2.X, gem2.Y] = gem1;
                
                int gem1X = gem1.X;
                int gem1Y = gem1.Y;

                gem1.MovableComponent.Move(gem2.X, gem2.Y,fillTime);
                gem2.MovableComponent.Move(gem1X, gem1Y,fillTime);

            }
        }

        public void PressGem(Gem gem)
        {
             pressedGem = gem;
       
        }

        public void EnterGem(Gem gem)
        {
            enteredGem = gem;
        }

        public void ReleaseGem()
        {
            if (IsAdjacent(pressedGem, enteredGem))
            {
                SwapGems(pressedGem, enteredGem);
            }
        }


    }
}

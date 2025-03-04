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

        public Gem pressedGem;
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


            //Destroy(gems[4, 4].gameObject);

            //SpawnNewGem(4, 4, GemType.OBSTACLE);


            //Destroy(gems[3, 4].gameObject);
            //SpawnNewGem(3, 4, GemType.OBSTACLE);

            //Destroy(gems[5, 6].gameObject);
            //SpawnNewGem(5, 6, GemType.OBSTACLE);



            StartCoroutine(Fill());
        }



        public IEnumerator Fill()
        {
            bool needsRefill = true;
            while (needsRefill)
            {
                yield return new WaitForSeconds(fillTime);
                while (FillStep())
                {
                    inverse = !inverse;
                    yield return new WaitForSeconds(fillTime);
                }

                needsRefill = ClearAllValidMatches();
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
                    int x = loopX;

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
                            for (int diag = -1; diag <= 1; diag++)
                            {
                                if (diag != 0)
                                {
                                    int diagX = x + diag;

                                    if (inverse)
                                    {
                                        diagX = x - diag;
                                    }

                                    if (diagX >= 0 && diagX < xDim)
                                    {
                                        Gem diagonalGem = gems[diagX, y + 1];

                                        if (diagonalGem.GemType == GemType.EMPTY)
                                        {
                                            bool hasGemAbove = true;

                                            for (int aboveY = y; aboveY >= 0; aboveY--)
                                            {
                                                Gem gemAbove = gems[diagX, aboveY];

                                                if (gemAbove.IsMovable())
                                                {
                                                    break;
                                                }
                                                else if (!gemAbove.IsMovable() && gemAbove.GemType != GemType.EMPTY)
                                                {
                                                    hasGemAbove = false;
                                                    break;
                                                }
                                            }

                                            if (!hasGemAbove)
                                            {
                                                Destroy(diagonalGem.gameObject);
                                                gem.MovableComponent.Move(diagX, y + 1, fillTime);
                                                gems[diagX, y + 1] = gem;
                                                SpawnNewGem(x, y, GemType.EMPTY);
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

                if (GetMatch(gem1, gem2.X, gem2.Y) != null || GetMatch(gem2, gem1.X, gem2.Y) != null)
                {
                    int gem1X = gem1.X;
                    int gem1Y = gem1.Y;

                    gem1.MovableComponent.Move(gem2.X, gem2.Y, fillTime);
                    gem2.MovableComponent.Move(gem1X, gem1Y, fillTime);

                    ClearAllValidMatches();
                    StartCoroutine(Fill());
                }
                else
                {
                    //gems[gem1.X, gem1.Y] = gem1;
                    //gems[gem2.X, gem2.Y] = gem2;

                    SimulateSwapAndReturn(pressedGem, enteredGem, fillTime);
                }


            }
        }
        public void SimulateSwapAndReturn(Gem gem1, Gem gem2, float fillTime)
        {

            int gem1X = gem1.X;
            int gem1Y = gem1.Y;
            int gem2X = gem2.X;
            int gem2Y = gem2.Y;

            gem1.MovableComponent.Move(gem2.X, gem2.Y, fillTime);
            gem2.MovableComponent.Move(gem1X, gem1Y, fillTime);

            gems[gem1.X, gem1.Y] = gem2;
            gems[gem2.X, gem2.Y] = gem1;

            StartCoroutine(ReturnToOriginalPositionAfterDelay(gem1, gem2, gem1X, gem1Y, gem2X, gem2Y, fillTime));
        }

        private IEnumerator ReturnToOriginalPositionAfterDelay(Gem gem1, Gem gem2, int gem1X, int gem1Y, int gem2X, int gem2Y, float fillTime)
        {

            yield return new WaitForSeconds(fillTime);


            gem1.MovableComponent.Move(gem1X, gem1Y, fillTime);
            gem2.MovableComponent.Move(gem2X, gem2Y, fillTime);


            gems[gem1X, gem1Y] = gem1;
            gems[gem2X, gem2Y] = gem2;
        }


        #region Click
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
        #endregion

        #region Match Gems

        public List<Gem> GetMatch(Gem gem, int newX, int newY)
        {
            if (gem.IsColored())
            {
                ColorGem.ColorType color = gem.ColorComponent.Color;
                List<Gem> horizontalGems = new List<Gem>();
                List<Gem> verticalGems = new List<Gem>();
                List<Gem> matchingGems = new List<Gem>();

                horizontalGems.Add(gem);
                for (int dir = 0; dir <= 1; dir++)
                {
                    for (int xOffset = 1; xOffset < xDim; xOffset++)
                    {
                        int x;
                        if (dir == 0) // Left
                        {
                            x = newX - xOffset;
                        }
                        else // Right
                        {
                            x = newX + xOffset;
                        }

                        if (x < 0 || x >= xDim) { break; }


                        if (gems[x, newY].IsColored() && gems[x, newY].ColorComponent.Color == color)
                        {
                            horizontalGems.Add(gems[x, newY]);
                        }
                        else { break; }


                    }
                }

                if (horizontalGems.Count >= 3)
                {
                    for (int i = 0; i < horizontalGems.Count; i++)
                    {
                        matchingGems.Add(horizontalGems[i]);

                    }
                }

                //Traverse vertically if we found a match (for T and L shape)
                if (horizontalGems.Count >= 3)
                {
                    for (int i = 0; i < horizontalGems.Count; i++)
                    {
                        for (int dir = 0; dir <= 1; dir++)
                        {
                            for (int yOffset = 1; yOffset < yDim; yOffset++)
                            {
                                int y;
                                if (dir == 0)
                                { y = newY - yOffset; }
                                else { y = newY + yOffset; }

                                if (y < 0 || y >= yDim) { break; }

                                if (gems[horizontalGems[i].X, y].IsColored() && gems[horizontalGems[i].X, y].ColorComponent.Color == color)
                                {
                                    verticalGems.Add(gems[horizontalGems[i].X, y]);
                                }
                                else { break; }
                            }
                        }

                        if (verticalGems.Count < 2)
                        {
                            verticalGems.Clear();
                        }
                        else
                        {
                            for (int j = 0; j < verticalGems.Count; j++)
                            {
                                matchingGems.Add(verticalGems[j]);
                            }
                            break;
                        }
                    }
                }

                if (matchingGems.Count >= 3)
                {
                    return matchingGems;
                }

                horizontalGems.Clear();
                verticalGems.Clear();
                // Check vertically
                verticalGems.Add(gem);
                for (int dir = 0; dir <= 1; dir++)
                {
                    for (int yOffset = 1; yOffset < yDim; yOffset++)
                    {
                        int y;
                        if (dir == 0) // Up
                        {
                            y = newY - yOffset;
                        }
                        else // Down
                        {
                            y = newY + yOffset;
                        }

                        if (y < 0 || y >= yDim) { break; }


                        if (gems[newX, y].IsColored() && gems[newX, y].ColorComponent.Color == color)
                        {
                            verticalGems.Add(gems[newX, y]);
                        }
                        else { break; }


                    }
                }

                if (verticalGems.Count >= 3)
                {
                    for (int i = 0; i < verticalGems.Count; i++)
                    {
                        matchingGems.Add(verticalGems[i]);

                    }
                }


                //Traverse horizontally if we found a match (for T and L shape)
                if (verticalGems.Count >= 3)
                {
                    for (int i = 0; i < verticalGems.Count; i++)
                    {
                        for (int dir = 0; dir <= 1; dir++)
                        {
                            for (int xOffset = 1; xOffset < xDim; xOffset++)
                            {
                                int x;
                                if (dir == 0) // Left
                                { x = newX - xOffset; }
                                else { x = newX + xOffset; } // Right

                                if (x < 0 || x >= xDim) { break; }

                                if (gems[x, verticalGems[i].Y].IsColored() && gems[x, verticalGems[i].Y].ColorComponent.Color == color)
                                {
                                    horizontalGems.Add(gems[x, verticalGems[i].Y]);
                                }
                                else { break; }
                            }
                        }

                        if (horizontalGems.Count < 2)
                        {
                            horizontalGems.Clear();
                        }
                        else
                        {
                            for (int j = 0; j < horizontalGems.Count; j++)
                            {
                                matchingGems.Add(horizontalGems[j]);
                            }
                            break;
                        }
                    }
                }

                if (matchingGems.Count >= 3)
                {
                    return matchingGems;
                }
            }

            return null;
        }

        #endregion

        public bool ClearAllValidMatches()
        {
            bool needsRefill=false;

            for (int y = 0; y < yDim; y++) 
            {
                for (int x = 0; x < xDim; x++)
                {
                    if (gems[x, y].IsClearable())
                    {
                        List<Gem> match = GetMatch(gems[x, y],x,y);
                        if (match != null)
                        {
                            for (int i = 0; i < match.Count; i++) {
                                if (ClearGem(match[i].X, match[i].Y)) 
                                { 
                                    needsRefill = true; 
                                }
                            }
                        }

                    }
                }
            }
            return needsRefill;
        }

        public bool ClearGem(int x, int y)
        {
            if (gems[x, y].IsClearable() && !gems[x,y].ClearableComponent.IsBeingCleared)
            {
                gems[x,y].ClearableComponent.Clear();
                SpawnNewGem(x,y,GemType.EMPTY);

                return true;
            }

            return false;
        }
    }
}
